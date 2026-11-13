// Hearth & Hollow — what the player's moves share: colours and light, reach and aim assist, arrow fans, slams, beams,
// the ongoing effects (whirlwind, sun, phoenix, beam) and the infused-meal flair.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
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
            _curType = ElementOf(_whirlMove); _curMove = _whirlMove;
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
        void ComboHit(MoveData m, bool finisher, Vector2 dir, float mult, int lvl)
        {
            _curType = finisher ? ElementOf2(m) : ElementOf(m); _curMove = m;
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
        void Slam(MoveData m, float mult, int lvl, Element type)
        {
            if (!Alive) return;
            _curType = type; _curMove = m;
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
            After(0.25f, () => { _curType = type; DungeonFx.RingAt(c, col, rad * 1.6f, 0.4f); DungeonFx.Flash(c, col, LightRadius(m) * 1.3f, 1.4f, 0.4f); HitArea(c, rad * 1.6f, mult * 0.6f, Vector2.zero, 6f); });
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
                if (e != null && !e.Dead && _sunHit.Add(e)) e.ElementContact(Element.Light);
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
                foreach (var e in z.Enemies()) { Hit(e, burnMult, Vector2.zero, 0f, Element.Fire); if (!Alive) return; }
            });
        }

        /// Flare Arrow bursts: a ball of light that hits (Light), stuns shadow creatures, shows weaknesses and secrets.
        void FlareBurst(MoveData m, Vector2 at, float mult, int lvl)
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
            _curType = Element.Light; _curMove = m;
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
                p.SetHit(_curType);
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
            _beamType = _curType;
            if (_beamSr == null)
            {
                var g = new GameObject("Sunbeam"); g.transform.SetParent(transform, false);
                _beamSr = g.AddComponent<SpriteRenderer>(); _beamSr.sprite = DungeonFx.Pixel; _beamSr.sortingOrder = 21000;
                if (FxMaterials.Glow != null) _beamSr.sharedMaterial = FxMaterials.Glow;
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
                    if (e != null && !e.Dead && hitNow.Add(e)) { Hit(e, _beamMult, Aim, 1.5f, _beamType); if (!Alive) return; }
            IgniteNear(a + Aim * len, half + 0.3f);
            DungeonFx.Embers(a + Aim * len, col, 2, 0.2f);
        }

        /// A move's element: its own, or (for moves without one) the run's infused element from meals.
        // ── ticket 34: an infused meal shows on you and on every hit ──
        float _flairAt;
        int _auraIx;
        void InfusedFlair(Vector3 at)
        {
            var els = S.RunInfusions;   // ticket 46: one burst per infused element, each in its own colour
            if (els.Count == 0) return;
            int each = Mathf.Max(3, (els.Contains(Element.Fire) ? 10 : 7) / els.Count + 1);
            for (int i = 0; i < els.Count; i++)
            {
                Color c = EnemyController.ElementColour(els[i]);
                DungeonFx.Embers(at, c, each, 0.4f);
                DungeonFx.RingAt(at, new Color(c.r, c.g, c.b, 0.7f), 0.6f + i * 0.18f, 0.25f);
            }
        }

        /// A few element motes drift off you while an infused meal is working (called from Update).
        void InfusedAura()
        {
            var els = S.RunInfusions;
            if (els.Count == 0 || Time.time < _flairAt) return;
            _auraIx = (_auraIx + 1) % els.Count;   // ticket 46: the motes take turns in each element's colour
            var el = els[_auraIx];
            _flairAt = Time.time + (el == Element.Fire ? 0.12f : 0.18f);
            DungeonFx.Embers(Hip, EnemyController.ElementColour(el), 1, 0.35f);
        }

        Element ElementOf(MoveData m) { return m != null && m.element != Element.None ? m.element : S.RunElement; }
        /// A combo finisher's element (falls back to the move's).
        Element ElementOf2(MoveData m) { return m != null && m.element2 != Element.None ? m.element2 : ElementOf(m); }
        /// Prism finds a weakness: the target's first known weak element (Light if it has none).
        static Element PrismElement(EnemyController e) { return e != null && e.WeakTo.Count > 0 ? e.WeakTo[0] : Element.Light; }

        /// Prism lands: it strikes with the target's weakness, splashing round about.
        void PrismBurst(MoveData m, Vector2 at, float mult, int lvl)
        {
            if (!Alive) return;
            EnemyController target = null; float bd = 1.3f;
            foreach (var e in Combat2D.EnemiesIn(at, 1.3f)) { if (e == null || e.Dead) continue; float d = Vector2.Distance(e.Body, at); if (d < bd) { bd = d; target = e; } }
            Color[] rainbow = { new Color(1f, 0.4f, 0.4f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 1f, 0.5f), new Color(0.45f, 0.7f, 1f), new Color(0.8f, 0.5f, 1f) };
            for (int i = 0; i < rainbow.Length; i++) DungeonFx.Throw(at, Quaternion.Euler(0, 0, i * 72f) * Vector2.up, rainbow[i], 3, 3f, 6f);
            DungeonFx.Flash(at, MoveColour(m), LightRadius(m), 2f, 0.5f);
            DungeonFx.RingAt(at, MoveColour(m), 1.2f, 0.35f);
            Element prismEl = target != null ? PrismElement(target) : Element.Light;   // Prism strikes with the target's weakness
            _curType = prismEl; _curMove = m;
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
            _curType = _phoenixType; _curMove = m;
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
            DungeonZone.Spawn(_dc, Feet, 0.9f, 2f, 0.5f, A(col, 0.25f), z => { foreach (var e in z.Enemies()) { Hit(e, burn, Vector2.zero, 0f, Element.Fire); if (!Alive) return; } });
        }

        /// Cinder Step: an attack lands during the stance. No damage; you strike back in a burst of fire (+20 Heat).
        void Counter(Vector2 from)
        {
            var m = _counterMove; _counterUntil = 0f; _counterMove = null;
            _invulnUntil = Time.time + 0.4f;
            Vector2 away = (Vector2)transform.position - from;
            Vector2 toward = away.sqrMagnitude > 0.001f ? -away.normalized : Aim;
            _curType = ElementOf(m); _curMove = m;
            Color col = MoveColour(m);
            DungeonFx.Ghost(_anim.Renderer, A(Accent, 0.8f), 0.3f, true, false);
            DungeonFx.Flash(Body, col, 5f, 2.2f, 0.4f); DungeonFx.RingAt(Body, col, 1.6f, 0.35f); DungeonFx.Sparks(Body, toward, col, 14);
            GameFeel.SlowMo(0.3f, 0.35f); GameLook.Punch(0.5f);
            DungeonHud.Number(Body + Vector2.up * 0.9f, "Counter!", "dnum--crit");
            HitArea(Body + toward * 0.6f, m.radius * _dc.Settings.playerReach, _counterMult, toward, 10f, 0.5f);
        }
    }
}
