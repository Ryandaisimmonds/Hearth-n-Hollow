// Hearth & Hollow — the player in the dungeon: walk, and run whatever the loadout holds.
//   J attack · K skill 1 · U skill 2 · Space dash. Moves, their numbers and tree bonuses come from GameSession
//   (Core: MoveData + Progression); this file only decides how each MoveKind plays out in the world.
// HP, damage, armour and crits come from GameSession too (meals, gear, level and skill tree included).
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class PlayerController : MonoBehaviour
    {
        DungeonController _dc;
        GameSession S { get { return _dc.Session; } }
        TuningData T { get { return _dc.Session.Content.tuning; } }

        Rigidbody2D _rb;
        CharacterAnimator _anim;
        ClassId _class;
        MoveData _atk, _skill1, _skill2, _dash, _ult;
        CombatMods _mods;

        Vector2 _move;
        Facing _facing = Facing.Down;
        Vector2 _aim = Vector2.down;   // exact direction of the last movement input (the sprite only has 4 facings)
        // attack
        float _attackCd, _attackT = -1f, _attackHitAt, _attackLen;
        bool _attackHitDone;
        // dash
        float _dashCd, _dashT; Vector2 _dashDir; float _dashSpeedMult = 1f;
        readonly HashSet<EnemyController> _dashHit = new HashSet<EnemyController>();
        float _trailT;
        // skills
        float _skill1Cd, _skill2Cd, _ultCd;
        int _ghostN;   // dash trail counter (alternates colours / lights)
        // buffs
        float _guardUntil, _guardMult = 1f;
        float _furyUntil, _furyBonus;
        float _hasteUntil;
        float _whirlUntil, _whirlTick;
        MoveData _whirlMove;
        // lunge step
        float _stepT; Vector2 _stepVel;
        // hits
        float _invulnUntil;
        float _knockT; Vector2 _knock;
        // timed follow-ups (volley's second arrow, slam's travelling wave, the hot pot landing…)
        struct Timed { public float t; public Action act; }
        readonly List<Timed> _timed = new List<Timed>();
        // the kind of hit being dealt (Break & Boost removed in ticket 01; ticket 06 turns this into elements)
        HitType _curType = HitType.Slash; int _curPips = 1;   // the kind of hit being dealt right now
        MoveData _curMove;   // the move being used right now (its light, homing…)
        // class rework stage 2: combo chains, Sunshield, Dawnbreaker's leap
        int _comboStep; float _comboUntil;
        MoveData _sunMove; float _sunUntil; int _sunLvl; UnityEngine.Rendering.Universal.Light2D _sunLight;
        readonly HashSet<EnemyController> _sunHit = new HashSet<EnemyController>();
        float _hopT, _hopDur;
        // Lamplighter: Sunbeam
        MoveData _beamMove; float _beamUntil, _beamTick, _beamMult, _beamK; HitType _beamType; int _beamPips;
        SpriteRenderer _beamSr; UnityEngine.Rendering.Universal.Light2D[] _beamLights;
        // Emberfist: Rising Phoenix, Cinder Step
        MoveData _phoenixMove; float _phoenixT, _phoenixMult; int _phoenixLvl, _phoenixPips; HitType _phoenixType;
        readonly HashSet<EnemyController> _phoenixHit = new HashSet<EnemyController>();
        MoveData _counterMove; float _counterUntil, _counterMult;
        Vector2 _sweepFrom;   // where a fast move's body was last frame (so a low frame rate can't skip past enemies)
        bool _dashTail;       // one last sweep on the frame a dash ends (a short dash can finish between two frames)

        public Vector2 Feet { get { return transform.position; } }
        public Vector2 Body { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }
        /// Hip height: projectiles fly on this plane so they hit short and tall monsters alike.
        Vector2 Hip { get { return (Vector2)transform.position + new Vector2(0, 0.45f); } }
        public bool Dashing { get { return _dashT > 0f; } }
        /// Walking, dashing or being shoved (Heat bites harder when this is false).
        public bool Moving { get { return _move.sqrMagnitude > 0.01f || Dashing || _knockT > 0f || _stepT > 0f; } }
        public Vector2 Aim { get { return _aim; } }

        /// Point the aim along the current movement input (any angle), and the sprite's facing to match.
        void TurnToInput()
        {
            Vector2 a;   // mouse cursor / right stick first (DungeonInput), then the way you're walking
            if (DungeonInput.AimFrom(Body, out a)) { _aim = a; _facing = CharacterAnimator.FacingFrom(a, _facing); return; }
            if (_move.sqrMagnitude <= 0.01f) return;
            _aim = _move.normalized;
            _facing = CharacterAnimator.FacingFrom(_move, _facing);
        }

        public void Setup(DungeonController dc, ClassId cls, HearthHollow.Data.ArtLibrary.Anim art)
        {
            _dc = dc;
            _class = cls;
            var load = dc.Session.Loadout(cls);
            _atk = load[0]; _skill1 = load[1]; _skill2 = load[2]; _dash = load[3];
            var cdat = dc.Session.Content.classes.Find(x => x.id == cls);
            _ult = cdat != null && !string.IsNullOrEmpty(cdat.ultimate) ? dc.Session.Content.moves.Find(x => x.id == cdat.ultimate) : null;
            _ultCd = 0f;   // each run starts with the ultimate ready
            _mods = dc.Session.Mods ?? new CombatMods();

            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; _rb.freezeRotation = true; _rb.mass = 1f;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.28f; col.offset = new Vector2(0, 0.2f);

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.AddComponent<SpriteRenderer>();
            _anim = body.AddComponent<CharacterAnimator>();
            _anim.Setup(art, dc.Settings.characterPixelsPerUnit, 1f);
            // body hurt-box (trigger) so bolts aimed at the torso connect, not just ones that clip the feet
            var hurt = body.AddComponent<CircleCollider2D>();
            hurt.isTrigger = true;
            hurt.radius = Mathf.Max(0.2f, _anim.FigureHeight * 0.35f) / Mathf.Max(0.01f, body.transform.lossyScale.x);
            hurt.offset = (Vector2)body.transform.InverseTransformPoint(Body);

            PushHud();
        }

        /// Remaining cooldowns (skill 1, skill 2, dash) — carried to the next floor by the DungeonController.
        public float[] Cooldowns { get { return new[] { Mathf.Max(0f, _skill1Cd), Mathf.Max(0f, _skill2Cd), Mathf.Max(0f, _dashCd), Mathf.Max(0f, _ultCd) }; } }
        public void SetCooldowns(float[] cd)
        {
            if (cd == null || cd.Length < 3) return;
            _skill1Cd = cd[0]; _skill2Cd = cd[1]; _dashCd = cd[2];
            if (cd.Length > 3) _ultCd = cd[3];
        }

        public void Teleport(Vector2 pos)
        {
            transform.position = pos;
            if (_rb != null) { _rb.position = pos; _rb.linearVelocity = Vector2.zero; }
            _dashT = 0f; _knockT = 0f; _stepT = 0f;
        }

        // ───────────────────────── input & timers ─────────────────────────
        void Update()
        {
            if (_dc == null || !_dc.GameplayActive) { _move = Vector2.zero; ShowReticle(false); return; }
            float dt = Time.deltaTime;
            _attackCd -= dt; _dashCd -= dt; _skill1Cd -= dt; _skill2Cd -= dt; _ultCd -= dt;
            if (_dashT > 0f) _dashT -= dt;
            if (_knockT > 0f) _knockT -= dt;
            if (_stepT > 0f) _stepT -= dt;
            if (_hopT > 0f)   // Dawnbreaker's leap: the body arcs up and lands
            {
                _hopT -= dt;
                float hk = 1f - Mathf.Clamp01(_hopT / Mathf.Max(0.01f, _hopDur));
                _anim.transform.localPosition = _hopT > 0f ? new Vector3(0, Mathf.Sin(hk * Mathf.PI) * 0.9f, 0) : Vector3.zero;
            }

            _move = DungeonInput.Move;
            ReticleUpdate();
            bool attacking = _attackT >= 0f;
            if (!attacking) TurnToInput();

            if (DungeonInput.Dash && _dashCd <= 0f && _knockT <= 0f)
            {
                if (_dc.Session.DashUnlocked) StartDash();
                else if (Time.unscaledTime > _noDashHintAt) { _noDashHintAt = Time.unscaledTime + 2.5f; DungeonHud.Number(Body + new Vector2(0, 0.6f), "No boots to roll in yet", "dnum--info"); }
            }
            if ((DungeonInput.Attack || DungeonInput.AttackHeld) && _attackCd <= 0f && !Dashing) StartAttack();
            if (DungeonInput.Special) TrySkill(_skill1, ref _skill1Cd, "K");
            if (DungeonInput.Special2) TrySkill(_skill2, ref _skill2Cd, "L");
            if (DungeonInput.Ultimate) TryUltimate();
            if (DungeonInput.Interact) _dc.TryInteract(this);
            // using an exit / ladder tears this floor down immediately — stop before anything else spawns into it
            if (!Alive) return;

            // attack timeline
            if (_attackT >= 0f)
            {
                _attackT += dt;
                if (!_attackHitDone && _attackT >= _attackHitAt) { _attackHitDone = true; AttackHit(); if (!Alive) return; }
                if (_attackT >= _attackLen) _attackT = -1f;
            }
            RunTimed(dt);
            if (!Alive) return;
            DashUpdate(dt);
            WhirlUpdate(dt);
            SunUpdate();
            BeamUpdate(dt);
            if (!Alive) return;
            PhoenixUpdate(dt);
            if (!Alive) return;
            if (!Alive) return;

            float now = Time.time;
            if (_guardUntil > 0f && now >= _guardUntil) { _guardUntil = 0f; _guardMult = 1f; S.DungeonGuardMult = 1f; }
            if (_furyUntil > 0f && now >= _furyUntil) { _furyUntil = 0f; _furyBonus = 0f; }

            // anim
            _anim.Dir = _facing;
            _anim.SetMoving(_move.sqrMagnitude > 0.01f || Dashing);
            UpdateTint();
            PushHud();
        }

        bool Alive { get { return _dc != null && _dc.FloorRoot != null && _dc.Player == this && _dc.Session.InDungeon; } }

        void FixedUpdate()
        {
            if (_rb == null) return;
            if (_dc == null || !_dc.GameplayActive) { _rb.linearVelocity = Vector2.zero; return; }
            Vector2 v;
            if (_knockT > 0f) v = _knock;
            else if (Dashing) v = _dashDir * _dc.Settings.dashSpeed * _dashSpeedMult;
            else if (_stepT > 0f) v = _stepVel;
            else
            {
                float speed = _dc.Settings.playerSpeed * _dc.Session.ArcSpeed * _mods.moveSpeed;   // opening arc: a slower start that gear builds back up
                if (Time.time < _hasteUntil) speed *= 1.3f;
                speed *= 1f - _dc.Session.DungeonHazardSlow;   // Frostfang cold
                speed *= _dc.Session.MutatorSpeed;              // Swift ×2 / Frantic ×3
                if (_attackT >= 0f) speed *= 0.3f;
                else if (_whirlUntil > Time.time) speed *= 0.7f;
                if (_beamMove != null) speed *= 0.5f;   // channelling Sunbeam
                v = _move * speed;
            }
            _rb.linearVelocity = v;
        }

        void LateUpdate() { if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y); }

        void UpdateTint()
        {
            var sr = _anim.Renderer;
            if (sr == null) return;
            Color c = Color.white;
            if (_guardUntil > 0f) c = new Color(1f, 0.92f, 0.55f);
            else if (_furyUntil > 0f) c = new Color(1f, 0.7f, 0.6f);
            if (Time.time < _invulnUntil && !Dashing) c.a = (Mathf.FloorToInt(Time.time * 18f) % 2 == 0) ? 0.35f : 1f;
            if (Dashing) c.a = 0.6f;
            sr.color = c;
        }

        void PushHud()
        {
            DungeonHud.SkillUnlocked = _skill1 != null;
            DungeonHud.Skill01 = _skill1 != null ? Mathf.Clamp01(1f - _skill1Cd / Mathf.Max(0.1f, SkillCooldown(_skill1))) : 0f;
            DungeonHud.Skill2Move = _skill2;
            DungeonHud.Skill1Move = _skill1;
            DungeonHud.Skill2_01 = _skill2 != null ? Mathf.Clamp01(1f - _skill2Cd / Mathf.Max(0.1f, SkillCooldown(_skill2))) : 0f;
            DungeonHud.Dash01 = Mathf.Clamp01(1f - _dashCd / Mathf.Max(0.1f, DashCooldown));
            DungeonHud.DashMove = _dash;
            DungeonHud.AttackMove = _atk;
            DungeonHud.Attack01 = _atk != null ? Mathf.Clamp01(1f - _attackCd / Mathf.Max(0.1f, _atk.cooldown)) : 1f;
            DungeonHud.UltMove = _ult;
            DungeonHud.Ult01 = _ult != null ? Mathf.Clamp01(1f - _ultCd / Mathf.Max(0.1f, _ult.cooldown)) : 0f;
            DungeonHud.Guarding = _guardUntil > 0f;
            string buff = null;
            if (_guardUntil > 0f) buff = "🛡 Guard";
            if (_furyUntil > 0f) buff = (buff != null ? buff + "  " : "") + "📯 Fury";
            if (Time.time < _hasteUntil) buff = (buff != null ? buff + "  " : "") + "💨 Haste";
            string hazard = _dc != null ? _dc.Session.DungeonHazardText : null;   // ☠️ Poison · 50% less
            if (hazard != null) buff = (buff != null ? buff + "  " : "") + hazard;
            string muts = _dc != null ? _dc.Session.DungeonMutatorText : null;   // 💨 Swift  💥 Big Bangs
            if (muts != null) buff = (buff != null ? buff + "  " : "") + muts;
            DungeonHud.BuffText = buff;
        }

        // ───────────────────────── numbers ─────────────────────────
        float DamageBuff { get { return 1f + (_furyUntil > Time.time ? _furyBonus : 0f); } }
        float AttackMult(MoveData m) { return (m != null ? m.power : 1f) * _mods.attackPower * DamageBuff; }
        float SkillMult(MoveData m) { return (m != null ? m.power : 1f) * _mods.skillPower * DamageBuff; }
        float SkillCooldown(MoveData m) { return m == null ? 1f : Mathf.Max(0.5f, m.cooldown * _mods.skillCooldown); }
        float DashCooldown { get { return _dash == null ? T.dashCooldownSeconds : Mathf.Max(0.2f, _dash.cooldown * _mods.dashCooldown); } }

        void After(float seconds, Action act) { _timed.Add(new Timed { t = seconds, act = act }); }

        void RunTimed(float dt)
        {
            for (int i = 0; i < _timed.Count; i++)
            {
                var x = _timed[i]; x.t -= dt; _timed[i] = x;
            }
            for (int i = 0; i < _timed.Count; i++)
            {
                if (_timed[i].t > 0f) continue;
                var act = _timed[i].act;
                _timed.RemoveAt(i); i--;
                act();
                if (!Alive) { _timed.Clear(); return; }
            }
        }

        /// Hits every enemy in a circle once. Returns how many were hit. Stops if the run ends mid-way.
        int HitArea(Vector2 centre, float radius, float mult, Vector2 knockDir, float knock = 6f, float stun = 0f)
        {
            int n = 0;
            radius *= S.MutatorAreaRadius; mult *= S.MutatorAreaDamage;   // Big Bangs
            IgniteNear(centre, radius);   // fire reaches braziers
            foreach (var e in Combat2D.EnemiesIn(centre, radius))
            {
                if (e == null || e.Dead) continue;
                Hit(e, mult, knockDir.sqrMagnitude > 0.01f ? knockDir : ((Vector2)e.transform.position - centre).normalized, knock);
                if (!Alive) return n;
                if (stun > 0f && e != null && !e.Dead) e.Stun(stun);
                n++;
            }
            return n;
        }

        void IgniteNear(Vector2 p, float r)
        {
            var room = _dc != null ? _dc.CurrentRoom : null;
            if (room != null && room.Lights != null) room.Lights.IgniteNear(p, r);
            if (room != null && room.Secrets != null) room.Secrets.Hit(p, r);   // knock on cracked walls
        }

        void Hit(EnemyController e, float mult, Vector2 knockDir, float knock = 6f) { Hit(e, mult, knockDir, knock, _curType, _curPips); }

        void Hit(EnemyController e, float mult, Vector2 knockDir, float knock, HitType type, int pips)
        {
            bool crit;
            float dmg = S.DungeonRollDamage(mult, out crit);
            e.TakeHit(dmg, crit, knockDir, knock);
            if (e != null && !e.Dead) e.TypedHit(type, pips);
        }

        Projectile Arrow(float mult, Vector2 dir, float speed, bool pierce, float scale = 1.4f)
        {
            var p = Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, speed, mult, 0f, DungeonFx.Arrow, Color.white, pierce, scale);
            if (p != null)
            {
                p.SetHit(_curType, _curPips);
                if (_curMove != null)
                {
                    DungeonFx.Flash(p.transform.position, MoveColour(_curMove), LightRadius(_curMove) * 0.7f, (_curMove.lightIntensity > 0f ? _curMove.lightIntensity : 1.4f) * ColourLightK, 0f, p.transform);   // every arrow carries its own light
                    if (_curMove.homing > 0f) p.Homing(_curMove.homing, Mathf.Max(6f, speed * 0.8f));
                    p.Tint(Color.Lerp(Color.white, MoveColour(_curMove), 0.92f));   // arrows / skewers in the move's colour
                }
            }
            return p;
        }

        // ───────────────────────── attack (J) ─────────────────────────
        void StartAttack()
        {
            var m = _atk;
            if (m == null) return;
            TurnToInput();   // a held attack re-aims each swing
            _attackT = 0f;
            _attackHitDone = false;
            _attackCd = Mathf.Max(0.2f, m.cooldown);
            switch (m.kind)
            {
                case MoveKind.Heavy: _attackLen = 0.62f; _attackHitAt = 0.34f; break;
                case MoveKind.Lunge: _attackLen = 0.34f; _attackHitAt = 0.12f; break;
                case MoveKind.Arrow: case MoveKind.SpreadArrow: _attackLen = 0.34f; _attackHitAt = 0.14f; break;
                case MoveKind.PierceArrow: _attackLen = 0.5f; _attackHitAt = 0.28f; break;
                case MoveKind.Wisp: _attackLen = 0.3f; _attackHitAt = 0.12f; break;
                case MoveKind.Combo:
                {
                    int chain = Mathf.Max(1, m.count);
                    _comboStep = Time.time <= _comboUntil ? (_comboStep + 1) % chain : 0;
                    if (_comboStep == chain - 1) { _attackLen = 0.42f; _attackHitAt = 0.16f; _attackCd = m.cooldown * 1.7f; }   // the finisher: slower, bigger
                    else { _attackLen = 0.26f; _attackHitAt = 0.08f; }
                    _comboUntil = Time.time + Mathf.Max(_attackCd, _attackLen) + _dc.Settings.comboWindow;
                    break;
                }
                default: _attackLen = 0.3f; _attackHitAt = 0.1f; break;
            }
            _attackLen = Mathf.Min(_attackLen, _attackCd);
            _anim.Dir = _facing;
            _anim.PlayAttack(_attackLen);
        }

        void AttackHit()
        {
            var m = _atk; Vector2 dir = Aim;
            float mult = AttackMult(m);
            int blv = 0;
            _curType = m.hitType; _curPips = 1; _curMove = m;
            dir = Assist(m, dir); _aim = dir;
            if (IsRanged(m)) { MoveLight(m, Body + dir * 0.5f, 0.9f, 0.25f); DungeonFx.Embers(Body + dir * 0.5f, Accent, 4, 0.25f); }   // muzzle flash (the arrows carry their own light)
            else
            {
                MoveLight(m, Body + dir * m.range * 0.8f, 1.4f + 0.25f * blv, 0.45f);
                DungeonFx.Embers(Body + dir * m.range, Accent, 6 + 2 * blv, 0.45f);   // every swing throws sparks of the class colour
                if (blv > 0) _dc.FloodLight(0.1f * blv, 0.6f);
            }
            switch (m.kind)
            {
                case MoveKind.Wisp:
                    Motes(m, mult, dir, blv);
                    break;
                case MoveKind.Combo:
                {
                    bool fin = _comboStep >= Mathf.Max(1, m.count) - 1;
                    int extra = m.boostShape == BoostShape.Hits ? Mathf.RoundToInt(m.boostStep * blv) : 0;
                    ComboHit(m, fin, dir, mult, blv, _curPips);
                    for (int h = 1; h <= extra; h++) { Vector2 d2 = dir; float mm = mult; int lv = blv, pp = _curPips; After(0.12f * h, () => ComboHit(m, true, d2, mm, lv, pp)); }   // Boost: extra finishers
                    break;
                }
                case MoveKind.Arrow:
                    Fan(m, mult, dir, m.range, false, 1.4f, blv);
                    break;
                case MoveKind.SpreadArrow:
                    Fan(m, mult, dir, m.range, false, 1.2f, blv);
                    break;
                case MoveKind.PierceArrow:
                    Fan(m, mult, dir, m.range, true, 1.9f, blv);
                    DungeonFx.Puff(Hip + dir * 0.4f, A(MoveColour(m), 0.5f), 4, 0.35f);
                    break;
                case MoveKind.Lunge:
                    _stepVel = dir * (m.range / 0.12f) * 0.5f; _stepT = 0.12f;   // a short step forward
                    DungeonFx.SlashAt(Body + dir * 0.4f, dir, A(MoveColour(m), 0.95f), 1.7f);
                    DungeonFx.RingAt(Body + dir * m.range, A(Accent, 0.8f), m.radius * 1.2f, 0.25f);
                    IgniteNear(Body + dir * m.range, m.radius + 0.4f);
                    // a thrust: three small circles along the line
                    var done = new HashSet<EnemyController>();
                    for (int i = 1; i <= 3; i++)
                        foreach (var e in Combat2D.EnemiesIn(Body + dir * (m.range * i / 3f), m.radius * Reach(m, blv)))
                            if (done.Add(e)) { Hit(e, mult, dir); if (!Alive) return; }
                    break;
                case MoveKind.Heavy:
                    DungeonFx.SlashAt(Body + dir * 0.3f, dir, A(MoveColour(m), 1f), 2.1f);
                    DungeonFx.RingAt(Body + dir * m.range, A(MoveColour(m), 0.85f), m.radius, 0.3f);
                    DungeonFx.RingAt(Body + dir * m.range, A(Accent, 0.7f), m.radius * 0.6f, 0.4f);
                    DungeonFx.Flash(Body + dir * m.range, MoveColour(m), LightRadius(m) * 1.4f, 1.3f, 0.4f);
                    _dc.Shake(0.12f, 0.12f);
                    HitArea(Body + dir * m.range, m.radius * Reach(m, blv), mult, dir, 10f, m.duration);
                    break;
                default:   // Swing
                    DungeonFx.SlashAt(Body + dir * 0.25f, dir, A(MoveColour(m), 0.95f), m.range < 0.8f ? 1.25f : 1.5f);   // in the class's colour
                    HitArea(Body + dir * m.range, m.radius * Reach(m, blv), mult, dir);
                    break;
            }
        }

        // ───────────────────────── dash (Space) ─────────────────────────
        float _noDashHintAt;   // opening arc: the dash comes with the first boots

        void StartDash()
        {
            var m = _dash;
            _dashDir = _move.sqrMagnitude > 0.01f ? _move.normalized : Aim;
            _dashT = _dc.Settings.dashSeconds;
            _dashSpeedMult = 1f;
            _dashCd = DashCooldown;
            _attackT = -1f;   // dash cancels a swing
            _dashHit.Clear();
            _sweepFrom = Body; _dashTail = true;
            _trailT = 0f;
            DungeonFx.Puff(transform.position + new Vector3(0, 0.1f), new Color(0.8f, 0.74f, 0.62f, 0.55f), 6, 0.5f);   // dust kicked up
            _ghostT = 0f;
            if (m == null) return;
            MoveLight(m, Feet + new Vector2(0, 0.3f), 0.6f, 0.35f);
            switch (m.kind)
            {
                case MoveKind.Blink:
                {
                    Vector2 from = Feet;
                    Vector2 to = WalkTo(from, _dashDir, m.range);
                    DungeonFx.Flash(from + new Vector2(0, 0.4f), MoveColour(m), 3f, 1.4f, 3f);   // the light left behind
                    DungeonFx.Puff(Body, A(MoveColour(m), 0.7f), 10, 0.6f);
                    Teleport(to);
                    _dashT = 0.12f; _dashSpeedMult = 0f;
                    DungeonFx.RingAt(Body, MoveColour(m), 0.9f, 0.3f); DungeonFx.Embers(Feet, MoveColour(m), 10, 0.3f);
                    break;
                }
                case MoveKind.ChargeDash:
                    _dashT *= 1.25f; _dashSpeedMult = 0.95f;
                    break;
                case MoveKind.Vault:
                    _dashDir = -Aim;   // flip backwards, keep facing forward
                    _curType = m.hitType; _curPips = 1; _curMove = m;
                    Fan(m, m.power * _mods.attackPower * DamageBuff, Assist(m, Aim), 14f, false, 1.4f, 0);
                    break;
                case MoveKind.Flicker:
                    _dashT *= 0.6f; _dashSpeedMult = 1.35f;
                    break;
                case MoveKind.SlickDash:
                    _dashT *= 1.3f; _dashSpeedMult = 0.85f;
                    break;
            }
        }

        float _ghostT;   // dash after-images

        void DashUpdate(float dt)
        {
            if (Dashing)
            {
                _ghostT -= dt;
                if (_ghostT <= 0f)
                {
                    _ghostT = 0.035f; _ghostN++;
                    Color dc = MoveColour(_dash);
                    DungeonFx.Ghost(_anim.Renderer, A(dc, 0.5f), 0.25f, true, false);
                    DungeonFx.Embers(Feet, (_ghostN & 1) == 0 ? dc : Accent, 3, 0.25f);
                    if ((_ghostN & 1) == 0) DungeonFx.Flash(Feet + new Vector2(0, 0.3f), dc, 2.8f, 0.9f, 0.3f);   // a trail of light behind you
                }
            }
            if ((!Dashing && !_dashTail) || _dash == null) return;
            if (!Dashing) _dashTail = false;
            switch (_dash.kind)
            {
                case MoveKind.ChargeDash:
                    foreach (var e in Swept(0.75f))
                    {
                        if (!_dashHit.Add(e)) continue;
                        _curType = _dash.hitType; _curPips = 1; _curMove = _dash;
                        Hit(e, _dash.power * _mods.attackPower * DamageBuff, _dashDir, 11f);
                        if (!Alive) return;
                        _dc.Shake(0.06f, 0.06f);
                    }
                    break;
                case MoveKind.Flicker:   // singes whatever you pass through
                    foreach (var e in Swept(0.7f))
                    {
                        if (!_dashHit.Add(e)) continue;
                        _curType = _dash.hitType; _curPips = 1; _curMove = _dash;
                        Hit(e, Mathf.Max(0.2f, _dash.power) * _mods.attackPower * DamageBuff, _dashDir, 2f);
                        if (!Alive) return;
                    }
                    break;
                case MoveKind.SlickDash:
                    _trailT -= dt;
                    if (_trailT <= 0f)
                    {
                        _trailT = 0.05f;
                        var slick = _dash;
                        DungeonZone.Spawn(_dc, Feet, 0.6f, slick.duration, 0.1f, A(MoveColour(slick), 0.35f), z =>
                        {
                            foreach (var e in z.Enemies()) e.Slow(1f - slick.value, slick.duration);
                        });
                    }
                    break;
            }
        }

        // ───────────────────────── ultimate (C / I) ─────────────────────────
        void TryUltimate()
        {
            if (_ult == null || _knockT > 0f) return;
            if (_ultCd > 0f) { DungeonHud.Number(Body + Vector2.up * 0.8f, _ult.emoji + " " + Mathf.CeilToInt(_ultCd) + "s", "dnum--info"); return; }
            if (!UseSkill(_ult)) return;
            _ultCd = _ult.cooldown;
            DungeonHud.Number(Body + Vector2.up * 1.2f, _ult.emoji + " " + _ult.name + "!", "dnum--crit");
            GameFeel.HitStop(0.05f);
        }

        /// Every hit you land takes a bit off the ultimate's cooldown (called by EnemyController.TakeHit).
        public void ChargeUltimate()
        {
            if (_ult != null && _ultCd > 0f) _ultCd = Mathf.Max(0f, _ultCd - _dc.Settings.ultChargePerHit);
        }

        // ── aiming reticle (mouse): a ring + dot in the class colour, with a little light; the OS cursor hides meanwhile ──
        GameObject _reticle; SpriteRenderer _retRing, _retDot; float _retSpin;

        void ReticleUpdate()
        {
            Vector2 c;
            bool show = DungeonInput.MouseAim && DungeonInput.Cursor(out c) && !DungeonInput.PointerOverUI();
            if (!show) { ShowReticle(false); return; }
            DungeonInput.Cursor(out c);
            if (_reticle == null)
            {
                _reticle = new GameObject("Aim Reticle");
                Color col = ClassColour(_class);
                _retRing = new GameObject("Ring").AddComponent<SpriteRenderer>();
                _retRing.transform.SetParent(_reticle.transform, false);
                _retRing.sprite = DungeonFx.Ring; _retRing.color = new Color(col.r, col.g, col.b, 0.9f); _retRing.sortingOrder = 32000;
                _retRing.transform.localScale = Vector3.one * 0.55f;
                _retDot = new GameObject("Dot").AddComponent<SpriteRenderer>();
                _retDot.transform.SetParent(_reticle.transform, false);
                _retDot.sprite = DungeonFx.Circle; _retDot.color = Color.Lerp(col, Color.white, 0.35f); _retDot.sortingOrder = 32001;
                _retDot.transform.localScale = Vector3.one * 0.12f;
                DungeonFx.Flash(_reticle.transform.position, col, 1.3f, 0.5f, 0f, _reticle.transform);   // it lights the floor a little
            }
            _reticle.SetActive(true);
            _reticle.transform.position = new Vector3(c.x, c.y, 0f);
            _retSpin += Time.deltaTime * 60f;
            _retRing.transform.localRotation = Quaternion.Euler(0, 0, _retSpin);
            bool ready = _attackCd <= 0f;
            _retRing.transform.localScale = Vector3.one * (ready ? 0.55f : 0.45f);   // tightens while the attack recovers
            UnityEngine.Cursor.visible = false;
        }

        void ShowReticle(bool on)
        {
            if (_reticle != null && _reticle.activeSelf != on) _reticle.SetActive(on);
            if (!on && !UnityEngine.Cursor.visible) UnityEngine.Cursor.visible = true;
        }

        /// A projectile's lifetime so it stops at the mouse cursor (`maxLife` without the mouse, or when the cursor is further).
        float AimLife(float speed, float maxLife)
        {
            if (!DungeonInput.MouseAim) return maxLife;
            return Mathf.Clamp(AimDistance(speed * maxLife) / Mathf.Max(0.1f, speed), 0.06f, maxLife);
        }

        /// How far away the mouse cursor is (clamped), or `max` when not aiming with the mouse.
        float AimDistance(float max)
        {
            Vector2 c;
            if (DungeonInput.MouseAim && DungeonInput.Cursor(out c)) return Mathf.Clamp(Vector2.Distance(Feet, c), 1f, max);
            return max;
        }

        // ───────────────────────── skills (K / L) ─────────────────────────
        void TrySkill(MoveData m, ref float cd, string key)
        {
            if (m == null)
            {
                if (key == "L")
                    DungeonHud.Number(Body + Vector2.up * 0.8f, S.Profile.Progress(_class).level < T.secondSkillLevel
                        ? "Skill 2 opens at Lv " + T.secondSkillLevel : "No second skill equipped", "dnum--info");
                return;
            }
            if (cd > 0f) return;
            bool used = UseSkill(m);
            if (!used) return;     // e.g. Hunter's Mark with nobody to mark
            cd = SkillCooldown(m);
        }

        bool UseSkill(MoveData m)
        {
            TurnToInput();
            Vector2 dir = Aim;
            float mult = SkillMult(m);
            _curType = m.hitType; _curPips = 1; _curMove = m;
            if (IsRanged(m)) { dir = Assist(m, dir); _aim = dir; }
            MoveLight(m, Body, 1.2f, 0.6f);   // every skill lights the world…
            if (m.cooldown >= 5.5f) _dc.FloodLight(0.25f, 1.2f);   // …the big ones flood the room
            _anim.Dir = _facing;
            switch (m.kind)
            {
                // ── Ultimates (contentVersion 20) ──
                case MoveKind.Sunfall:
                {
                    Vector2 to = DungeonInput.MouseAim ? WalkTo(Feet, dir, AimDistance(m.range)) : LeapTarget(m, dir);
                    float dur = 0.45f;
                    _anim.PlayAttack(dur + 0.2f);
                    _stepVel = (to - Feet) / dur; _stepT = dur;
                    _hopT = _hopDur = dur;
                    _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.3f);
                    var sfm = m; float sfMult = mult; var sfT = _curType; Vector2 sfTo = to;
                    DungeonFx.Flash(Body, MoveColour(m), 4f, 2f, dur, transform);
                    After(dur, () =>
                    {
                        if (Vector2.Distance(Feet, sfTo) > 0.1f) Teleport(sfTo);
                        Slam(sfm, sfMult, 1, sfT, 3);
                        _furyUntil = Time.time + 6f; _furyBonus = sfm.value;
                        _dc.FloodLight(0.8f, 2.5f); GameLook.Punch(0.8f);
                    });
                    return true;
                }
                case MoveKind.Starfall:
                {
                    _anim.PlayAttack(0.4f);
                    Vector2 at = Feet + dir * AimDistance(m.range);
                    var sroom = _dc.CurrentRoom; if (sroom != null) at = sroom.NearestWalkable(at);
                    float tickMult = mult; var stT = _curType; var stm = m; Color star = new Color(1f, 0.95f, 0.65f);
                    MoveLight(m, at, 1.4f, m.duration + 0.5f); _dc.FloodLight(0.5f, m.duration);
                    DungeonZone.Spawn(_dc, at, m.radius * Reach(m, 0), m.duration, 0.25f, A(MoveColour(m), 0.25f), z =>
                    {
                        for (int i = 0; i < 4; i++)
                            DungeonFx.Spawn(DungeonFx.Arrow, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius + Vector2.up * 0.6f, star, 0.18f, 1.4f, 1.0f, -90f);
                        foreach (var e in z.Enemies())
                        {
                            if (e == null || e.Dead) continue;
                            e.Mark(stm.value * _mods.skillPower, 1.5f);
                            Hit(e, tickMult, Vector2.zero, 0f, stT, 2);
                            if (!Alive) return;
                        }
                    });
                    return true;
                }
                case MoveKind.GrandFeast:
                {
                    _anim.PlayAttack(0.45f);
                    Color fire = MoveColour(m);
                    float frad = m.radius * Reach(m, 0);
                    DungeonFx.RingAt(Body, fire, frad, 0.5f); DungeonFx.RingAt(Body, Accent, frad * 0.6f, 0.35f);
                    DungeonFx.Embers(Body, fire, 40, frad * 0.5f);
                    _dc.Shake(0.25f, 0.18f); GameLook.Punch(0.6f); _dc.FloodLight(0.6f, 2f);
                    HitArea(Body, frad, mult, Vector2.zero, 10f);
                    if (!Alive) return true;
                    foreach (var e in Combat2D.EnemiesIn(Feet, frad)) if (e != null && !e.Dead) e.Slow(0.5f, 4f);
                    int fh = S.DungeonHealPlayer(Mathf.RoundToInt(m.value * _mods.skillPower));
                    if (fh > 0) DungeonHud.Number(Body + Vector2.up * 0.8f, "+" + fh, "dnum--heal");
                    // and a great cookfire where you stand
                    var froom = _dc.CurrentRoom; Vector2 fat = froom != null ? froom.NearestWalkable(Feet) : Feet;
                    float crad = 2.8f; float life = m.duration; int perTick = Mathf.Max(1, Mathf.RoundToInt(3f * _mods.skillPower));
                    var fl = DungeonFx.Flash(fat + new Vector2(0, 0.4f), fire, crad * 2.4f, 1.8f, life);
                    DungeonController.CastShadows(fl, 0.7f, 0.45f);
                    DungeonFx.Spawn(DungeonFx.Flame, fat + new Vector2(0, 0.3f), fire, life, 1.7f, 1.3f, 0f, DungeonController.SortOrder(fat.y) + 2, true);
                    if (froom != null && froom.Lights != null) froom.Lights.IgniteNear(fat, crad);
                    DungeonZone.Spawn(_dc, fat, crad, life, 0.5f, A(fire, 0.2f), z =>
                    {
                        DungeonFx.Embers(z.Centre, fire, 1, 0.3f);
                        var pl = _dc.Player;
                        if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                        int healed = S.DungeonHealPlayer(perTick);
                        if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
                    });
                    return true;
                }
                case MoveKind.Dawn:
                {
                    _anim.PlayAttack(0.5f);
                    Color col = MoveColour(m);
                    var droom = _dc.CurrentRoom;
                    if (droom != null && droom.Lights != null) droom.Lights.IgniteWave(Feet);
                    _dc.FloodLight(1f, 3f);
                    DungeonFx.Flash(Body, col, 14f, 3f, 1.2f);
                    DungeonFx.RingAt(Body, col, 4f, 0.6f); DungeonFx.RingAt(Body, Accent, 7f, 0.8f);
                    DungeonFx.Embers(Body, col, 40, 2f);
                    _dc.Shake(0.2f, 0.15f); GameLook.Punch(0.6f);
                    _curType = HitType.Light; _curPips = Mathf.Max(1, Mathf.RoundToInt(m.value));
                    HitArea(Body, m.radius, mult, Vector2.zero, 4f, m.duration);
                    return true;
                }
                case MoveKind.Supernova:
                {
                    _anim.PlayAttack(0.5f);
                    Color col = MoveColour(m);
                    DungeonFx.Flash(Body, col, 6f, 2f, 0.4f, transform); DungeonFx.Embers(Body, col, 30, 0.6f);
                    var snm = m; float snMult = mult; var snT = _curType;
                    _invulnUntil = Mathf.Max(_invulnUntil, Time.time + 0.45f);
                    After(0.35f, () =>
                    {
                        _curType = snT; _curPips = 3; _curMove = snm;
                        float rad = snm.radius * _dc.Settings.playerReach;
                        DungeonFx.Flash(Body, col, rad * 3f, 3f, 0.9f); DungeonFx.RingAt(Body, col, rad, 0.6f); DungeonFx.RingAt(Body, Accent, rad * 0.6f, 0.4f);
                        DungeonFx.Embers(Body, col, 50, rad * 0.5f);
                        for (int i = 0; i < 24; i++) DungeonFx.Throw(Body, UnityEngine.Random.insideUnitCircle.normalized, col, 1, 4f, 12f);
                        _dc.Shake(0.35f, 0.25f); GameLook.Punch(0.9f); GameFeel.HitStop(0.1f); _dc.FloodLight(0.7f, 2f);
                        HitArea(Body, rad, snMult, Vector2.zero, 12f, snm.duration);
                    });
                    return true;
                }

                // ── Warden ──
                case MoveKind.Cleave:
                    _anim.PlayAttack(0.35f);
                    DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), m.radius); DungeonFx.RingAt(Body, A(Accent, 0.8f), m.radius * 0.6f, 0.3f);
                    _dc.Shake(0.15f, 0.12f);
                    HitArea(Body, m.radius * Reach(m, 0), mult, Vector2.zero);
                    return true;
                case MoveKind.Shockwave:
                {
                    _anim.PlayAttack(0.4f);
                    _dc.Shake(0.2f, 0.15f);
                    var origin = Body; var hitOnce = new HashSet<EnemyController>(); var swT = _curType; int swP = _curPips;
                    int steps = Mathf.Max(1, Mathf.RoundToInt(m.range));
                    for (int i = 1; i <= steps; i++)
                    {
                        Vector2 at = origin + dir * i;
                        After(0.06f * i, () =>
                        {
                            DungeonFx.RingAt(at, A(MoveColour(m), 0.85f), m.radius, 0.3f);
                            DungeonFx.Puff(at, new Color(0.7f, 0.6f, 0.45f, 0.6f), 3, 0.4f);
                            foreach (var e in Combat2D.EnemiesIn(at, m.radius * S.MutatorAreaRadius))
                            {
                                if (!hitOnce.Add(e)) continue;
                                Hit(e, mult * S.MutatorAreaDamage, dir, 7f, swT, swP);
                                if (!Alive) return;
                                if (e != null && !e.Dead) e.Stun(m.duration);
                            }
                        });
                    }
                    return true;
                }
                case MoveKind.WarCry:
                    _anim.PlayAttack(0.3f);
                    _furyUntil = Time.time + m.duration; _furyBonus = m.value;
                    DungeonFx.Flash(Body, MoveColour(m), LightRadius(m), 1.8f, m.duration, transform);   // Rally: your light flares up
                    DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), 2.4f, 0.5f); DungeonFx.Embers(Body, Accent, 16, 0.8f);
                    DungeonHud.Number(Body + Vector2.up * 0.8f, "+" + Mathf.RoundToInt(m.value * 100f) + "% damage", "dnum--info");
                    return true;
                case MoveKind.ShieldWall:
                    _anim.PlayAttack(0.25f);
                    Guard(m.value, m.duration);
                    DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), 1.3f, 0.5f);
                    return true;
                case MoveKind.Whirlwind:
                    _whirlMove = m; _whirlUntil = Time.time + m.duration; _whirlTick = 0f;
                    return true;

                case MoveKind.Sunshield:
                {
                    _anim.PlayAttack(0.3f);
                    float dur = m.duration * (1f + (m.boostShape == BoostShape.Duration ? m.boostStep * 0 : 0f));
                    Guard(m.value, dur);
                    _sunMove = m; _sunUntil = Time.time + dur; _sunLvl = 0; _sunHit.Clear();
                    if (_sunLight != null) Destroy(_sunLight.gameObject);
                    _sunLight = DungeonFx.Flash(Body + Aim * 2f, MoveColour(m), LightRadius(m) * (1f + 0.2f * 0), 2.2f, 0f, transform);
                    DungeonController.CastShadows(_sunLight, 0.7f, 0.4f);
                    DungeonFx.RingAt(Body, MoveColour(m), 1.4f, 0.4f);
                    return true;
                }
                case MoveKind.LeapSlam:
                {
                    Vector2 to = DungeonInput.MouseAim ? WalkTo(Feet, dir, AimDistance(m.range)) : LeapTarget(m, dir);   // mouse: land on the cursor
                    float dur = 0.36f;
                    _anim.PlayAttack(dur + 0.15f);
                    _stepVel = (to - Feet) / dur; _stepT = dur;
                    _hopT = _hopDur = dur;
                    _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.1f);
                    var slam = m; float slamMult = mult; int slamLvl = 0; var slamT = _curType; int slamP = _curPips;
                    DungeonFx.Puff(Feet, new Color(0.8f, 0.74f, 0.62f, 0.55f), 6, 0.5f);
                    Vector2 slamTo = to;
                    After(dur, () => { if (Vector2.Distance(Feet, slamTo) > 0.1f) Teleport(slamTo); Slam(slam, slamMult, slamLvl, slamT, slamP); });   // land exactly where it aimed
                    return true;
                }

                // ── Ranger ──
                case MoveKind.Volley:
                    _anim.PlayAttack(0.3f);
                    Arrow(mult, Quaternion.Euler(0, 0, -7f) * dir, 13f, true);
                    After(0.09f, () => Arrow(mult, Quaternion.Euler(0, 0, 7f) * Aim, 13f, true));
                    return true;
                case MoveKind.Trap:
                {
                    _anim.PlayAttack(0.25f);
                    var trapMove = m; float trapMult = mult; var trT = _curType; int trP = _curPips;
                    DungeonZone.Spawn(_dc, Feet, m.radius, m.value, 0f, A(MoveColour(m), 0.45f), z =>
                    {
                        var hit = z.Enemies();
                        if (hit.Count == 0) return;
                        var e = hit[0];
                        z.End();
                        DungeonFx.RingAt(z.Centre, A(MoveColour(trapMove), 0.9f), trapMove.radius * 1.3f, 0.35f);
                        Hit(e, trapMult, Vector2.zero, 0f, trT, trP);
                        if (e != null && !e.Dead) e.Root(trapMove.duration);
                    });
                    return true;
                }
                case MoveKind.Mark:
                {
                    var target = Combat2D.Nearest(Feet, m.range);
                    if (target == null) { DungeonHud.Number(Body + Vector2.up * 0.8f, "No target", "dnum--info"); return false; }
                    _anim.PlayAttack(0.25f);
                    target.Mark(m.value * _mods.skillPower, m.duration);
                    return true;
                }
                case MoveKind.ArrowRain:
                {
                    _anim.PlayAttack(0.3f);
                    Vector2 at = Feet + dir * AimDistance(m.range);   // mouse: on the cursor
                    float tickMult = mult; var rnT = _curType; int rnP = _curPips;
                    MoveLight(m, at, 1f, m.duration + 0.3f);   // the storm lights the whole area
                    Color rainCol = MoveColour(m);
                    DungeonZone.Spawn(_dc, at, m.radius * Reach(m, 0), m.duration, 0.25f, A(rainCol, 0.22f), z =>
                    {
                        for (int i = 0; i < 3; i++)
                            DungeonFx.Spawn(DungeonFx.Arrow, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius + Vector2.up * 0.6f, Color.Lerp(Color.white, rainCol, 0.9f), 0.18f, 1.2f, 1.0f, -90f);
                        foreach (var e in z.Enemies()) { Hit(e, tickMult, Vector2.zero, 0f, rnT, rnP); if (!Alive) return; }
                    });
                    return true;
                }
                case MoveKind.Smoke:
                    _anim.PlayAttack(0.2f);
                    DungeonFx.Puff(Body, A(Color.Lerp(new Color(0.7f, 0.7f, 0.72f), MoveColour(m), 0.45f), 0.7f), 16, m.radius * 0.55f);
                    DungeonFx.Flash(Body, MoveColour(m), m.radius * 1.7f, 1.0f, 0.6f);
                    foreach (var e in Combat2D.EnemiesIn(Feet, m.radius)) e.Slow(1f - m.value, m.duration);
                    _hasteUntil = Time.time + m.duration;
                    return true;

                case MoveKind.EmberArrow:
                {
                    _anim.PlayAttack(0.3f);
                    int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * 0) : 0);
                    float sp = m.spread > 0f ? m.spread : 10f;
                    var em = m; float burn = SkillMult(m) * m.value;
                    for (int i = 0; i < n; i++)
                    {
                        var p = Arrow(mult, Quaternion.Euler(0, 0, (i - (n - 1) * 0.5f) * sp) * dir, m.range, false, 1.5f);
                        if (p == null) continue;
                        p.SetLife(AimLife(m.range, 0.7f));   // mouse: lands where you clicked
                        p.OnStop = at => EmberLand(em, at, burn);
                    }
                    return true;
                }
                case MoveKind.FlareArrow:
                {
                    _anim.PlayAttack(0.3f);
                    var p = Arrow(mult, dir, m.range, false, 1.5f);
                    if (p == null) return true;
                    var fm = m; float fmult = mult; int flvl = 0; int fpips = _curPips;
                    p.Tint(Color.Lerp(Color.white, MoveColour(m), 0.9f)).SetLife(AimLife(m.range, 0.6f));   // mouse: bursts where you clicked
                    p.OnStop = at => FlareBurst(fm, at, fmult, flvl, fpips);
                    return true;
                }
                case MoveKind.Spotlight:
                {
                    var target = Combat2D.Nearest(Feet, m.range);
                    if (target == null) { DungeonHud.Number(Body + Vector2.up * 0.8f, "No target", "dnum--info"); return false; }
                    _anim.PlayAttack(0.25f);
                    DungeonFx.Flash(target.Body, MoveColour(m), 2.6f, 2.2f, m.duration, target.transform);   // it's lit: a weak spot
                    target.Mark(m.value * _mods.skillPower, m.duration);
                    DungeonHud.Number(target.Body + Vector2.up * 0.9f, "Spotted!", "dnum--info");
                    return true;
                }

                // ── Lamplighter ──
                case MoveKind.Lantern:
                {
                    _anim.PlayAttack(0.3f);
                    int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * 0) : 0);
                    for (int i = 0; i < n; i++)
                    {
                        float ang = n == 1 ? 0f : (i - (n - 1) * 0.5f) * 50f;
                        Vector2 at = Feet + (Vector2)(Quaternion.Euler(0, 0, ang) * dir) * 1.2f + new Vector2(0, 0.9f);
                        HoverLantern.Place(_dc, at, m, mult, _curPips, MoveColour(m), LightRadius(m));
                    }
                    return true;
                }
                case MoveKind.Sunbeam:
                    _anim.PlayAttack(m.duration);
                    StartBeam(m, mult, 1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f), m.duration);
                    return true;
                case MoveKind.Prism:
                {
                    _anim.PlayAttack(0.3f);
                    dir = Assist(m, dir); _aim = dir;
                    var p = Projectile.Spawn(_dc, true, Body + dir * 0.4f, dir, m.range, mult, 0f, DungeonFx.Orb, MoveColour(m), false, 1.3f);
                    if (p == null) return true;
                    var pm = m; float pmult = mult; int plvl = 0;
                    p.SetHit(HitType.Light, _curPips); p.SetLife(AimLife(m.range, 1.1f));
                    p.OnStop = at => PrismBurst(pm, at, pmult, plvl);
                    return true;
                }
                case MoveKind.Kindle:
                {
                    _anim.PlayAttack(0.35f);
                    var room = _dc.CurrentRoom; int braziers = 0;
                    if (room != null && room.Lights != null) { foreach (var b in room.Lights.Braziers) if (b != null && !b.Puzzle) braziers++; room.Lights.IgniteWave(Feet); }
                    _dc.FloodLight(0.6f, 2.5f);
                    DungeonFx.RingAt(Body, MoveColour(m), 3f, 0.6f);
                    DungeonFx.Embers(Body, MoveColour(m), 30, 1.5f);
                    int heal = Mathf.Min(12, Mathf.RoundToInt(braziers * m.value * _mods.skillPower));
                    if (heal > 0) { int healed = S.DungeonHealPlayer(heal); if (healed > 0) DungeonHud.Number(Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal"); }
                    return true;
                }

                // ── Emberfist ──
                case MoveKind.Phoenix:
                {
                    _anim.PlayAttack(0.4f);
                    dir = Assist(m, dir); _aim = dir;
                    float dist = m.range * (1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f));
                    Vector2 to = WalkTo(Feet, dir, dist);
                    float dur = 0.28f;
                    _stepVel = (to - Feet) / dur; _stepT = dur;
                    _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.1f);
                    _phoenixMove = m; _phoenixT = dur; _phoenixMult = mult; _phoenixLvl = 0; _phoenixHit.Clear();
                    _phoenixType = _curType; _phoenixPips = _curPips; _sweepFrom = Body;
                    return true;
                }
                case MoveKind.FlameWheel:
                {
                    _anim.PlayAttack(0.35f);
                    float rad = m.radius * Reach(m, 0); var wm = m; float wmult = mult; var wt = _curType; int wp = _curPips; Color col = MoveColour(m);
                    for (int i = 0; i < Mathf.Max(1, m.count); i++)
                        After(0.001f + i * 0.18f, () => { _curType = wt; _curPips = wp; _curMove = wm; DungeonFx.RingAt(Body, col, rad, 0.3f); DungeonFx.Throw(Body, Vector2.up, col, 8, 3f, 7f); MoveLight(wm, Body, 1.2f, 0.3f); HitArea(Body, rad, wmult, Vector2.zero, 7f); });
                    return true;
                }
                case MoveKind.Detonate:
                {
                    _anim.PlayAttack(0.35f);
                    const float h = 0.6f;   // Heat was cut in Design v2: a fixed-size blast (what 60 Heat used to give)
                    float rad = (m.radius + h * m.range) * _dc.Settings.playerReach;
                    float dmult = mult * (0.5f + h * m.value);
                    Color col = MoveColour(m);
                    DungeonFx.Flash(Body, col, rad * 3f, 2.6f, 0.8f); DungeonFx.RingAt(Body, col, rad, 0.5f); DungeonFx.RingAt(Body, Color.white, rad * 0.6f, 0.35f);
                    DungeonFx.Embers(Body, col, 40, rad * 0.5f);
                    for (int i = 0; i < 20; i++) DungeonFx.Throw(Body, UnityEngine.Random.insideUnitCircle.normalized, col, 1, 4f, 10f);
                    _dc.Shake(0.3f, 0.2f); GameLook.Punch(0.8f); GameFeel.HitStop(0.08f); _dc.FloodLight(0.5f, 1.5f);
                    DungeonHud.Number(Body + Vector2.up * 1f, "Detonate!", "dnum--crit");
                    HitArea(Body, rad, dmult, Vector2.zero, 11f, 0.4f);
                    return true;
                }
                case MoveKind.Counter:
                {
                    _anim.PlayAttack(0.2f);
                    float dur = m.duration * (1f + (m.boostShape == BoostShape.Duration ? m.boostStep * 0 : 0f));
                    _counterMove = m; _counterUntil = Time.time + dur; _counterMult = mult;
                    DungeonFx.RingAt(Body, MoveColour(m), 1f, dur);
                    DungeonFx.Flash(Body, MoveColour(m), 3f, 1.4f, dur, transform);
                    return true;
                }

                // ── Cook ──
                case MoveKind.Snack:
                {
                    _anim.PlayAttack(0.3f);
                    int healed = S.DungeonHealPlayer(Mathf.RoundToInt(m.value * _mods.skillPower));
                    if (healed > 0) DungeonHud.Number(Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
                    Guard(T.guardDamageMult, m.duration);
                    DungeonFx.RingAt(Body, new Color(0.6f, 1f, 0.6f, 0.8f), 1.2f);
                    return true;
                }
                case MoveKind.PepperToss:
                    _anim.PlayAttack(0.3f);
                    MoveLight(m, Body + dir * m.range * 0.6f, 1.4f, 0.5f);   // Flambé: a huge burst of firelight
                    DungeonFx.Throw(Body + dir * 0.5f, dir, MoveColour(m), 10, 3f, 7f);
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 d = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-m.radius, m.radius)) * dir;
                        var fx = DungeonFx.Spawn(DungeonFx.Circle, Body + d * 0.4f, A((i & 1) == 0 ? MoveColour(m) : Accent, 0.8f), 0.4f, 0.35f, 0.15f);
                        fx.velocity = d * UnityEngine.Random.Range(m.range * 1.5f, m.range * 2.6f);
                    }
                    foreach (var e in Combat2D.EnemiesInCone(Body, dir, m.range * S.MutatorAreaRadius, m.radius))
                    {
                        Hit(e, mult * S.MutatorAreaDamage, dir, 3f);
                        if (!Alive) return true;
                        if (e != null && !e.Dead) e.Slow(1f - m.value, m.duration);
                    }
                    return true;
                case MoveKind.HotPot:
                {
                    _anim.PlayAttack(0.3f);
                    Vector2 at = Feet + dir * AimDistance(m.range);   // mouse: on the cursor
                    var pot = DungeonFx.Spawn(DungeonFx.Circle, Body, new Color(0.55f, 0.4f, 0.3f, 1f), 0.35f, 0.7f, 0.7f);
                    pot.velocity = (at - Body) * 6f;   // FxSprite damps velocity by ~6/s, so this lands near the target
                    var potMove = m; float potMult = mult; var ptT = _curType; int ptP = _curPips; float potK = Reach(m, 0);   // Boost: a bigger pool
                    After(0.35f, () =>
                    {
                        DungeonFx.Puff(at, A(MoveColour(potMove), 0.8f), 8, 1f);
                        DungeonFx.Flash(at, MoveColour(potMove), potMove.radius * potK * 2.4f, 1.3f, potMove.duration);   // the pool glows like a fire
                        Color potCol = MoveColour(potMove), potAcc = Accent;
                        DungeonZone.Spawn(_dc, at, potMove.radius * potK, potMove.duration, potMove.value, A(potCol, 0.35f), z =>
                        {
                            DungeonFx.Spawn(DungeonFx.Circle, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius, A(potAcc, 0.7f), 0.3f, 0.3f, 0.05f);
                            foreach (var e in z.Enemies()) { Hit(e, potMult, Vector2.zero, 0f, ptT, ptP); if (!Alive) return; }
                        });
                    });
                    return true;
                }
                case MoveKind.Picnic:
                {
                    _anim.PlayAttack(0.3f);
                    int perTick = Mathf.Max(1, Mathf.RoundToInt(m.value * _mods.skillPower));
                    DungeonZone.Spawn(_dc, Feet, m.radius, m.duration, 0.5f, new Color(1f, 0.55f, 0.6f, 0.3f), z =>
                    {
                        var pl = _dc.Player;
                        if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                        int healed = S.DungeonHealPlayer(perTick);
                        if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
                    });
                    return true;
                }
                case MoveKind.SkewerToss:
                    _anim.PlayAttack(0.3f);
                    dir = Assist(m, dir); _aim = dir;
                    Fan(m, mult, dir, m.range, false, 1.3f, 0);
                    return true;
                case MoveKind.Cookfire:
                {
                    _anim.PlayAttack(0.3f);
                    var room = _dc.CurrentRoom;
                    Vector2 at = Feet + dir * 0.9f;
                    if (room != null) at = room.NearestWalkable(at);
                    float k = 1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f);
                    float rad = m.radius * k;
                    int perTick = Mathf.Max(1, Mathf.RoundToInt(m.value * _mods.skillPower * k));
                    Color fire = MoveColour(m);
                    var l = DungeonFx.Flash(at + new Vector2(0, 0.4f), fire, rad * 2.4f, 1.6f, m.duration);   // a real light source: light-shy monsters keep away
                    DungeonController.CastShadows(l, 0.7f, 0.45f);
                    DungeonFx.Spawn(DungeonFx.Flame, at + new Vector2(0, 0.3f), fire, m.duration, 1.3f * k, 1.0f * k, 0f, DungeonController.SortOrder(at.y) + 2, true);
                    DungeonFx.Embers(at, fire, 14, 0.4f);
                    if (room != null && room.Lights != null) room.Lights.IgniteNear(at, rad);
                    DungeonZone.Spawn(_dc, at, rad, m.duration, 0.5f, new Color(1f, 0.55f, 0.25f, 0.18f), z =>
                    {
                        DungeonFx.Embers(z.Centre, fire, 1, 0.25f);
                        var pl = _dc.Player;
                        if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                        int healed = S.DungeonHealPlayer(perTick);
                        if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
                    });
                    return true;
                }
                case MoveKind.RollingPin:
                {
                    _anim.PlayAttack(0.3f);
                    var p = Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, m.value, mult, 0f, DungeonFx.Arrow, Color.Lerp(new Color(0.85f, 0.65f, 0.45f), MoveColour(m), 0.5f), true, 2.2f);
                    if (p != null) DungeonFx.Flash(p.transform.position, MoveColour(m), LightRadius(m) * 0.6f, 1.0f, 0f, p.transform);   // the pin glows as it flies
                    if (p != null) { p.Boomerang(m.range, 900f); p.SetHit(_curType, _curPips); }
                    return true;
                }
            }
            return false;
        }

        void Guard(float damageMult, float seconds)
        {
            _guardMult = Mathf.Min(_guardUntil > 0f ? _guardMult : 1f, damageMult);
            _guardUntil = Mathf.Max(_guardUntil, Time.time + seconds);
            S.DungeonGuardMult = _guardMult;
        }

        void WhirlUpdate(float dt)
        {
            if (_whirlMove == null) return;
            if (Time.time >= _whirlUntil) { _whirlMove = null; return; }
            _whirlTick -= dt;
            if (_whirlTick > 0f) return;
            _whirlTick = Mathf.Max(0.1f, _whirlMove.value);
            _anim.PlayAttack(0.2f);
            _curType = _whirlMove.hitType; _curPips = 1; _curMove = _whirlMove;
            MoveLight(_whirlMove, Body, 0.8f, 0.25f);
            DungeonFx.RingAt(Body, A(MoveColour(_whirlMove), 0.75f), _whirlMove.radius, 0.2f); MoveLight(_whirlMove, Body, 1f, 0.25f);
            HitArea(Body, _whirlMove.radius, SkillMult(_whirlMove), Vector2.zero, 4f);
        }

        // class colour themes (contentVersion 21), in ClassId order: Warden, Ranger, Cook, Lamplighter, Emberfist
        static readonly Color[] ClassLight = { new Color(1f, 0.72f, 0.05f), new Color(0.05f, 0.92f, 0.45f), new Color(1f, 0.5f, 0f), new Color(0.18f, 0.62f, 1f), new Color(1f, 0.23f, 0.23f) };
        static readonly Color[] ClassAccent = { new Color(1f, 0.55f, 0.05f), new Color(0f, 0.85f, 0.85f), new Color(1f, 0.82f, 0f), new Color(0.65f, 0.3f, 1f), new Color(1f, 0.37f, 0.66f) };
        /// A class's main colour (its carried light is tinted by it too).
        public static Color ClassColour(ClassId c) { int i = (int)c; return i >= 0 && i < ClassLight.Length ? ClassLight[i] : new Color(1f, 0.8f, 0.5f); }
        public ClassId ClassId { get { return _class; } }
        /// The class's second colour: sparks, rings and trails around the move's own colour.
        Color Accent { get { int i = (int)_class; return i >= 0 && i < ClassAccent.Length ? ClassAccent[i] : Color.white; } }
        static Color A(Color c, float a) { c.a = a; return c; }

        Color MoveColour(MoveData m)
        {
            Color c;
            if (m != null && !string.IsNullOrEmpty(m.lightColour) && ColorUtility.TryParseHtmlString(m.lightColour.StartsWith("#") ? m.lightColour : "#" + m.lightColour, out c)) return c;
            int i = (int)_class;
            return i >= 0 && i < ClassLight.Length ? ClassLight[i] : new Color(1f, 0.8f, 0.5f);
        }

        /// Move lights are kept softer than white light: bright coloured lights add up to white, so less intensity = more colour.
        const float ColourLightK = 0.6f;

        float LightRadius(MoveData m) { return m.lightRadius > 0f ? m.lightRadius : Mathf.Clamp((m.radius + Mathf.Min(m.range, 4f)) * 1.6f, 2.5f, 9f); }

        /// Every move lights the world: a flash of the move's own light at <paramref name="at"/>.
        void MoveLight(MoveData m, Vector2 at, float scale = 1f, float life = 0.35f)
        {
            if (m == null) return;
            DungeonFx.Flash(at, MoveColour(m), LightRadius(m) * scale * 1.15f, (m.lightIntensity > 0f ? m.lightIntensity : 1.4f) * ColourLightK, life);
        }

        /// Hit-area size: the player's bigger reach, plus Radius Boosts.
        float Reach(MoveData m, int lvl) { return _dc.Settings.playerReach * (1f + (m.boostShape == BoostShape.Radius ? m.boostStep * lvl : 0f)); }

        static bool IsRanged(MoveData m)
        {
            return m.kind == MoveKind.Arrow || m.kind == MoveKind.SpreadArrow || m.kind == MoveKind.PierceArrow || m.kind == MoveKind.Volley || m.kind == MoveKind.RollingPin || m.kind == MoveKind.Vault
                || m.kind == MoveKind.EmberArrow || m.kind == MoveKind.FlareArrow || m.kind == MoveKind.SkewerToss
                || m.kind == MoveKind.Wisp || m.kind == MoveKind.Prism;
        }

        /// Aim assist: snap to the nearest enemy inside the move's cone and reach.
        Vector2 Assist(MoveData m, Vector2 dir)
        {
            float cone = m.aimAssist >= 0f ? m.aimAssist : IsRanged(m) ? _dc.Settings.rangedAimAssist : _dc.Settings.meleeAimAssist;
            if (DungeonInput.MouseAim) cone *= _dc.Settings.mouseAimAssist;   // the cursor already aims
            if (cone <= 0f) return dir;
            float reach = IsRanged(m) ? Mathf.Min(m.range, 14f) : m.range + m.radius + 0.6f;
            EnemyController best = null; float bd = reach;
            foreach (var e in Combat2D.EnemiesIn(Body, reach))
            {
                if (e == null || e.Dead) continue;
                Vector2 to = e.Body - Body; float d = to.magnitude;
                if (d < 0.01f || Vector2.Angle(dir, to) > cone * 0.5f) continue;
                if (d < bd) { bd = d; best = e; }
            }
            return best != null ? (best.Body - Body).normalized : dir;
        }

        /// Looses the move's arrows in a fan (its count, + more with a Count Boost); each one lit, maybe homing.
        void Fan(MoveData m, float mult, Vector2 dir, float speed, bool pierce, float scale, int boostLvl)
        {
            int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * boostLvl) : 0);
            float sp = m.spread > 0f ? m.spread : (m.value > 0f ? m.value : 8f);
            for (int i = 0; i < n; i++) Arrow(mult, Quaternion.Euler(0, 0, (i - (n - 1) * 0.5f) * sp) * dir, speed, pierce, scale);
        }

        /// One hit of a combo chain: quick flaming arcs, then a bigger finisher (its second hit type) that stuns.
        void ComboHit(MoveData m, bool finisher, Vector2 dir, float mult, int lvl, int pips)
        {
            _curType = finisher ? m.hitType2 : m.hitType; _curPips = pips; _curMove = m;
            float rad = m.radius * Reach(m, lvl) * (finisher ? 1.25f : 1f);
            Vector2 at = Body + dir * m.range;
            Color col = MoveColour(m);
            if (finisher)
            {
                DungeonFx.SlashAt(Body + dir * 0.3f, dir, A(Accent, 1f), 2.0f);
                DungeonFx.Flash(at, col, LightRadius(m) * 1.5f, 1.4f, 0.4f);
                DungeonFx.RingAt(at, col, rad, 0.3f);
                DungeonFx.Sparks(at, dir, col, 10);
                MoveLight(m, at, 1.6f, 0.4f);
                _dc.Shake(0.1f, 0.1f);
                HitArea(at, rad, mult * Mathf.Max(1f, m.value), dir, 9f, m.duration);
            }
            else
            {
                DungeonFx.SlashAt(Body + dir * 0.25f, Quaternion.Euler(0, 0, _comboStep % 2 == 0 ? 25f : -25f) * dir, col, 1.35f);
                DungeonFx.Throw(at, dir, col, 3, 2f, 5f);
                MoveLight(m, at, 1.1f, 0.25f);
                HitArea(at, rad, mult, dir, 5f);
            }
        }

        /// Dawnbreaker lands: a ring of Light round you (Boost: a second, wider shockwave).
        void Slam(MoveData m, float mult, int lvl, HitType type, int pips)
        {
            if (!Alive) return;
            _curType = type; _curPips = pips; _curMove = m;
            float rad = m.radius * Reach(m, lvl);
            Color col = MoveColour(m);
            DungeonFx.RingAt(Body, col, rad, 0.45f);
            DungeonFx.Flash(Body, col, LightRadius(m) * (1f + 0.2f * lvl), (m.lightIntensity > 0f ? m.lightIntensity : 1.4f) * 1.3f, 0.7f);
            DungeonFx.Embers(Feet, col, 24, 1f); DungeonFx.Sparks(Body, Vector2.up, col, 14);
            DungeonFx.Puff(Feet, new Color(0.75f, 0.68f, 0.55f, 0.6f), 10, rad * 0.6f);
            _dc.Shake(0.22f, 0.16f); GameLook.Punch(0.5f); _dc.FloodLight(0.3f + 0.1f * lvl, 1.2f);
            HitArea(Body, rad, mult, Vector2.zero, 9f, m.duration);
            if (!Alive || lvl <= 0) return;
            Vector2 c = Body;
            After(0.25f, () => { _curType = type; _curPips = 1; DungeonFx.RingAt(c, col, rad * 1.6f, 0.4f); DungeonFx.Flash(c, col, LightRadius(m) * 1.3f, 1.4f, 0.4f); HitArea(c, rad * 1.6f, mult * 0.6f, Vector2.zero, 6f); });
        }

        /// Where Dawnbreaker lands: on the nearest enemy ahead if there is one, otherwise full distance; never through a wall.
        Vector2 LeapTarget(MoveData m, Vector2 dir)
        {
            EnemyController best = null; float bd = m.range + 0.5f;
            foreach (var e in Combat2D.EnemiesIn(Feet, m.range + 0.5f))
            {
                if (e == null || e.Dead) continue;
                Vector2 to = (Vector2)e.transform.position - Feet;
                if (Vector2.Angle(dir, to) > 35f) continue;
                if (to.magnitude < bd) { bd = to.magnitude; best = e; }
            }
            Vector2 goal = Feet + dir * m.range;
            if (best != null) { Vector2 ep = best.transform.position; goal = ep - (ep - Feet).normalized * 0.6f; }
            var room = _dc.CurrentRoom; Vector2 d = goal - Feet; float len = d.magnitude;
            if (len < 0.01f || room == null) return Feet;
            d /= len; Vector2 last = Feet;
            for (float s = 0.25f; s <= len; s += 0.25f)
            {
                Vector2 p = Feet + d * s;
                var cell = new Vector3Int(Mathf.FloorToInt(p.x - room.Origin.x), Mathf.FloorToInt(p.y - room.Origin.y), 0);
                if (!room.IsWalkable(cell)) break;
                last = p;
            }
            return last;
        }

        /// Sunshield, each frame: the light cone follows your aim, lights (and Light-hits) enemies in it, and bounces shots back.
        void SunUpdate()
        {
            if (_sunMove == null) return;
            if (Time.time >= _sunUntil) { if (_sunLight != null) Destroy(_sunLight.gameObject); _sunLight = null; _sunMove = null; return; }
            var m = _sunMove;
            if (_sunLight != null) _sunLight.transform.position = Body + Aim * 2f;
            foreach (var e in Combat2D.EnemiesInCone(Body, Aim, m.range * (1f + 0.2f * _sunLvl), m.radius))
                if (e != null && !e.Dead && _sunHit.Add(e)) e.TypedHit(HitType.Light, 1 + _sunLvl);
            foreach (var p in UnityEngine.Object.FindObjectsByType<Projectile>())
            {
                if (p == null || p.FromPlayer) continue;
                Vector2 to = (Vector2)p.transform.position - Body;
                if (to.magnitude < 1.5f && Vector2.Angle(Aim, to) < 75f) { p.Reflect(_mods.attackPower * DamageBuff); DungeonFx.Sparks(p.transform.position, -to, MoveColour(m), 6); }
            }
        }

        /// Ember Arrow lands: it sticks and burns like a thrown torch, scorching anything standing there.
        void EmberLand(MoveData m, Vector2 at, float burnMult)
        {
            if (!Alive) return;
            ThrownTorch.Throw(_dc, at, Vector2.down, 0f, m.duration);
            DungeonFx.Sparks(at, Vector2.up, MoveColour(m), 8);
            DungeonZone.Spawn(_dc, at, m.radius, m.duration, 0.5f, A(MoveColour(m), 0.22f), z =>
            {
                foreach (var e in z.Enemies()) { Hit(e, burnMult, Vector2.zero, 0f, HitType.Fire, 1); if (!Alive) return; }
            });
        }

        /// Flare Arrow bursts: a ball of light that hits (Light), stuns shadow creatures, shows weaknesses and secrets.
        void FlareBurst(MoveData m, Vector2 at, float mult, int lvl, int pips)
        {
            if (!Alive) return;
            float rad = m.radius * Reach(m, lvl);
            Color col = MoveColour(m);
            DungeonFx.Flash(at, col, LightRadius(m) * (1f + 0.25f * lvl), m.lightIntensity > 0f ? m.lightIntensity : 2f, 2.5f);
            DungeonFx.RingAt(at, col, rad, 0.45f); DungeonFx.Embers(at, col, 20, 0.6f); GameLook.Punch(0.3f);
            _dc.FloodLight(0.2f, 1f);
            var room = _dc.CurrentRoom;
            if (room != null) { if (room.Lights != null) room.Lights.IgniteNear(at, rad); if (room.Secrets != null) room.Secrets.Reveal(at, LightRadius(m), 6f); }
            foreach (var e in Combat2D.EnemiesIn(at, rad * 1.5f))
            {
                if (e == null || e.Dead) continue;
                if (e.Shadow) e.Stun(m.duration);
            }
            _curType = HitType.Light; _curPips = pips; _curMove = m;
            HitArea(at, rad, mult, Vector2.zero, 5f);
        }

        /// The Lamplighter's wisps: glowing motes in a fan that home in on enemies.
        void Motes(MoveData m, float mult, Vector2 dir, int boostLvl)
        {
            int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * boostLvl) : 0);
            float sp = m.spread > 0f ? m.spread : 15f; Color col = MoveColour(m);
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Quaternion.Euler(0, 0, (i - (n - 1) * 0.5f) * sp) * dir;
                var p = Projectile.Spawn(_dc, true, Body + d * 0.4f, d, m.range, mult, 0f, DungeonFx.Orb, col, m.value > 0f, 0.9f);   // value > 0: pierces (Light Lance)
                if (p == null) continue;
                p.SetHit(_curType, _curPips);
                if (m.homing > 0f) p.Homing(m.homing, 10f);
            }
        }

        /// The furthest open floor along a line (stops before walls and pits).
        Vector2 WalkTo(Vector2 from, Vector2 dir, float len)
        {
            var room = _dc.CurrentRoom;
            if (room == null || dir.sqrMagnitude < 0.01f) return from;
            dir = dir.normalized; Vector2 last = from;
            for (float s = 0.25f; s <= len; s += 0.25f)
            {
                Vector2 p = from + dir * s;
                var c = new Vector3Int(Mathf.FloorToInt(p.x - room.Origin.x), Mathf.FloorToInt(p.y - room.Origin.y), 0);
                if (!room.IsWalkable(c)) break;
                last = p;
            }
            return last;
        }

        void StartBeam(MoveData m, float mult, float k, float seconds)
        {
            _beamMove = m; _beamMult = mult; _beamK = k; _beamUntil = Time.time + seconds; _beamTick = 0f;
            _beamType = _curType; _beamPips = _curPips;
            if (_beamSr == null)
            {
                var g = new GameObject("Sunbeam"); g.transform.SetParent(transform, false);
                _beamSr = g.AddComponent<SpriteRenderer>(); _beamSr.sprite = DungeonFx.Pixel; _beamSr.sortingOrder = 21000;
                if (DungeonFx.GlowMat != null) _beamSr.sharedMaterial = DungeonFx.GlowMat;
                _beamLights = new UnityEngine.Rendering.Universal.Light2D[3];
                for (int i = 0; i < 3; i++) _beamLights[i] = DungeonFx.Flash(Body, MoveColour(m), LightRadius(m), 1.6f, 0f, g.transform);
            }
            _beamSr.gameObject.SetActive(true);
            _dc.FloodLight(0.15f, seconds);
        }

        /// Sunbeam, each frame: a line of light along your aim (stopping at walls, passing over pits) that hits everything in it every tick.
        void BeamUpdate(float dt)
        {
            if (_beamMove == null) return;
            var m = _beamMove;
            if (Time.time >= _beamUntil) { _beamMove = null; if (_beamSr != null) _beamSr.gameObject.SetActive(false); return; }
            float len = m.range * _beamK, half = Mathf.Max(0.2f, m.radius * _beamK);
            Vector2 a = Body + Aim * 0.3f;
            var room = _dc.CurrentRoom; var hazard = _dc.Theme != null ? _dc.Theme.hazard : null;
            if (room != null && room.Template.walls != null)
                for (float s = 0.3f; s <= len; s += 0.25f)
                {
                    Vector2 p = Body + Aim * s;
                    var t = room.Template.walls.GetTile(new Vector3Int(Mathf.FloorToInt(p.x - room.Origin.x), Mathf.FloorToInt(p.y - room.Origin.y), 0));
                    if (t != null && t != hazard) { len = s; break; }
                }
            Color col = MoveColour(m);
            var b = DungeonFx.Pixel.bounds.size;
            float flick = 1f + 0.12f * Mathf.Sin(Time.time * 40f);
            _beamSr.transform.position = a + Aim * (len * 0.5f);
            _beamSr.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(Aim.y, Aim.x) * Mathf.Rad2Deg);
            _beamSr.transform.localScale = new Vector3(len / b.x, half * 2f * flick / b.y, 1f);
            _beamSr.color = new Color(col.r, col.g, col.b, 0.75f);
            for (int i = 0; i < _beamLights.Length; i++) if (_beamLights[i] != null) _beamLights[i].transform.position = a + Aim * (len * (i + 0.5f) / _beamLights.Length);
            _beamTick -= dt;
            if (_beamTick > 0f) return;
            _beamTick = Mathf.Max(0.05f, m.value);
            var hitNow = new HashSet<EnemyController>();
            for (float s = 0.3f; s <= len; s += half)
                foreach (var e in Combat2D.EnemiesIn(a + Aim * s, half))
                    if (e != null && !e.Dead && hitNow.Add(e)) { Hit(e, _beamMult, Aim, 1.5f, _beamType, _beamPips); if (!Alive) return; }
            IgniteNear(a + Aim * len, half + 0.3f);
            DungeonFx.Embers(a + Aim * len, col, 2, 0.2f);
        }

        /// Prism lands: it strikes its target with every one of its weaknesses at once, and splashes Light round about.
        void PrismBurst(MoveData m, Vector2 at, float mult, int lvl)
        {
            if (!Alive) return;
            EnemyController target = null; float bd = 1.3f;
            foreach (var e in Combat2D.EnemiesIn(at, 1.3f)) { if (e == null || e.Dead) continue; float d = Vector2.Distance(e.Body, at); if (d < bd) { bd = d; target = e; } }
            Color[] rainbow = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(0.45f, 0.7f, 1f), new Color(0.8f, 0.5f, 1f) };
            for (int i = 0; i < rainbow.Length; i++) DungeonFx.Throw(at, Quaternion.Euler(0, 0, i * 72f) * Vector2.up, rainbow[i], 3, 3f, 6f);
            DungeonFx.Flash(at, MoveColour(m), LightRadius(m), 2f, 0.5f);
            DungeonFx.RingAt(at, MoveColour(m), 1.2f, 0.35f);
            if (target != null) target.TypedHit(HitType.Light, 1, true);   // ticket 06: Prism strikes with the target's weakest element
            _curType = HitType.Light; _curPips = 1; _curMove = m;
            HitArea(at, m.radius * Reach(m, lvl), mult * 0.5f, Vector2.zero, 3f);
        }

        /// Enemies along the path the body travelled since last frame (a capsule), so fast moves never skip past anyone.
        List<EnemyController> Swept(float radius)
        {
            var list = new List<EnemyController>();
            Vector2 a = _sweepFrom, b = Body;
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / Mathf.Max(0.1f, radius * 0.6f)));
            for (int i = 0; i <= steps; i++)
                foreach (var e in Combat2D.EnemiesIn(Vector2.Lerp(a, b, i / (float)steps), radius))
                    if (!list.Contains(e)) list.Add(e);
            _sweepFrom = b;
            return list;
        }

        /// Rising Phoenix in flight: hits everything you rush through, then a flaming uppercut where you stop.
        void PhoenixUpdate(float dt)
        {
            if (_phoenixMove == null) return;
            var m = _phoenixMove;
            _phoenixT -= dt;
            Color col = MoveColour(m);
            DungeonFx.Ghost(_anim.Renderer, A(col, 0.5f), 0.25f, true, false);
            DungeonFx.Embers(Feet, Accent, 3, 0.25f);
            _curType = _phoenixType; _curPips = _phoenixPips; _curMove = m;
            foreach (var e in Swept(0.9f * _dc.Settings.playerReach))
                if (e != null && !e.Dead && _phoenixHit.Add(e)) { Hit(e, _phoenixMult, _stepVel.sqrMagnitude > 0.01f ? _stepVel.normalized : Aim, 4f); if (!Alive) return; }
            if (_phoenixT > 0f) return;
            _phoenixMove = null;
            float rad = m.radius * Reach(m, _phoenixLvl);
            DungeonFx.Flash(Body, col, LightRadius(m) * 1.2f, 2.2f, 0.6f); DungeonFx.RingAt(Body, col, rad, 0.4f);
            for (int i = 0; i < 12; i++) DungeonFx.Throw(Body, (Vector2.up + UnityEngine.Random.insideUnitCircle * 0.6f).normalized, col, 1, 3f, 9f);
            _dc.Shake(0.15f, 0.12f); _dc.FloodLight(0.25f, 0.8f);
            HitArea(Body, rad, _phoenixMult * 1.4f, Vector2.up, 8f, m.duration);
            if (!Alive) return;
            float burn = _phoenixMult * 0.2f;
            DungeonZone.Spawn(_dc, Feet, 0.9f, 2f, 0.5f, A(col, 0.25f), z => { foreach (var e in z.Enemies()) { Hit(e, burn, Vector2.zero, 0f, HitType.Fire, 1); if (!Alive) return; } });
        }

        /// Cinder Step: an attack lands during the stance. No damage; you strike back in a burst of fire (+20 Heat).
        void Counter(Vector2 from)
        {
            var m = _counterMove; _counterUntil = 0f; _counterMove = null;
            _invulnUntil = Time.time + 0.4f;
            Vector2 away = (Vector2)transform.position - from;
            Vector2 toward = away.sqrMagnitude > 0.001f ? -away.normalized : Aim;
            _curType = m.hitType; _curPips = 2; _curMove = m;
            Color col = MoveColour(m);
            DungeonFx.Ghost(_anim.Renderer, A(Accent, 0.8f), 0.3f, true, false);
            DungeonFx.Flash(Body, col, 5f, 2.2f, 0.4f); DungeonFx.RingAt(Body, col, 1.6f, 0.35f); DungeonFx.Sparks(Body, toward, col, 14);
            GameFeel.SlowMo(0.3f, 0.35f); GameLook.Punch(0.5f);
            DungeonHud.Number(Body + Vector2.up * 0.9f, "Counter!", "dnum--crit");
            HitArea(Body + toward * 0.6f, m.radius * _dc.Settings.playerReach, _counterMult, toward, 10f, 0.5f);
        }

        // ───────────────────────── getting hit ─────────────────────────
        public bool CanBeHit { get { return !Dashing && Time.time >= _invulnUntil; } }

        public void TakeHit(float raw, Vector2 from)
        {
            if (!CanBeHit || _dc == null || !_dc.GameplayActive) return;
            if (_counterUntil > Time.time && _counterMove != null) { Counter(from); return; }   // Cinder Step
            _invulnUntil = Time.time + T.hurtInvulnSeconds;
            Vector2 away = ((Vector2)transform.position - from);
            _knock = (away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.down) * 7f;
            _knockT = 0.12f;
            _attackT = -1f;
            _dc.Shake(0.14f, 0.12f);
            GameFeel.HitStop(0.07f);
            GameLook.Hurt();
            DungeonFx.Sparks(Body, away, new Color(1f, 0.35f, 0.3f), 7);
            DungeonFx.Ghost(_anim.Renderer, new Color(1f, 0.25f, 0.2f, 0.9f), 0.14f, true, true);
            if (DungeonHud.PlayerHurt != null) DungeonHud.PlayerHurt();
            Vector3 numAt = Body + Vector2.up * 0.6f;
            int dmg = S.DungeonDamagePlayer(raw);   // may end the run (the floor is torn down)
            DungeonHud.Number(numAt, "-" + dmg, "dnum--hurt");
        }

        void OnDestroy()
        {
            UnityEngine.Cursor.visible = true;
            if (_reticle != null) Destroy(_reticle);
            if (_dc != null && _dc.Session != null) _dc.Session.DungeonGuardMult = 1f;
        }
    }
}
