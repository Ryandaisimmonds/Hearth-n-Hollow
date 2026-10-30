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
        MoveData _atk, _skill1, _skill2, _dash;
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
        float _skill1Cd, _skill2Cd;
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

        public Vector2 Feet { get { return transform.position; } }
        public Vector2 Body { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }
        /// Hip height: projectiles fly on this plane so they hit short and tall monsters alike.
        Vector2 Hip { get { return (Vector2)transform.position + new Vector2(0, 0.45f); } }
        public bool Dashing { get { return _dashT > 0f; } }
        /// Walking, dashing or being shoved (Heat bites harder when this is false).
        public bool Moving { get { return _move.sqrMagnitude > 0.01f || Dashing || _knockT > 0f || _stepT > 0f; } }
        Vector2 Aim { get { return _aim; } }

        /// Point the aim along the current movement input (any angle), and the sprite's facing to match.
        void TurnToInput()
        {
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
        public float[] Cooldowns { get { return new[] { Mathf.Max(0f, _skill1Cd), Mathf.Max(0f, _skill2Cd), Mathf.Max(0f, _dashCd) }; } }
        public void SetCooldowns(float[] cd)
        {
            if (cd == null || cd.Length < 3) return;
            _skill1Cd = cd[0]; _skill2Cd = cd[1]; _dashCd = cd[2];
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
            if (_dc == null || !_dc.GameplayActive) { _move = Vector2.zero; return; }
            float dt = Time.deltaTime;
            _attackCd -= dt; _dashCd -= dt; _skill1Cd -= dt; _skill2Cd -= dt;
            if (_dashT > 0f) _dashT -= dt;
            if (_knockT > 0f) _knockT -= dt;
            if (_stepT > 0f) _stepT -= dt;

            _move = DungeonInput.Move;
            bool attacking = _attackT >= 0f;
            if (!attacking) TurnToInput();

            if (DungeonInput.Dash && _dashCd <= 0f && _knockT <= 0f) StartDash();
            if ((DungeonInput.Attack || DungeonInput.AttackHeld) && _attackCd <= 0f && !Dashing) StartAttack();
            if (DungeonInput.Special) TrySkill(_skill1, ref _skill1Cd, "K");
            if (DungeonInput.Special2) TrySkill(_skill2, ref _skill2Cd, "U");
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
                float speed = _dc.Settings.playerSpeed * _mods.moveSpeed;
                if (Time.time < _hasteUntil) speed *= 1.3f;
                speed *= 1f - _dc.Session.DungeonHazardSlow;   // Frostfang cold
                speed *= _dc.Session.MutatorSpeed;              // Swift ×2 / Frantic ×3
                if (_attackT >= 0f) speed *= 0.3f;
                else if (_whirlUntil > Time.time) speed *= 0.7f;
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

        void Hit(EnemyController e, float mult, Vector2 knockDir, float knock = 6f)
        {
            bool crit;
            float dmg = S.DungeonRollDamage(mult, out crit);
            e.TakeHit(dmg, crit, knockDir, knock);
        }

        Projectile Arrow(float mult, Vector2 dir, float speed, bool pierce, float scale = 1.4f)
        {
            return Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, speed, mult, 0f, DungeonFx.Arrow, Color.white, pierce, scale);
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
            switch (m.kind)
            {
                case MoveKind.Arrow:
                    Arrow(mult, dir, m.range, false);
                    break;
                case MoveKind.SpreadArrow:
                    for (int i = -1; i <= 1; i++) Arrow(mult, Quaternion.Euler(0, 0, i * m.value) * dir, m.range, false, 1.2f);
                    break;
                case MoveKind.PierceArrow:
                    Arrow(mult, dir, m.range, true, 1.9f);
                    DungeonFx.Puff(Hip + dir * 0.4f, new Color(1f, 1f, 1f, 0.5f), 3, 0.3f);
                    break;
                case MoveKind.Lunge:
                    _stepVel = dir * (m.range / 0.12f) * 0.5f; _stepT = 0.12f;   // a short step forward
                    DungeonFx.SlashAt(Body + dir * 0.4f, dir, new Color(0.95f, 0.95f, 1f, 0.9f), 1.4f);
                    // a thrust: three small circles along the line
                    var done = new HashSet<EnemyController>();
                    for (int i = 1; i <= 3; i++)
                        foreach (var e in Combat2D.EnemiesIn(Body + dir * (m.range * i / 3f), m.radius))
                            if (done.Add(e)) { Hit(e, mult, dir); if (!Alive) return; }
                    break;
                case MoveKind.Heavy:
                    DungeonFx.SlashAt(Body + dir * 0.3f, dir, new Color(1f, 0.85f, 0.6f, 1f), 1.7f);
                    DungeonFx.RingAt(Body + dir * m.range, new Color(1f, 0.9f, 0.7f, 0.8f), m.radius, 0.3f);
                    _dc.Shake(0.12f, 0.12f);
                    HitArea(Body + dir * m.range, m.radius, mult, dir, 10f, m.duration);
                    break;
                default:   // Swing
                    DungeonFx.SlashAt(Body + dir * 0.25f, dir, _class == ClassId.Cook ? new Color(1f, 0.85f, 0.6f, 0.9f) : new Color(0.9f, 0.95f, 1f, 0.9f), m.range < 0.8f ? 1.0f : 1.2f);
                    HitArea(Body + dir * m.range, m.radius, mult, dir);
                    break;
            }
        }

        // ───────────────────────── dash (Space) ─────────────────────────
        void StartDash()
        {
            var m = _dash;
            _dashDir = _move.sqrMagnitude > 0.01f ? _move.normalized : Aim;
            _dashT = _dc.Settings.dashSeconds;
            _dashSpeedMult = 1f;
            _dashCd = DashCooldown;
            _attackT = -1f;   // dash cancels a swing
            _dashHit.Clear();
            _trailT = 0f;
            DungeonFx.Puff(transform.position + new Vector3(0, 0.1f), new Color(1, 1, 1, 0.5f), 4, 0.4f);
            if (m == null) return;
            switch (m.kind)
            {
                case MoveKind.ChargeDash:
                    _dashT *= 1.25f; _dashSpeedMult = 0.95f;
                    break;
                case MoveKind.Vault:
                    _dashDir = -Aim;   // flip backwards, keep facing forward
                    Arrow(m.power * _mods.attackPower * DamageBuff, Aim, 14f, false);
                    break;
                case MoveKind.SlickDash:
                    _dashT *= 1.3f; _dashSpeedMult = 0.85f;
                    break;
            }
        }

        void DashUpdate(float dt)
        {
            if (!Dashing || _dash == null) return;
            switch (_dash.kind)
            {
                case MoveKind.ChargeDash:
                    foreach (var e in Combat2D.EnemiesIn(Body, 0.75f))
                    {
                        if (!_dashHit.Add(e)) continue;
                        Hit(e, _dash.power * _mods.attackPower * DamageBuff, _dashDir, 11f);
                        if (!Alive) return;
                        _dc.Shake(0.06f, 0.06f);
                    }
                    break;
                case MoveKind.SlickDash:
                    _trailT -= dt;
                    if (_trailT <= 0f)
                    {
                        _trailT = 0.05f;
                        var slick = _dash;
                        DungeonZone.Spawn(_dc, Feet, 0.6f, slick.duration, 0.1f, new Color(1f, 0.92f, 0.45f, 0.35f), z =>
                        {
                            foreach (var e in z.Enemies()) e.Slow(1f - slick.value, slick.duration);
                        });
                    }
                    break;
            }
        }

        // ───────────────────────── skills (K / U) ─────────────────────────
        void TrySkill(MoveData m, ref float cd, string key)
        {
            if (m == null)
            {
                if (key == "U")
                    DungeonHud.Number(Body + Vector2.up * 0.8f, S.Profile.Progress(_class).level < T.secondSkillLevel
                        ? "U slot opens at Lv " + T.secondSkillLevel : "Nothing in the U slot", "dnum--info");
                return;
            }
            if (cd > 0f) return;
            if (!UseSkill(m)) return;     // e.g. Hunter's Mark with nobody to mark
            cd = SkillCooldown(m);
        }

        bool UseSkill(MoveData m)
        {
            TurnToInput();
            Vector2 dir = Aim;
            float mult = SkillMult(m);
            _anim.Dir = _facing;
            switch (m.kind)
            {
                // ── Warden ──
                case MoveKind.Cleave:
                    _anim.PlayAttack(0.35f);
                    DungeonFx.RingAt(Body, new Color(1f, 0.95f, 0.8f, 0.9f), m.radius);
                    _dc.Shake(0.15f, 0.12f);
                    HitArea(Body, m.radius, mult, Vector2.zero);
                    return true;
                case MoveKind.Shockwave:
                {
                    _anim.PlayAttack(0.4f);
                    _dc.Shake(0.2f, 0.15f);
                    var origin = Body; var hitOnce = new HashSet<EnemyController>();
                    int steps = Mathf.Max(1, Mathf.RoundToInt(m.range));
                    for (int i = 1; i <= steps; i++)
                    {
                        Vector2 at = origin + dir * i;
                        After(0.06f * i, () =>
                        {
                            DungeonFx.RingAt(at, new Color(0.95f, 0.8f, 0.55f, 0.85f), m.radius, 0.3f);
                            DungeonFx.Puff(at, new Color(0.7f, 0.6f, 0.45f, 0.6f), 3, 0.4f);
                            foreach (var e in Combat2D.EnemiesIn(at, m.radius * S.MutatorAreaRadius))
                            {
                                if (!hitOnce.Add(e)) continue;
                                Hit(e, mult * S.MutatorAreaDamage, dir, 7f);
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
                    DungeonFx.RingAt(Body, new Color(1f, 0.5f, 0.35f, 0.9f), 2.4f, 0.5f);
                    DungeonHud.Number(Body + Vector2.up * 0.8f, "+" + Mathf.RoundToInt(m.value * 100f) + "% damage", "dnum--info");
                    return true;
                case MoveKind.ShieldWall:
                    _anim.PlayAttack(0.25f);
                    Guard(m.value, m.duration);
                    DungeonFx.RingAt(Body, new Color(0.7f, 0.85f, 1f, 0.9f), 1.3f, 0.5f);
                    return true;
                case MoveKind.Whirlwind:
                    _whirlMove = m; _whirlUntil = Time.time + m.duration; _whirlTick = 0f;
                    return true;

                // ── Ranger ──
                case MoveKind.Volley:
                    _anim.PlayAttack(0.3f);
                    Arrow(mult, Quaternion.Euler(0, 0, -7f) * dir, 13f, true);
                    After(0.09f, () => Arrow(mult, Quaternion.Euler(0, 0, 7f) * Aim, 13f, true));
                    return true;
                case MoveKind.Trap:
                {
                    _anim.PlayAttack(0.25f);
                    var trapMove = m; float trapMult = mult;
                    DungeonZone.Spawn(_dc, Feet, m.radius, m.value, 0f, new Color(0.75f, 0.6f, 0.4f, 0.55f), z =>
                    {
                        var hit = z.Enemies();
                        if (hit.Count == 0) return;
                        var e = hit[0];
                        z.End();
                        DungeonFx.RingAt(z.Centre, new Color(0.9f, 0.75f, 0.5f, 0.9f), trapMove.radius * 1.3f, 0.35f);
                        Hit(e, trapMult, Vector2.zero, 0f);
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
                    Vector2 at = Feet + dir * m.range;
                    float tickMult = mult;
                    DungeonZone.Spawn(_dc, at, m.radius, m.duration, 0.25f, new Color(0.85f, 0.9f, 1f, 0.22f), z =>
                    {
                        for (int i = 0; i < 3; i++)
                            DungeonFx.Spawn(DungeonFx.Arrow, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius + Vector2.up * 0.6f, Color.white, 0.18f, 1.2f, 1.0f, -90f);
                        foreach (var e in z.Enemies()) { Hit(e, tickMult, Vector2.zero, 0f); if (!Alive) return; }
                    });
                    return true;
                }
                case MoveKind.Smoke:
                    _anim.PlayAttack(0.2f);
                    DungeonFx.Puff(Body, new Color(0.7f, 0.7f, 0.72f, 0.7f), 14, 1.6f);
                    foreach (var e in Combat2D.EnemiesIn(Feet, m.radius)) e.Slow(1f - m.value, m.duration);
                    _hasteUntil = Time.time + m.duration;
                    return true;

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
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 d = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-m.radius, m.radius)) * dir;
                        var fx = DungeonFx.Spawn(DungeonFx.Circle, Body + d * 0.4f, new Color(0.95f, 0.3f, 0.15f, 0.8f), 0.4f, 0.35f, 0.15f);
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
                    Vector2 at = Feet + dir * m.range;
                    var pot = DungeonFx.Spawn(DungeonFx.Circle, Body, new Color(0.55f, 0.4f, 0.3f, 1f), 0.35f, 0.7f, 0.7f);
                    pot.velocity = (at - Body) * 6f;   // FxSprite damps velocity by ~6/s, so this lands near the target
                    var potMove = m; float potMult = mult;
                    After(0.35f, () =>
                    {
                        DungeonFx.Puff(at, new Color(1f, 0.6f, 0.25f, 0.8f), 8, 1f);
                        DungeonZone.Spawn(_dc, at, potMove.radius, potMove.duration, potMove.value, new Color(1f, 0.45f, 0.15f, 0.35f), z =>
                        {
                            DungeonFx.Spawn(DungeonFx.Circle, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius, new Color(1f, 0.7f, 0.3f, 0.7f), 0.3f, 0.3f, 0.05f);
                            foreach (var e in z.Enemies()) { Hit(e, potMult, Vector2.zero, 0f); if (!Alive) return; }
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
                case MoveKind.RollingPin:
                {
                    _anim.PlayAttack(0.3f);
                    var p = Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, m.value, mult, 0f, DungeonFx.Arrow, new Color(0.85f, 0.65f, 0.45f), true, 2.2f);
                    if (p != null) p.Boomerang(m.range, 900f);
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
            DungeonFx.RingAt(Body, new Color(0.9f, 0.95f, 1f, 0.7f), _whirlMove.radius, 0.2f);
            HitArea(Body, _whirlMove.radius, SkillMult(_whirlMove), Vector2.zero, 4f);
        }

        // ───────────────────────── getting hit ─────────────────────────
        public bool CanBeHit { get { return !Dashing && Time.time >= _invulnUntil; } }

        public void TakeHit(float raw, Vector2 from)
        {
            if (!CanBeHit || _dc == null || !_dc.GameplayActive) return;
            _invulnUntil = Time.time + T.hurtInvulnSeconds;
            Vector2 away = ((Vector2)transform.position - from);
            _knock = (away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.down) * 7f;
            _knockT = 0.12f;
            _attackT = -1f;
            _dc.Shake(0.12f, 0.1f);
            if (DungeonHud.PlayerHurt != null) DungeonHud.PlayerHurt();
            Vector3 numAt = Body + Vector2.up * 0.6f;
            int dmg = S.DungeonDamagePlayer(raw);   // may end the run (the floor is torn down)
            DungeonHud.Number(numAt, "-" + dmg, "dnum--hurt");
        }

        void OnDestroy()
        {
            if (_dc != null && _dc.Session != null) _dc.Session.DungeonGuardMult = 1f;
        }
    }
}
