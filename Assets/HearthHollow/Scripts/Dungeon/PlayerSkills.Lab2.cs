// Hearth & Hollow — the Effects Lab's second batch: five attacks and five buffs built on the first lab moves
// (SlashRing trails, GlowAura, LightRings, FxMover), each a different shape, weapon and colour, each with three levels.
//   Attacks  Tidal Cleave (a wave), Frost Lance (a thrust), Void Rift (a burst at range), Sunbreaker (a ground slam),
//            Petal Flurry (a flurry of blades).
//   Buffs    Ember Mantle (burns what's near), Frost Aegis (shards that block hits), Gale Rush (speed, wind that cuts),
//            Crimson Hunger (hits heal you), Astral Crown (stars that shoot for you).
// Their MoveData is added by the lab (EffectsLab.LabMoves). The buffs hook into PlayerController through
// LabBuffsTick (Update), LabHitMult / LabAfterHit (Hit), LabAbsorb (TakeHit) and LabSpeedMult (FixedUpdate).
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        int LabLevel(MoveData m) { return Mathf.Clamp(S.MoveLevel(m.id), 1, GameSession.MaxMoveLevel); }

        static void Paint(SlashRing r, Color hot, Color pale, Color light, Color deep, float heat = 1f, float headHeat = 3f)
        {
            r.HotColour = hot; r.PaleColour = pale; r.LightColour = light; r.DeepColour = deep;
            r.HeatScale = heat; r.HeadHeat = headHeat;
        }

        /// A closed ring of trail that bursts outwards from <paramref name="follow"/> (the ends overlap and cross-fade: no seam).
        SlashRing BurstRing(Transform follow, Vector2 offset, float radius, float width, float drift, float life)
        {
            var w = SlashRing.Play(follow, offset, Random.Range(0f, Mathf.PI * 2f), 1);
            w.ShowBlade = false; w.Radius = radius; w.Width = width;
            w.SweepTime = 0.03f; w.Turns = 1.1f; w.StartFadeDeg = 36f; w.EndFadeDeg = 36f;
            w.TrailLife = life; w.Drift = drift; w.Grow = 0.9f; w.Sparks = 0; w.LeafChance = 0f;
            return w;
        }

        /// Something to hang an effect on at a spot on the floor (it cleans itself up after <paramref name="life"/>).
        static Transform Spot(Vector2 at, float life)
        {
            var m = FxMover.Throw(at, Vector2.zero, 0f);
            m.Linger = life;
            return m.transform;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return (p - (a + ab * t)).magnitude;
        }

        /// Everyone within <paramref name="half"/> of the line from a to b.
        static List<EnemyController> OnLine(Vector2 a, Vector2 b, float half)
        {
            var list = new List<EnemyController>();
            foreach (var e in Combat2D.EnemiesIn((a + b) * 0.5f, (b - a).magnitude * 0.5f + half + 0.5f))
                if (e != null && !e.Dead && SegDist(e.Body, a, b) <= half) list.Add(e);
            return list;
        }

        /// Hit everyone in an ellipse (the ground seen at an angle). Returns how many.
        int HitRound(Vector2 c, float radius, float mult, float knock, Element type, HashSet<EnemyController> skip = null, bool pull = false)
        {
            int n = 0;
            foreach (var e in Combat2D.EnemiesIn(c, radius + 0.5f))
            {
                if (e == null || e.Dead || (skip != null && skip.Contains(e))) continue;
                Vector2 off = e.Body - c;
                if (new Vector2(off.x, off.y / 0.72f).magnitude > radius + 0.3f) continue;
                if (skip != null) skip.Add(e);
                Vector2 away = off.sqrMagnitude > 0.001f ? off.normalized : Random.insideUnitCircle.normalized;
                _curType = type;
                Hit(e, mult * S.MutatorAreaDamage, pull ? -away : away, knock, type);
                n++;
                if (!Alive) return n;
            }
            return n;
        }

        /// The monster nearest to the line along <paramref name="dir"/> (within reach), or null.
        EnemyController AimedAt(Vector2 dir, float reach)
        {
            EnemyController best = null; float bestScore = float.MaxValue;
            foreach (var e in Combat2D.EnemiesIn(Body, reach + 0.5f))
            {
                if (e == null || e.Dead) continue;
                Vector2 to = e.Body - Body;
                float ang = Vector2.Angle(dir, to);
                if (ang > 30f) continue;
                float score = ang * 0.05f + to.magnitude;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }

        // ═════════════════════════ ATTACKS ═════════════════════════

        // ───────────── Tidal Cleave ─────────────
        static readonly Color TideHot = new Color(0.92f, 1f, 1f), TidePale = new Color(0.55f, 0.97f, 1f),
                              TideLight = new Color(0.12f, 0.78f, 0.95f), TideDeep = new Color(0.04f, 0.3f, 0.85f);

        /// Tidal Cleave: a great overhead cleave across the front that throws a wave forward. White foam at the blade,
        /// cooling through aqua into deep blue.
        ///   Level 1  the cleave and one wave.
        ///   Level 2  wider, harder, and a second wave on the heels of the first.
        ///   Level 3  where the waves stop, a whirlpool spins up and drags monsters into it, hitting three times.
        bool UseTidalCleave(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float radius = m.radius * Reach(m, 0) * (1f + 0.2f * up);
            mult *= 1f + 0.3f * up;
            int spin = dir.x >= 0f ? -1 : 1;
            float aim = Mathf.Atan2(dir.y, dir.x);
            float sweep = 0.24f;
            var type = Element.Water;
            _anim.PlayAttack(sweep + 0.05f);

            // the cleave: half a circle across the front, over the top and down
            var ring = SlashRing.Play(transform, Body - Feet, aim - spin * 1.4f, spin);
            ring.Radius = radius; ring.Width = radius * 0.7f; ring.Turns = 0.45f; ring.SweepTime = sweep;
            ring.TrailLife = 0.45f; ring.Grow = 0.6f; ring.Drift = 0.2f; ring.BladeThickness = 0.42f + 0.06f * up; ring.BladeReach = 0.25f;
            ring.Sparks = 0; ring.LeafChance = 0f; ring.LightIntensity = 0.6f;
            Paint(ring, TideHot, TidePale, TideLight, TideDeep, 1f + 0.15f * up, 3.2f + 0.4f * up);
            var hit = new HashSet<EnemyController>();
            After(sweep * 0.5f, () =>
            {
                if (!Alive) return;
                foreach (var e in Combat2D.EnemiesInCone(Body, dir, radius + 0.6f, 100f))
                {
                    if (e == null || e.Dead || hit.Contains(e)) continue;
                    hit.Add(e); _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, (e.Body - Body).normalized, 8f, type);
                    if (!Alive) return;
                    DungeonFx.Sparks(e.Body, e.Body - Body, TidePale, 10);
                }
                _dc.Shake(0.12f + 0.04f * up, 0.08f);
                for (int i = 0; i < 10 + 6 * up; i++)
                {
                    Vector2 o = Quaternion.Euler(0, 0, Random.Range(-70f, 70f)) * dir;
                    var drop = DungeonFx.Spawn(DungeonFx.Drop, (Vector3)(Body + o * radius * 0.8f), Color.Lerp(TidePale, TideLight, Random.value), Random.Range(0.4f, 0.7f), 1.4f, 0.6f, 0f, 20460, true);
                    drop.velocity = o * Random.Range(3f, 6f) + Vector2.up * 2f; drop.accel = Vector2.down * 12f; drop.drag = 1f;
                }
            });

            // the wave(s)
            int waves = lvl >= 2 ? 2 : 1;
            for (int w = 0; w < waves; w++)
            {
                float delay = sweep * 0.6f + w * 0.13f;
                bool last = w == waves - 1, second = w > 0;
                After(delay, () => { if (Alive) ThrowWave(dir, radius * (second ? 0.85f : 1f), m.range * (1f + 0.25f * up), mult * 0.7f, lvl, last && lvl >= 3, type); });
            }
            return true;
        }

        void ThrowWave(Vector2 d, float size, float reach, float mult, int lvl, bool whirl, Element type)
        {
            const float Fly = 0.5f, Drag = 2f;
            float v0 = reach * Drag / (1f - Mathf.Exp(-Drag * Fly));
            var mover = FxMover.Throw(Body + d * 0.4f, d * v0, Fly);
            mover.Drag = Drag; mover.Linger = whirl ? 2.2f : 0.9f;
            var ring = SlashRing.Play(mover.transform, Vector2.zero, 0f, 1);
            float squash = ring.Squash, a0 = Mathf.Atan2(d.y / squash, d.x), arc = 120f * Mathf.Deg2Rad;
            ring.SetStart(a0 - arc * 0.5f);
            ring.ShowBlade = false; ring.Radius = size * 0.75f; ring.Width = size * 0.55f; ring.Turns = arc / (Mathf.PI * 2f); ring.SweepTime = 0.06f;
            ring.TrailLife = Fly + 0.35f; ring.Drift = 0.3f; ring.Grow = 0.7f; ring.StartFadeDeg = 35f; ring.EndFadeDeg = 35f;
            ring.Sparks = 0; ring.LeafChance = 0f;
            Paint(ring, TideHot, TidePale, TideLight, TideDeep, 0.95f + 0.1f * lvl, 2.6f);
            DungeonFx.Flash(Body, TideLight, size * 2.4f, 0.6f + 0.15f * lvl, Fly + 0.3f, mover.transform);
            var hit = new HashSet<EnemyController>(); float foamT = 0f;
            mover.Tick = pos =>
            {
                if (!Alive) return;
                foamT -= Time.deltaTime;
                if (foamT <= 0f)
                {
                    foamT = 0.05f;
                    Vector2 front = pos + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0) * squash) * size * 0.75f;
                    var f = DungeonFx.Spawn(DungeonFx.Circle, (Vector3)(front + Random.insideUnitCircle * 0.3f), TidePale, 0.35f, 0.35f, 0.05f, 0f, 20460, true);
                    f.velocity = d * 2f + Random.insideUnitCircle;
                }
                foreach (var e in Combat2D.EnemiesIn(pos, size + 0.4f))
                {
                    if (e == null || e.Dead || hit.Contains(e)) continue;
                    hit.Add(e); _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, d, 11f, type);   // the wave carries them
                    if (!Alive) return;
                }
            };
            if (!whirl) return;
            mover.Landed = at =>
            {
                if (!Alive) return;
                // a whirlpool: rings spinning in, dragging monsters to the middle
                for (int i = 0; i < 3; i++)
                {
                    var wr = SlashRing.Play(mover.transform, Vector2.zero, i * 2.1f, 1);
                    wr.ShowBlade = false; wr.Radius = size * (1f - 0.25f * i); wr.Width = size * 0.35f;
                    wr.Turns = 2.2f; wr.SweepTime = 1.5f; wr.TrailLife = 0.4f; wr.Drift = -0.15f; wr.Grow = 0.3f;
                    wr.StartFadeDeg = 60f; wr.EndFadeDeg = 60f; wr.Sparks = 0; wr.LeafChance = 0f; wr.LightIntensity = 0.3f;
                    Paint(wr, TideHot, TidePale, TideLight, TideDeep, 0.9f, 2.2f);
                }
                DungeonFx.Flash(at, TideLight, size * 3f, 1f, 1.6f, mover.transform);
                for (int k = 0; k < 3; k++)
                    After(0.3f + 0.45f * k, () =>
                    {
                        if (!Alive) return;
                        DungeonFx.RingAt(at, new Color(0.5f, 0.95f, 1f, 0.6f), size * 0.8f, 0.3f);
                        HitRound(at, size * 1.3f, mult * 0.45f, 5f, type, null, true);
                    });
            };
        }

        // ───────────── Frost Lance ─────────────
        static readonly Color FrostCore = new Color(0.85f, 0.97f, 1f), FrostGlow = new Color(0.45f, 0.85f, 1f), FrostDeep = new Color(0.25f, 0.5f, 1f);

        /// Frost Lance: a lunge and a straight thrust — a long, narrow lance of ice-light that pierces everything in line.
        ///   Level 1  one lance.
        ///   Level 2  longer, harder; two thinner lances fan out beside it.
        ///   Level 3  the main lance leaves a line of ice crystals that grow, then shatter, hitting the line again.
        bool UseFrostLance(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float reach = m.range * (1f + 0.25f * up);
            mult *= 1f + 0.3f * up;
            var type = Element.Frost;
            _anim.PlayAttack(0.2f);
            _stepVel = dir * 7f; _stepT = 0.08f;   // a short lunge into it
            _dc.Shake(0.08f + 0.03f * up, 0.06f);

            Vector2 from = Body + dir * 0.3f;
            var hit = new HashSet<EnemyController>();
            Lance(from, dir, reach, 0.3f + 0.05f * up, mult, type, hit);
            if (lvl >= 2)
                foreach (float a in new[] { -14f, 14f })
                    Lance(from, Quaternion.Euler(0, 0, a) * dir, reach * 0.75f, 0.18f, mult * 0.55f, type, hit);
            DungeonFx.Flash(from + dir * reach * 0.4f, FrostGlow, reach * 1.2f, 1f + 0.2f * up, 0.3f);

            if (lvl >= 3)
            {
                Vector2 end = from + dir * reach;
                int n = 9;
                for (int i = 0; i < n; i++)
                {
                    Vector2 p = Vector2.Lerp(from, end, (i + 0.5f) / n) + Random.insideUnitCircle * 0.12f;
                    var c = DungeonFx.Spawn(DungeonFx.Shard, (Vector3)p, Color.Lerp(FrostCore, FrostGlow, Random.value * 0.6f), 0.55f, 0.4f, 1.6f, Random.Range(-25f, 25f), 20460, true);
                    c.drag = 0f;
                }
                After(0.5f, () =>
                {
                    if (!Alive) return;
                    _dc.Shake(0.15f, 0.12f); GameFeel.HitStop(0.04f);
                    LabBeam.Line(from, end, FrostGlow, 0.5f, 0.3f);
                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 p = Vector2.Lerp(from, end, Random.value);
                        var s = DungeonFx.Spawn(DungeonFx.Shard, (Vector3)p, Color.Lerp(FrostCore, FrostDeep, Random.value * 0.5f), Random.Range(0.4f, 0.7f), 1f, 0.3f, Random.Range(0f, 360f), 20460, true);
                        s.velocity = Random.insideUnitCircle.normalized * Random.Range(3f, 6f); s.drag = 4f; s.spin = Random.Range(-600f, 600f);
                    }
                    foreach (var e in OnLine(from, end, 0.75f))
                    {
                        _curType = type;
                        Hit(e, mult * 0.7f * S.MutatorAreaDamage, Random.insideUnitCircle.normalized, 4f, type);
                        if (!Alive) return;
                    }
                });
            }
            return true;
        }

        void Lance(Vector2 from, Vector2 d, float reach, float width, float mult, Element type, HashSet<EnemyController> hit)
        {
            Vector2 to = from + d * reach;
            LabBeam.Line(from, to, FrostGlow, width, 0.32f, 0.07f);
            LabBeam.Line(from, from + d * reach * 1.08f, FrostDeep, width * 0.45f, 0.45f, 0.1f, 20478);   // a thin afterglow past the tip
            for (int i = 0; i < 8; i++)
            {
                var s = DungeonFx.Spawn(DungeonFx.Twinkle, (Vector3)Vector2.Lerp(from, to, Random.value), FrostCore, Random.Range(0.3f, 0.5f), 0.8f, 0.2f, 0f, 20482, true);
                s.velocity = d * Random.Range(2f, 5f);
            }
            After(0.04f, () =>
            {
                if (!Alive) return;
                foreach (var e in OnLine(from, to, 0.55f + width))
                {
                    if (hit.Contains(e)) continue;
                    hit.Add(e); _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, d, 9f, type);
                    if (!Alive) return;
                    DungeonFx.Sparks(e.Body, d, FrostCore, 10);
                }
            });
        }

        // ───────────── Void Rift ─────────────
        static readonly Color VoidHot = new Color(1f, 0.85f, 1f), VoidPale = new Color(0.95f, 0.5f, 1f),
                              VoidLight = new Color(0.62f, 0.2f, 1f), VoidDeep = new Color(0.25f, 0.05f, 0.6f);

        /// Void Rift: tear a rift open at range (on the monster you're aiming at). Light and motes are sucked into it,
        /// monsters are dragged in, then it bursts in a ring of violet.
        ///   Level 1  one rift.
        ///   Level 2  bigger, a harder pull and burst.
        ///   Level 3  as it bursts, two more rifts open on monsters nearby and burst a beat later.
        bool UseVoidRift(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float radius = m.radius * (1f + 0.25f * up);
            mult *= 1f + 0.3f * up;
            var target = AimedAt(dir, m.range);
            Vector2 at = target != null ? target.Body : Body + dir * m.range * 0.8f;
            _anim.PlayAttack(0.25f);
            LabBeam.Line(Body, at, VoidLight, 0.12f, 0.25f, 0.08f);   // a thread of void to it
            Rift(at, radius, mult, lvl, lvl >= 3);
            return true;
        }

        void Rift(Vector2 at, float radius, float mult, int lvl, bool spawnMore)
        {
            var type = Element.None;
            const float Gather = 0.5f;
            var spot = Spot(at, Gather + 1.2f);
            // gathering: a ring closing in, motes drawn to the middle, a dark-violet light
            var g1 = BurstRing(spot, Vector2.zero, radius * 1.5f, radius * 0.22f, -0.85f, Gather);   // rings closing in on the middle
            Paint(g1, VoidHot, VoidPale, VoidLight, VoidDeep, 0.8f, 2.4f); g1.Grow = -0.6f;
            var g2 = BurstRing(spot, Vector2.zero, radius * 1.1f, radius * 0.16f, -0.8f, Gather * 0.85f);
            Paint(g2, VoidHot, VoidPale, VoidLight, VoidDeep, 0.7f, 2.4f); g2.Grow = -0.6f;
            DungeonFx.Flash(at, VoidLight, radius * 2.5f, 0.9f, Gather + 0.2f);
            for (int i = 0; i < 24; i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized * Random.Range(radius * 0.9f, radius * 1.6f);
                var s = DungeonFx.Spawn(Random.value < 0.5f ? DungeonFx.Twinkle : DungeonFx.Circle, (Vector3)(at + o), Color.Lerp(VoidPale, VoidLight, Random.value), Gather, 0.6f, 0.15f, 0f, 20472, true);
                s.velocity = -o / Gather * 1.1f; s.drag = 0f;
            }
            var core = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)at, VoidDeep, Gather + 0.1f, 0.2f, radius * 0.6f, 0f, 20468, false);
            // the pull
            for (int k = 0; k < 2; k++)
                After(0.12f + 0.18f * k, () =>
                {
                    if (!Alive) return;
                    foreach (var e in Combat2D.EnemiesIn(at, radius * 1.8f))
                    {
                        if (e == null || e.Dead) continue;
                        Vector2 to = at - e.Body;
                        if (to.sqrMagnitude < 0.05f) continue;
                        _curType = type;
                        Hit(e, mult * 0.08f, to.normalized, 4f + 2f * lvl, type);
                        if (!Alive) return;
                    }
                });
            // the burst
            After(Gather, () =>
            {
                if (!Alive) return;
                _dc.Shake(0.16f + 0.04f * lvl, 0.12f); GameFeel.HitStop(0.04f); GameLook.Punch(0.3f + 0.1f * lvl);
                DungeonFx.Flash(at, VoidPale, radius * 3f, 1.5f, 0.4f);
                var w = BurstRing(spot, Vector2.zero, radius * 0.35f, radius * 0.5f, 1.3f, 0.45f);
                Paint(w, VoidHot, VoidPale, VoidLight, VoidDeep, 1.1f, 3f);
                var w2 = BurstRing(spot, Vector2.zero, radius * 0.2f, radius * 0.25f, 2f, 0.55f);
                Paint(w2, VoidHot, VoidPale, VoidLight, VoidDeep, 0.8f, 3f);
                for (int i = 0; i < 18; i++)
                {
                    Vector2 o = Random.insideUnitCircle.normalized;
                    var s = DungeonFx.Spawn(DungeonFx.Twinkle, (Vector3)at, Color.Lerp(VoidHot, VoidLight, Random.value), Random.Range(0.4f, 0.7f), 1f, 0.2f, 0f, 20472, true);
                    s.velocity = o * Random.Range(4f, 8f); s.drag = 4f;
                }
                for (int i = 0; i < 5; i++) { Vector2 o = Random.insideUnitCircle.normalized; GlowAura.Bolt(at, at + o * radius * Random.Range(0.8f, 1.3f), VoidPale, 20474, 0.06f, 0.18f); }
                var hitSet = new HashSet<EnemyController>();
                HitRound(at, radius * 1.25f, mult, 9f, type, hitSet);
                if (!spawnMore) return;
                // level 3: two more rifts on monsters nearby (or round about, if there aren't any)
                var near = new List<EnemyController>();
                foreach (var e in Combat2D.EnemiesIn(at, 5f)) if (e != null && !e.Dead && (e.Body - at).magnitude > radius * 0.8f) near.Add(e);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 p = i < near.Count ? near[i].Body : at + Random.insideUnitCircle.normalized * radius * 2f;
                    float delay = 0.12f + 0.12f * i;
                    After(delay, () => { if (Alive) Rift(p, radius * 0.7f, mult * 0.6f, lvl, false); });
                }
            });
        }

        // ───────────── Sunbreaker ─────────────
        static readonly Color SunHot = new Color(1f, 0.98f, 0.85f), SunPale = new Color(1f, 0.88f, 0.45f),
                              SunLight = new Color(1f, 0.68f, 0.15f), SunDeep = new Color(0.85f, 0.35f, 0.05f);

        /// Sunbreaker: a hammer-blow into the ground. Golden shockwaves roll out across the floor, cracks of light
        /// split from the impact, dust thrown up.
        ///   Level 1  one shockwave.
        ///   Level 2  a second, wider wave rolls out after it; longer cracks.
        ///   Level 3  a third wave, and a pillar of sunlight falls on every monster hit.
        bool UseSunbreaker(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float radius = m.radius * (1f + 0.15f * up);
            mult *= 1f + 0.3f * up;
            var type = Element.Light;
            _anim.PlayAttack(0.35f);
            // the wind-up: light gathering at the body
            DungeonFx.Flash(Body, SunPale, 2.5f, 0.8f, 0.2f);
            After(0.16f, () =>
            {
                if (!Alive) return;
                Vector2 at = Feet + dir * 0.4f;
                var spot = Spot(at, 2f);
                _dc.Shake(0.3f + 0.08f * up, 0.2f); GameFeel.HitStop(0.06f); GameLook.Punch(0.5f + 0.15f * up);
                DungeonFx.Flash(at, SunHot, radius * 3f, 1.8f + 0.3f * up, 0.45f);
                DungeonFx.Puff(at, new Color(0.75f, 0.65f, 0.5f, 0.7f), 14, 1.6f);
                // cracks of light splitting out
                int cracks = 7 + 2 * up;
                for (int i = 0; i < cracks; i++)
                {
                    float a = (i / (float)cracks) * 360f + Random.Range(-15f, 15f);
                    Vector2 o = Quaternion.Euler(0, 0, a) * Vector2.right; o.y *= 0.72f;
                    Vector2 mid = at + o * radius * Random.Range(0.35f, 0.55f) + Random.insideUnitCircle * 0.12f;
                    Vector2 end = at + o * radius * Random.Range(0.7f, 1f + 0.25f * up);
                    LabBeam.Line(at, mid, SunLight, 0.09f, 1.1f, 0.06f, 20400);
                    LabBeam.Line(mid, end, SunLight, 0.06f, 1f, 0.1f, 20400);
                }
                var hitSet = new HashSet<EnemyController>();
                int waves = 1 + up;
                for (int w = 0; w < waves; w++)
                {
                    float r = radius * (1f + 0.55f * w);
                    int wv = w;
                    After(0.17f * w, () =>
                    {
                        if (!Alive) return;
                        var ring = BurstRing(spot, Vector2.zero, r * 0.3f, r * 0.35f, 2f, 0.4f);
                        Paint(ring, SunHot, SunPale, SunLight, SunDeep, 1f - 0.1f * wv, 3f);
                        if (wv > 0) _dc.Shake(0.12f, 0.1f);
                        var before = new HashSet<EnemyController>(hitSet);
                        HitRound(at, r, mult * (wv == 0 ? 1f : 0.6f), 10f, type, hitSet);
                        if (lvl < 3) return;
                        foreach (var e in hitSet)
                        {
                            if (e == null || e.Dead || before.Contains(e)) continue;
                            var target = e;
                            After(0.15f, () => SunPillar(target, mult * 0.5f, type));
                        }
                    });
                }
            });
            return true;
        }

        void SunPillar(EnemyController e, float mult, Element type)
        {
            if (!Alive || e == null || e.Dead) return;
            Vector2 p = e.Body;
            LabBeam.Line(p + Vector2.up * 5f, p + Vector2.down * 0.3f, SunPale, 0.5f, 0.4f, 0.05f, 20480);
            DungeonFx.Flash(p, SunHot, 2.5f, 1.4f, 0.3f);
            DungeonFx.RingAt(p, new Color(1f, 0.85f, 0.4f, 0.7f), 0.7f, 0.25f);
            _curType = type;
            Hit(e, mult * S.MutatorAreaDamage, Vector2.down, 2f, type);
        }

        // ───────────── Petal Flurry ─────────────
        static readonly Color PetalHot = new Color(1f, 0.97f, 0.98f), PetalPale = new Color(1f, 0.78f, 0.9f),
                              PetalLight = new Color(1f, 0.42f, 0.72f), PetalDeep = new Color(0.82f, 0.1f, 0.45f);

        /// Petal Flurry: twin blades in a blur of quick slashes in front of you, forehand then backhand, shedding
        /// pink petals. Each slash hits.
        ///   Level 1  three slashes.
        ///   Level 2  five, wider.
        ///   Level 3  seven, and a finishing cross-cut — an X of light where you're aiming.
        bool UsePetalFlurry(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            int count = 3 + 2 * up;
            float radius = m.radius * Reach(m, 0) * (1f + 0.12f * up);
            float aim = Mathf.Atan2(dir.y, dir.x);
            var type = Element.Nature;
            const float Gap = 0.075f;
            _anim.PlayAttack(Gap * count + 0.1f);
            for (int i = 0; i < count; i++)
            {
                int k = i;
                After(Gap * i, () =>
                {
                    if (!Alive) return;
                    int spin = k % 2 == 0 ? -1 : 1;
                    float tilt = Random.Range(-0.25f, 0.25f);
                    var r = SlashRing.Play(transform, Body - Feet, aim + tilt - spin * 0.8f, spin);
                    r.Radius = radius * Random.Range(0.85f, 1.05f); r.Width = radius * 0.45f; r.Turns = 0.26f; r.SweepTime = 0.07f;
                    r.TrailLife = 0.28f; r.Grow = 0.4f; r.Drift = 0.12f; r.BladeThickness = 0.26f; r.BladeReach = 0.15f; r.BladeFrom = 0.45f;
                    r.StartFadeDeg = 25f; r.EndFadeDeg = 25f; r.Sparks = 0; r.LeafChance = 0f; r.LightIntensity = 0.35f;
                    Paint(r, PetalHot, PetalPale, PetalLight, PetalDeep, 1f + 0.1f * up, 2.8f);
                    for (int p = 0; p < 3 + up; p++)
                    {
                        Vector2 o = Quaternion.Euler(0, 0, Random.Range(-60f, 60f)) * dir;
                        var leaf = DungeonFx.Spawn(DungeonFx.Leaf, (Vector3)(Body + o * radius * Random.Range(0.6f, 1f)), Color.Lerp(PetalPale, PetalLight, Random.value), Random.Range(0.7f, 1.1f), 0.9f, 0.6f, Random.Range(0f, 360f), 20460, false);
                        leaf.velocity = o * Random.Range(1.5f, 3.5f); leaf.drag = 2.5f; leaf.spin = Random.Range(-300f, 300f); leaf.sway = 0.7f;
                    }
                    foreach (var e in Combat2D.EnemiesInCone(Body, dir, radius + 0.5f, 70f))
                    {
                        if (e == null || e.Dead) continue;
                        _curType = type;
                        Hit(e, mult * 0.45f * S.MutatorAreaDamage, dir, 2.5f, type);
                        if (!Alive) return;
                        DungeonFx.Sparks(e.Body, dir, PetalPale, 5);
                    }
                    _dc.Shake(0.04f, 0.04f);
                });
            }
            if (lvl >= 3)
                After(Gap * count + 0.06f, () =>
                {
                    if (!Alive) return;
                    Vector2 c = Body + dir * radius * 0.9f;
                    float s = radius * 0.9f;
                    Vector2 n = new Vector2(-dir.y, dir.x);
                    LabBeam.Line(c - (dir + n) * s * 0.6f, c + (dir + n) * s * 0.6f, PetalLight, 0.3f, 0.4f, 0.06f);
                    LabBeam.Line(c - (dir - n) * s * 0.6f, c + (dir - n) * s * 0.6f, PetalLight, 0.3f, 0.4f, 0.06f);
                    DungeonFx.Flash(c, PetalPale, 3f, 1.4f, 0.35f);
                    _dc.Shake(0.15f, 0.1f); GameFeel.HitStop(0.05f);
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 o = Random.insideUnitCircle.normalized;
                        var leaf = DungeonFx.Spawn(DungeonFx.Leaf, (Vector3)c, Color.Lerp(PetalPale, PetalDeep, Random.value), Random.Range(0.8f, 1.2f), 1f, 0.6f, Random.Range(0f, 360f), 20460, false);
                        leaf.velocity = o * Random.Range(3f, 6f); leaf.drag = 3f; leaf.spin = Random.Range(-400f, 400f); leaf.sway = 0.8f;
                    }
                    HitRound(c, s, mult * 1.2f, 9f, type);
                });
            return true;
        }

        // ═════════════════════════ BUFFS ═════════════════════════
        // Each runs for a while; LabBuffsTick drives what they do over time.

        float _emberUntil, _emberT, _emberMult, _emberR; int _emberLvl;
        readonly List<Vector3> _emberSteps = new List<Vector3>();   // x, y, time laid (level 3's burning footprints)
        Vector2 _emberLastStep;
        float _aegisUntil; int _aegisLvl; float _aegisMult; readonly List<SpriteRenderer> _aegisShards = new List<SpriteRenderer>(); float _aegisSpin;
        float _galeUntil, _galeSpeed, _galeT, _galeMult; int _galeLvl; readonly Dictionary<EnemyController, float> _galeCut = new Dictionary<EnemyController, float>();
        float _crimsonUntil, _crimsonBonus, _crimsonSteal; int _crimsonLvl;
        float _crownUntil, _crownFire, _crownMult, _crownT; int _crownLvl; readonly List<SpriteRenderer> _crownStars = new List<SpriteRenderer>();

        float LabSpeedMult { get { return Time.time < _galeUntil ? _galeSpeed : 1f; } }
        float LabHitMult() { return Time.time < _crimsonUntil ? 1f + _crimsonBonus : 1f; }

        void LabAfterHit(EnemyController e, float dmg)
        {
            if (Time.time >= _crimsonUntil || e == null) return;
            int heal = Mathf.Max(1, Mathf.RoundToInt(dmg * _crimsonSteal));
            if (S.Run != null && S.Run.hp < S.Stats.maxHp) S.DungeonHealPlayer(heal);
            // a red mote flies from it back to you
            var mote = DungeonFx.Spawn(DungeonFx.Circle, (Vector3)e.Body, new Color(1f, 0.2f, 0.25f), 0.35f, 0.5f, 0.15f, 0f, 20480, true);
            mote.velocity = (Body - e.Body) / 0.3f; mote.drag = 0f;
            if (_crimsonLvl >= 3 && e.Dead) { _crimsonUntil += 1f; DungeonFx.RingAt(Body, new Color(1f, 0.25f, 0.3f, 0.6f), 0.9f, 0.25f); }   // kills feed it
        }

        /// Frost Aegis: a shard takes the hit instead of you. True = the hit is gone.
        bool LabAbsorb(Vector2 from)
        {
            if (Time.time >= _aegisUntil || _aegisShards.Count == 0) return false;
            var s = _aegisShards[_aegisShards.Count - 1];
            _aegisShards.RemoveAt(_aegisShards.Count - 1);
            Vector2 p = s != null ? (Vector2)s.transform.position : Body;
            if (s != null) Destroy(s.gameObject);
            _invulnUntil = Time.time + T.hurtInvulnSeconds;
            DungeonFx.Flash(p, FrostGlow, 3f, 1.4f, 0.3f);
            DungeonFx.RingAt(Body, new Color(0.7f, 0.92f, 1f, 0.8f), 1.1f, 0.25f);
            for (int i = 0; i < 12; i++)
            {
                var sh = DungeonFx.Spawn(DungeonFx.Shard, (Vector3)p, Color.Lerp(FrostCore, FrostGlow, Random.value), Random.Range(0.35f, 0.6f), 0.9f, 0.3f, Random.Range(0f, 360f), 20480, true);
                sh.velocity = Random.insideUnitCircle.normalized * Random.Range(2.5f, 5f); sh.drag = 4f; sh.spin = Random.Range(-600f, 600f);
            }
            _dc.Shake(0.1f, 0.08f);
            if (_aegisLvl >= 2) HitRound(Body, 1.6f + 0.3f * (_aegisLvl - 2), _aegisMult * 0.6f, 9f, Element.Frost);   // it shatters outwards
            if (_aegisShards.Count == 0) _aegisUntil = Time.time;
            return true;
        }

        void LabBuffsTick()
        {
            float dt = Time.deltaTime;
            if (Time.time < _emberUntil) EmberTick(dt);
            AegisTick(dt);
            if (Time.time < _galeUntil) GaleTick(dt);
            CrownTick(dt);
        }

        // ───────────── Ember Mantle ─────────────
        static readonly Color EmberHot = new Color(1f, 0.85f, 0.5f), EmberPale = new Color(1f, 0.55f, 0.12f),
                              EmberLight = new Color(1f, 0.3f, 0.03f), EmberDeep = new Color(0.7f, 0.05f, 0.02f);

        /// Ember Mantle: wrap yourself in fire. You glow orange, rings of flame pulse out from your feet and burn
        /// everything close every half second; embers stream up off you.
        ///   Level 1  6 s.
        ///   Level 2  8 s, a wider, hotter burn.
        ///   Level 3  10 s, and your footsteps leave fire on the floor that burns what walks through it.
        bool UseEmberMantle(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float seconds = (m.duration > 0f ? m.duration : 6f) + 2f * up;
            _emberUntil = Time.time + seconds; _emberT = 0f; _emberLvl = lvl; _emberMult = mult * (1f + 0.3f * up);
            _emberR = m.radius * (1f + 0.25f * up); _emberSteps.Clear(); _emberLastStep = Feet;
            var aura = GlowAura.On(_anim.Renderer, seconds);
            aura.Inner = EmberPale; aura.Outer = EmberLight; aura.Fill = 0.15f; aura.Strength = 1f + 0.2f * up; aura.Spread = 0.35f + 0.08f * up; aura.CracklesPerSecond = 0f;
            var glow = DungeonFx.Flash(Body, EmberLight, 4f + up, 0.8f + 0.2f * up, 0f, transform);
            After(seconds, () => { if (glow != null) Destroy(glow.gameObject); });
            _anim.PlayAttack(0.3f);
            DungeonFx.Flash(Body, EmberHot, 5f, 1.5f, 0.35f);
            var w = BurstRing(transform, Vector2.zero, _emberR * 0.4f, _emberR * 0.5f, 1.2f, 0.45f);
            Paint(w, EmberHot, EmberPale, EmberLight, EmberDeep, 0.9f, 2.6f);
            DungeonFx.Embers(Body, EmberPale, 24, 0.6f);
            _dc.Shake(0.1f, 0.08f); GameLook.Punch(0.3f);
            return true;
        }

        void EmberTick(float dt)
        {
            if (Random.value < dt * (14f + 6f * _emberLvl)) DungeonFx.Embers(Body + Random.insideUnitCircle * 0.25f, Color.Lerp(EmberPale, EmberLight, Random.value), 1, 0.2f);
            if (_emberLvl >= 3 && (Feet - _emberLastStep).sqrMagnitude > 0.3f)
            {
                _emberLastStep = Feet;
                _emberSteps.Add(new Vector3(Feet.x, Feet.y, Time.time));
                DungeonFx.FloorEmbers(Feet, EmberLight, 3);
            }
            _emberT -= dt;
            if (_emberT > 0f) return;
            _emberT = 0.5f;
            var r = BurstRing(transform, Vector2.zero, _emberR * 0.35f, _emberR * 0.3f, 1.1f, 0.35f);
            Paint(r, EmberHot, EmberPale, EmberLight, EmberDeep, 0.6f + 0.1f * _emberLvl, 2f);
            HitRound(Feet, _emberR, _emberMult * 0.22f, 2f, Element.Fire);
            if (_emberLvl < 3) return;
            _emberSteps.RemoveAll(s => Time.time - s.z > 2.5f);
            var burned = new HashSet<EnemyController>();
            foreach (var s in _emberSteps)
            {
                Vector2 p = s;
                foreach (var e in Combat2D.EnemiesIn(p, 0.6f))
                {
                    if (e == null || e.Dead || burned.Contains(e)) continue;
                    burned.Add(e); _curType = Element.Fire;
                    Hit(e, _emberMult * 0.15f, Vector2.up, 0.5f, Element.Fire);
                    if (!Alive) return;
                }
            }
        }

        // ───────────── Frost Aegis ─────────────
        /// Frost Aegis: shards of ice circle you, each one taking a hit for you and shattering.
        ///   Level 1  10 s, three shards.
        ///   Level 2  12 s, four, and each one that breaks bursts outwards, hitting what's near.
        ///   Level 3  14 s, five; a bigger burst, and any shards left at the end fly out at the nearest monsters.
        bool UseFrostAegis(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float seconds = (m.duration > 0f ? m.duration : 10f) + 2f * up;
            ClearShards(_aegisShards);
            _aegisUntil = Time.time + seconds; _aegisLvl = lvl; _aegisMult = mult;
            for (int i = 0; i < 3 + up; i++)
            {
                var go = new GameObject("aegis shard");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = DungeonFx.Shard; sr.color = Color.Lerp(FrostCore, FrostGlow, 0.3f);
                if (FxMaterials.Glow != null) sr.sharedMaterial = FxMaterials.Glow;
                go.transform.localScale = Vector3.one * 2f;
                _aegisShards.Add(sr);
            }
            var aura = GlowAura.On(_anim.Renderer, seconds);
            aura.Inner = FrostCore; aura.Outer = FrostGlow; aura.Fill = 0f; aura.Strength = 0.6f; aura.Spread = 0.22f; aura.CracklesPerSecond = 0f;
            _anim.PlayAttack(0.3f);
            DungeonFx.Flash(Body, FrostCore, 4f, 1.3f, 0.35f);
            var w = BurstRing(transform, Body - Feet, 0.6f, 0.5f, 1f, 0.4f);
            Paint(w, FrostCore, FrostCore, FrostGlow, FrostDeep, 0.9f, 2.5f);
            for (int i = 0; i < 14; i++)
            {
                var sh = DungeonFx.Spawn(DungeonFx.Shard, (Vector3)(Body + Random.insideUnitCircle.normalized * 1.4f), FrostCore, 0.35f, 0.8f, 0.2f, Random.Range(0f, 360f), 20480, true);
                sh.velocity = (Body - (Vector2)sh.transform.position) / 0.35f; sh.drag = 0f;   // the ice gathers in
            }
            return true;
        }

        void AegisTick(float dt)
        {
            if (_aegisShards.Count == 0) return;
            if (Time.time >= _aegisUntil)
            {
                // level 3: what's left flies at the nearest monsters
                if (_aegisLvl >= 3)
                    foreach (var s in _aegisShards)
                    {
                        if (s == null) continue;
                        EnemyController best = null; float bd = 36f;
                        foreach (var e in Combat2D.EnemiesIn(Body, 6f)) { if (e == null || e.Dead) continue; float d = (e.Body - Body).sqrMagnitude; if (d < bd) { bd = d; best = e; } }
                        Vector2 p = s.transform.position;
                        if (best != null)
                        {
                            LabBeam.Line(p, best.Body, FrostGlow, 0.18f, 0.25f, 0.06f);
                            _curType = Element.Frost;
                            Hit(best, _aegisMult * 0.6f, (best.Body - p).normalized, 6f, Element.Frost);
                            if (!Alive) break;
                        }
                    }
                ClearShards(_aegisShards);
                return;
            }
            _aegisSpin += dt * 2.6f;
            float left = _aegisUntil - Time.time;
            float fade = Mathf.Clamp01(left / 0.5f);
            int n = _aegisShards.Count;
            int order = _anim.Renderer.sortingOrder;
            Vector2 c = Body - Feet;
            for (int i = 0; i < n; i++)
            {
                var s = _aegisShards[i];
                if (s == null) continue;
                float a = _aegisSpin + i * Mathf.PI * 2f / n;
                float bob = Mathf.Sin(Time.time * 3f + i) * 0.06f;
                s.transform.localPosition = c + new Vector2(Mathf.Cos(a) * 0.95f, Mathf.Sin(a) * 0.42f + bob);
                s.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Cos(a) * 20f);
                s.sortingOrder = Mathf.Sin(a) < 0f ? order + 2 : order - 2;   // in front on the near side
                var col = Color.Lerp(FrostCore, FrostGlow, 0.3f + 0.2f * Mathf.Sin(Time.time * 5f + i)); col.a = fade; s.color = col;
            }
            if (Random.value < dt * 6f && n > 0) { var s = _aegisShards[Random.Range(0, n)]; if (s != null) DungeonFx.Spawn(DungeonFx.Twinkle, s.transform.position, FrostCore, 0.3f, 0.6f, 0.1f, 0f, 20482, true); }
        }

        static void ClearShards(List<SpriteRenderer> list) { foreach (var s in list) if (s != null) Destroy(s.gameObject); list.Clear(); }

        // ───────────── Gale Rush ─────────────
        static readonly Color GaleHot = new Color(0.95f, 1f, 0.98f), GalePale = new Color(0.75f, 1f, 0.9f), GaleLight = new Color(0.4f, 0.95f, 0.8f), GaleDeep = new Color(0.15f, 0.6f, 0.65f);

        /// Gale Rush: the wind takes you. You run much faster, leave after-images and streaks of wind behind you, and
        /// your dash comes back quicker.
        ///   Level 1  6 s, +45% speed.
        ///   Level 2  8 s, +60%, and the wind round you cuts monsters you run past.
        ///   Level 3  10 s, +75%, a wider cut, and a gust on cast that throws monsters back.
        bool UseGaleRush(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float seconds = (m.duration > 0f ? m.duration : 6f) + 2f * up;
            _galeUntil = Time.time + seconds; _galeSpeed = 1.45f + 0.15f * up; _galeLvl = lvl; _galeMult = mult; _galeT = 0f; _galeCut.Clear();
            var aura = GlowAura.On(_anim.Renderer, seconds);
            aura.Inner = GaleHot; aura.Outer = GaleLight; aura.Fill = 0f; aura.Strength = 0.55f; aura.Spread = 0.2f; aura.CracklesPerSecond = 0f;
            _anim.PlayAttack(0.2f);
            // a whirl of wind round you
            for (int i = 0; i < 3; i++)
            {
                var r = SlashRing.Play(transform, Body - Feet, i * 2.1f, 1);
                r.ShowBlade = false; r.Radius = 0.9f + 0.25f * i; r.Width = 0.12f; r.Turns = 1.4f; r.SweepTime = 0.35f;
                r.TrailLife = 0.3f; r.Drift = 0.4f; r.Grow = 0.3f; r.StartFadeDeg = 80f; r.EndFadeDeg = 80f; r.Sparks = 0; r.LeafChance = 0f; r.LightIntensity = 0.2f;
                Paint(r, GaleHot, GalePale, GaleLight, GaleDeep, 1.1f, 2.2f);
            }
            if (lvl >= 3)
            {
                var g = BurstRing(transform, Vector2.zero, 0.6f, 0.4f, 2.2f, 0.4f);
                Paint(g, GaleHot, GalePale, GaleLight, GaleDeep, 0.9f, 2.4f);
                HitRound(Feet, 2.4f, mult * 0.5f, 16f, Element.Nature);
                _dc.Shake(0.12f, 0.1f);
            }
            return true;
        }

        void GaleTick(float dt)
        {
            _dashCd -= dt * 0.5f;   // the dash comes back half as fast again
            bool moving = _move.sqrMagnitude > 0.05f;
            _galeT -= dt;
            if (_galeT <= 0f && moving)
            {
                _galeT = 0.07f;
                DungeonFx.Ghost(_anim.Renderer, new Color(0.6f, 1f, 0.9f, 0.45f), 0.25f, true, false);
                Vector2 back = -_move.normalized;
                Vector2 o = new Vector2(-back.y, back.x) * Random.Range(-0.35f, 0.35f);
                Vector2 p = Body + o + Vector2.up * Random.Range(-0.25f, 0.25f);
                LabBeam.Line(p, p + back * Random.Range(0.8f, 1.5f), GaleLight, 0.05f, 0.25f, 0f, 20400);
            }
            if (_galeLvl < 2 || !moving) return;
            float r = 0.9f + 0.3f * (_galeLvl - 2);
            foreach (var e in Combat2D.EnemiesIn(Body, r))
            {
                if (e == null || e.Dead) continue;
                float last;
                if (_galeCut.TryGetValue(e, out last) && Time.time - last < 0.6f) continue;
                _galeCut[e] = Time.time;
                Vector2 d = (e.Body - Body).sqrMagnitude > 0.001f ? (e.Body - Body).normalized : _move.normalized;
                DungeonFx.SlashAt(e.Body, _move.normalized, GalePale, 0.7f);
                _curType = Element.Nature;
                Hit(e, _galeMult * 0.35f, d, 4f, Element.Nature);
                if (!Alive) return;
            }
        }

        // ───────────── Crimson Hunger ─────────────
        static readonly Color CrimsonHot = new Color(1f, 0.7f, 0.7f), CrimsonDeep = new Color(0.75f, 0.02f, 0.12f);

        /// Crimson Hunger: a red hunger. You glow deep crimson with dark rings circling you; you hit harder, and every
        /// hit heals you a little (a red mote flies back to you).
        ///   Level 1  6 s: +15% damage, heal 8% of damage dealt.
        ///   Level 2  8 s: +25%, 12%.
        ///   Level 3  10 s: +35%, 16%, and every kill adds a second to it.
        bool UseCrimsonHunger(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float seconds = (m.duration > 0f ? m.duration : 6f) + 2f * up;
            _crimsonUntil = Time.time + seconds; _crimsonLvl = lvl; _crimsonBonus = 0.15f + 0.1f * up; _crimsonSteal = 0.08f + 0.04f * up;
            var aura = GlowAura.On(_anim.Renderer, seconds);
            aura.Inner = CrimsonHot; aura.Outer = CrimsonDeep; aura.Fill = 0.12f; aura.Strength = 1.1f + 0.2f * up; aura.Spread = 0.4f + 0.08f * up; aura.CracklesPerSecond = 0f;
            float size = Mathf.Max(0.8f, (Body.y - Feet.y) / 0.45f) * 0.7f;
            var rings = LightRings.Play(transform, Body - Feet, size, 1 + up, seconds);
            rings.Head = CrimsonHot; rings.Trail = CrimsonDeep; rings.SparklesPerSecond = 4f; rings.Heat = 2.6f;
            var glow = DungeonFx.Flash(Body, CrimsonDeep, 3.5f + up, 0.7f + 0.15f * up, 0f, transform);
            After(seconds, () => { if (glow != null) Destroy(glow.gameObject); });
            _anim.PlayAttack(0.3f);
            DungeonFx.Flash(Body, new Color(1f, 0.3f, 0.35f), 5f, 1.4f, 0.35f);
            // the hunger draws in: red motes pulled into you
            for (int i = 0; i < 20 + 6 * up; i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized * Random.Range(1.2f, 2.2f);
                var s = DungeonFx.Spawn(DungeonFx.Circle, (Vector3)(Body + o), Color.Lerp(CrimsonHot, CrimsonDeep, Random.value), 0.4f, 0.5f, 0.15f, 0f, 20480, true);
                s.velocity = -o / 0.4f; s.drag = 0f;
            }
            _dc.Shake(0.1f, 0.08f); GameLook.Punch(0.3f);
            return true;
        }

        // ───────────── Astral Crown ─────────────
        static readonly Color StarHot = new Color(1f, 0.97f, 0.85f), StarGold = new Color(1f, 0.82f, 0.4f), StarViolet = new Color(0.65f, 0.5f, 1f);

        /// Astral Crown: a crown of stars circles above your head, and each one fires a bolt of starlight at the
        /// nearest monster in turn.
        ///   Level 1  8 s, three stars, a bolt every 0.6 s.
        ///   Level 2  10 s, four stars, a bolt every 0.45 s.
        ///   Level 3  12 s, five, every 0.35 s, and each bolt bursts into a little nova where it lands.
        bool UseAstralCrown(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float seconds = (m.duration > 0f ? m.duration : 8f) + 2f * up;
            ClearShards(_crownStars);
            _crownUntil = Time.time + seconds; _crownLvl = lvl; _crownMult = mult; _crownFire = 0.6f - 0.125f * up; _crownT = 0.2f;
            for (int i = 0; i < 3 + up; i++)
            {
                var go = new GameObject("crown star");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = DungeonFx.Twinkle; sr.color = StarGold;
                if (FxMaterials.Glow != null) sr.sharedMaterial = FxMaterials.Glow;
                go.transform.localScale = Vector3.one * 2.3f;
                _crownStars.Add(sr);
            }
            _anim.PlayAttack(0.3f);
            DungeonFx.Flash(Body, StarGold, 4f, 1.2f, 0.35f);
            for (int i = 0; i < 16; i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized;
                var s = DungeonFx.Spawn(DungeonFx.Twinkle, (Vector3)Body, Color.Lerp(StarGold, StarViolet, Random.value), 0.5f, 0.9f, 0.2f, 0f, 20480, true);
                s.velocity = o * Random.Range(2f, 4f) + Vector2.up * 2f; s.drag = 3f;
            }
            return true;
        }

        void CrownTick(float dt)
        {
            if (_crownStars.Count == 0) return;
            if (Time.time >= _crownUntil) { foreach (var s in _crownStars) if (s != null) DungeonFx.Spawn(DungeonFx.Twinkle, s.transform.position, StarGold, 0.4f, 1.4f, 0.1f, 0f, 20482, true); ClearShards(_crownStars); return; }
            int n = _crownStars.Count;
            int order = _anim.Renderer.sortingOrder;
            float top = (Body.y - Feet.y) * 2.1f + 0.2f;
            for (int i = 0; i < n; i++)
            {
                var s = _crownStars[i];
                float a = Time.time * 1.8f + i * Mathf.PI * 2f / n;
                s.transform.localPosition = new Vector2(Mathf.Cos(a) * 0.55f, top + Mathf.Sin(a) * 0.16f);
                s.transform.localRotation = Quaternion.Euler(0, 0, Time.time * 90f + i * 30f);
                s.sortingOrder = Mathf.Sin(a) < 0f ? order + 2 : order - 2;
                float tw = 0.85f + 0.15f * Mathf.Sin(Time.time * 11f + i * 2f);
                s.color = Color.Lerp(StarGold, StarHot, 0.4f + 0.3f * Mathf.Sin(Time.time * 7f + i)) * tw;
            }
            _crownT -= dt;
            if (_crownT > 0f) return;
            // the next star in turn fires at the nearest monster
            EnemyController best = null; float bd = 7f * 7f;
            foreach (var e in Combat2D.EnemiesIn(Body, 7f)) { if (e == null || e.Dead) continue; float d = (e.Body - Body).sqrMagnitude; if (d < bd) { bd = d; best = e; } }
            if (best == null) { _crownT = 0.15f; return; }
            _crownT = _crownFire;
            var star = _crownStars[Random.Range(0, n)];
            StarBolt(star.transform.position, best);
        }

        void StarBolt(Vector2 from, EnemyController target)
        {
            Vector2 to0 = target.Body - from;
            // it leaves the star going up and sideways, then curves in
            Vector2 side = new Vector2(-to0.y, to0.x).normalized * (Random.value < 0.5f ? -1f : 1f);
            var mover = FxMover.Throw(from, (side * 3f + Vector2.up * 2f), 1.2f);
            mover.Linger = 0.05f;
            float trailT = 0f; bool done = false;
            mover.Tick = pos =>
            {
                if (done || !Alive) return;
                if (target == null || target.Dead) { mover.FlyTime = 0f; return; }
                Vector2 to = target.Body - pos;
                mover.Velocity = Vector2.Lerp(mover.Velocity, to.normalized * 13f, Time.deltaTime * 9f);   // homes in
                trailT -= Time.deltaTime;
                if (trailT <= 0f)
                {
                    trailT = 0.02f;
                    DungeonFx.Spawn(DungeonFx.Twinkle, (Vector3)pos, Color.Lerp(StarGold, StarViolet, Random.value * 0.6f), 0.3f, 0.9f, 0.1f, Random.Range(0f, 90f), 20482, true);
                }
                if (to.magnitude > 0.35f) return;
                done = true; mover.FlyTime = 0f;
                DungeonFx.Flash(pos, StarGold, 2f, 1f, 0.2f);
                _curType = Element.Light;
                Hit(target, _crownMult * 0.3f, to.normalized, 3f, Element.Light);
                if (!Alive || _crownLvl < 3) return;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 o = Random.insideUnitCircle.normalized;
                    var s = DungeonFx.Spawn(DungeonFx.Twinkle, (Vector3)pos, Color.Lerp(StarHot, StarViolet, Random.value), 0.4f, 1f, 0.2f, 0f, 20482, true);
                    s.velocity = o * Random.Range(3f, 5f); s.drag = 4f;
                }
                DungeonFx.RingAt(pos, new Color(0.8f, 0.7f, 1f, 0.6f), 0.8f, 0.2f);
                var hitSet = new HashSet<EnemyController> { target };
                HitRound(pos, 1f, _crownMult * 0.2f, 4f, Element.Light, hitSet);
            };
        }
    }
}
