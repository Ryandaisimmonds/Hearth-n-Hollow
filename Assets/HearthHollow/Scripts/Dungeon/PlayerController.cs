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
    public partial class PlayerController : MonoBehaviour   // moves: PlayerAttacks, PlayerSkills.*, PlayerDash, PlayerMoveKit
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
        Element _curType = Element.None;   // the kind of hit being dealt right now
        MoveData _curMove;   // the move being used right now (its light, homing…)
        // class rework stage 2: combo chains, Sunshield, Dawnbreaker's leap
        int _comboStep; float _comboUntil;
        MoveData _sunMove; float _sunUntil; int _sunLvl; UnityEngine.Rendering.Universal.Light2D _sunLight;
        readonly HashSet<EnemyController> _sunHit = new HashSet<EnemyController>();
        float _hopT, _hopDur;
        // Lamplighter: Sunbeam
        MoveData _beamMove; float _beamUntil, _beamTick, _beamMult, _beamK; Element _beamType;
        SpriteRenderer _beamSr; UnityEngine.Rendering.Universal.Light2D[] _beamLights;
        // Emberfist: Rising Phoenix, Cinder Step
        MoveData _phoenixMove; float _phoenixT, _phoenixMult; int _phoenixLvl; Element _phoenixType;
        readonly HashSet<EnemyController> _phoenixHit = new HashSet<EnemyController>();
        MoveData _counterMove; float _counterUntil, _counterMult;
        Vector2 _sweepFrom;   // where a fast move's body was last frame (so a low frame rate can't skip past enemies)
        bool _dashTail;       // one last sweep on the frame a dash ends (a short dash can finish between two frames)

        public Vector2 Feet { get { return transform.position; } }
        /// Ticket 55: the way the cook faces, as a unit vector.
        public Vector2 FacingDir { get { return _facing == Facing.Left ? Vector2.left : _facing == Facing.Right ? Vector2.right : _facing == Facing.Up ? Vector2.up : Vector2.down; } }
        public Vector2 Body { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }
        /// Hip height: projectiles fly on this plane so they hit short and tall monsters alike.
        Vector2 Hip { get { return (Vector2)transform.position + new Vector2(0, 0.45f); } }
        public bool Dashing { get { return _dashT > 0f; } }
        /// Walking, dashing or being shoved (Heat bites harder when this is false).
        public bool Moving { get { return _move.sqrMagnitude > 0.01f || Dashing || _knockT > 0f || _stepT > 0f; } }
        public Vector2 Aim { get { return _aim; } }
        bool _aimLocked;

        /// Effects Lab: use any move right now (any class's, any slot), aimed along <paramref name="aim"/> (zero = the
        /// way you're already aiming), without touching your loadout or its cooldowns. False if nothing happened.
        public bool LabCast(MoveData m, Vector2 aim)
        {
            if (m == null || _dc == null || !_dc.GameplayActive) return false;
            if (aim.sqrMagnitude > 0.0001f) { _aim = aim.normalized; _facing = CharacterAnimator.FacingFrom(_aim, _facing); _anim.Dir = _facing; }
            _aimLocked = true;
            try
            {
                switch (m.slot)
                {
                    case MoveSlot.Attack:
                    {
                        var keep = _atk; _atk = m;
                        StartAttack(); _attackHitDone = true; AttackHit();   // the swing's hit now, not on the next frames
                        _atk = keep;
                        return true;
                    }
                    case MoveSlot.Dash:
                    {
                        var keep = _dash; _dash = m;
                        StartDash();
                        After(_dc.Settings.dashSeconds + 0.15f, () => _dash = keep);   // the dash reads its move while it runs
                        return true;
                    }
                    default:
                        return UseSkill(m);
                }
            }
            finally { _aimLocked = false; }
        }

        /// Point the aim along the current movement input (any angle), and the sprite's facing to match.
        void TurnToInput()
        {
            if (_aimLocked) return;   // the Effects Lab aimed this one for you
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
            _ult = dc.Session.UltimateMove(cls);   // the chosen Ultimate (none until one is learned)
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
            if (S.Run != null) InfusedAura();   // ticket 34: your meal's element drifts off you
            if (StormOn) StormTick();           // Effects Lab: Storm Edge's pulse
            LabBuffsTick();                     // Effects Lab: the second batch's buffs
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
                speed *= LabSpeedMult;   // Effects Lab: Gale Rush
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
            if (_dc == null) return;
            var hud = _dc.Hud;
            hud.SkillUnlocked = _skill1 != null;
            hud.Skill01 = _skill1 != null ? Mathf.Clamp01(1f - _skill1Cd / Mathf.Max(0.1f, SkillCooldown(_skill1))) : 0f;
            hud.Skill2Move = _skill2;
            hud.Skill1Move = _skill1;
            hud.Skill2_01 = _skill2 != null ? Mathf.Clamp01(1f - _skill2Cd / Mathf.Max(0.1f, SkillCooldown(_skill2))) : 0f;
            hud.Dash01 = Mathf.Clamp01(1f - _dashCd / Mathf.Max(0.1f, DashCooldown));
            hud.DashMove = _dash;
            hud.AttackMove = _atk;
            hud.Attack01 = _atk != null ? Mathf.Clamp01(1f - _attackCd / Mathf.Max(0.1f, _atk.cooldown)) : 1f;
            hud.UltMove = _ult;
            hud.Ult01 = _ult != null ? Mathf.Clamp01(1f - _ultCd / Mathf.Max(0.1f, _ult.cooldown)) : 0f;
            hud.Guarding = _guardUntil > 0f;
            string buff = null;
            if (_guardUntil > 0f) buff = "🛡 Guard";
            if (_furyUntil > 0f) buff = (buff != null ? buff + "  " : "") + "📯 Fury";
            if (Time.time < _hasteUntil) buff = (buff != null ? buff + "  " : "") + "💨 Haste";
            if (S.RunInfusions.Count > 0) buff = (buff != null ? buff + "  " : "") + Elements.List(S.RunInfusions) + " Infused";
            foreach (var g in S.RunGuards) buff = (buff != null ? buff + "  " : "") + Elements.Emoji(g) + " Guard";
            string hazard = _dc != null ? _dc.Session.DungeonHazardText : null;   // ☠️ Poison · 50% less
            if (hazard != null) buff = (buff != null ? buff + "  " : "") + hazard;
            string muts = _dc != null ? _dc.Session.DungeonMutatorText : null;   // 💨 Swift  💥 Big Bangs
            if (muts != null) buff = (buff != null ? buff + "  " : "") + muts;
            hud.BuffText = buff;
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

        void Hit(EnemyController e, float mult, Vector2 knockDir, float knock = 6f) { Hit(e, mult, knockDir, knock, _curType); }

        void Hit(EnemyController e, float mult, Vector2 knockDir, float knock, Element type)
        {
            bool crit;
            if (StormOn && !_stormChaining) mult *= 1f + _stormBonus;   // Storm Edge: charged hits
            if (RadiantOn) mult *= 1f + _radiantBonus;                   // Radiant Surge
            mult *= LabHitMult();                                        // Crimson Hunger
            float dmg = S.DungeonRollDamage(mult, out crit);
            if (type != Element.None && S.RunInfusions.Contains(type) && e != null) InfusedFlair(e.Body);   // ticket 34/46: meals add their elements to your hits
            e.TakeHit(dmg, crit, knockDir, knock, type);
            if (StormOn && !_stormChaining) StormStrike(e, mult);   // …that crackle, and (level 2+) arc on to others
            LabAfterHit(e, dmg);                                     // Crimson Hunger: hits heal
        }

        Projectile Arrow(float mult, Vector2 dir, float speed, bool pierce, float scale = 1.4f)
        {
            var p = Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, speed, mult, 0f, DungeonFx.Arrow, Color.white, pierce, scale);
            if (p != null)
            {
                p.SetHit(_curType);
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
            HearthHollow.Runtime.GameAudio.Play("swing");   // ticket 62
            var m = _atk; Vector2 dir = Aim;
            float mult = AttackMult(m);
            int blv = 0;
            _curType = ElementOf(m); _curMove = m;
            dir = Assist(m, dir); _aim = dir;
            if (IsRanged(m)) { MoveLight(m, Body + dir * 0.5f, 0.9f, 0.25f); DungeonFx.Embers(Body + dir * 0.5f, Accent, 4, 0.25f); }   // muzzle flash (the arrows carry their own light)
            else
            {
                MoveLight(m, Body + dir * m.range * 0.8f, 1.4f + 0.25f * blv, 0.45f);
                DungeonFx.Embers(Body + dir * m.range, Accent, 6 + 2 * blv, 0.45f);   // every swing throws sparks of the class colour
                if (blv > 0) _dc.FloodLight(0.1f * blv, 0.6f);
            }
            AttackUse use;
            if (Attacks.TryGetValue(m.kind, out use)) use(this, m, dir, mult, blv);
            else AttackSwing(m, dir, mult, blv);
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
                    DungeonHud.Number(Body + Vector2.up * 0.8f, "No second skill equipped", "dnum--info");
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
            _curType = ElementOf(m); _curMove = m;
            if (IsRanged(m)) { dir = Assist(m, dir); _aim = dir; }
            MoveLight(m, Body, 1.2f, 0.6f);   // every skill lights the world…
            if (m.cooldown >= 5.5f) _dc.FloodLight(0.25f, 1.2f);   // …the big ones flood the room
            _anim.Dir = _facing;
            SkillUse use;
            return Skills.TryGetValue(m.kind, out use) && use(this, m, dir, mult);
        }

        // ───────────────────────── getting hit ─────────────────────────
        public bool CanBeHit { get { return !Dashing && Time.time >= _invulnUntil; } }

        public void TakeHit(float raw, Vector2 from, Element element = Element.None, string source = null)
        {
            if (!CanBeHit || _dc == null || !_dc.GameplayActive) return;
            bool guarded = element != Element.None && S.RunGuards.Contains(element);
            if (LabAbsorb(from)) return;   // Frost Aegis: a shard takes it
            if (RadiantOn) { raw *= 1f - _radiantGuard; DungeonFx.RingAt(Body, new Color(0.8f, 0.92f, 1f, 0.7f), 0.9f, 0.2f); }   // Radiant Surge: the light takes some of it
            if (guarded) raw *= S.Content.tuning.guardMult;   // a guard meal: this element hits for half
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
            if (_dc != null) _dc.RaisePlayerHurt();
            HearthHollow.Runtime.GameAudio.Play("hurt");
            Vector3 numAt = Body + Vector2.up * 0.6f;
            int dmg = S.DungeonDamagePlayer(raw, source);   // may end the run (the floor is torn down); ticket 65: who did it
            DungeonHud.Number(numAt, "-" + dmg, "dnum--hurt");
            if (guarded) { DungeonHud.Number(numAt + Vector3.up * 0.35f, Elements.Emoji(element) + " Guarded", "dnum--info"); S.Log(Elements.Emoji(element) + " Your meal guarded a " + Elements.Name(element) + " hit"); }
        }

        void OnDestroy()
        {
            UnityEngine.Cursor.visible = true;
            if (_reticle != null) Destroy(_reticle);
            if (_dc != null && _dc.Session != null) _dc.Session.DungeonGuardMult = 1f;
        }
    }
}
