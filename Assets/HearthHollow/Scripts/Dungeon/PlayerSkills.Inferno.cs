// Hearth & Hollow — Inferno Crescent: Verdant Crescent's great swing all the way round you, remade in fire.
// The trail is drawn by Rendering/FireTrail.shader (SlashRing.FireMaterial): churning noise rising through it, so
// it burns rather than glows — white-yellow at the blade, cooling to orange and a ragged deep red, breaking into
// tongues that lift and gutter out. Over it: flame licks rising off the blade, sparks, embers drifting up, smoke
// rolling off the top, and the swing lights the room a flickering orange.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        static readonly Color FireWhite = new Color(1f, 0.96f, 0.82f), FireYellow = new Color(1f, 0.78f, 0.28f),
                              FireOrange = new Color(1f, 0.45f, 0.08f), FireRed = new Color(0.72f, 0.1f, 0.03f),
                              Smoke = new Color(0.13f, 0.11f, 0.1f);

        /// Inferno Crescent: one huge swing of fire all the way round you; everything the blade passes is hit and flung out.
        ///   Level 1  the swing.
        ///   Level 2  further and harder, a ring of fire just inside it, the floor left burning, and what it hits keeps
        ///            burning (three ticks).
        ///   Level 3  further again; it goes round almost twice (hitting a second time) and ends in a firestorm — a
        ///            ring of fire bursting outwards and a column of flame roaring up from you.
        bool UseInfernoCrescent(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float sweep = (m.duration > 0f ? m.duration : 0.5f) * (1f + 0.15f * up);
            float radius = m.radius * Reach(m, 0) * (1f + 0.22f * up);
            mult *= 1f + 0.35f * up;
            float turns = (m.value > 0f ? m.value : 1.15f) + (lvl >= 3 ? 0.6f : 0.1f * up);
            int spin = dir.x >= 0f ? -1 : 1;
            float aim = Mathf.Atan2(dir.y, dir.x);
            float start = aim - spin * 0.35f * Mathf.PI;
            Vector2 lift = Body - Feet;
            var type = Element.Fire;
            _anim.PlayAttack(sweep);

            var ring = SlashRing.Play(transform, lift, start, spin);
            ring.Look = SlashRing.FireMaterial;
            ring.Radius = radius; ring.SweepTime = sweep; ring.Turns = turns;
            ring.Width = radius * (0.5f + 0.05f * up);
            ring.TrailLife = 0.8f + 0.1f * up;     // fire hangs in the air longer than a wind-slash
            ring.Grow = 0.45f; ring.Drift = 0.06f; ring.Rise = 0.9f + 0.25f * up;
            ring.StartFadeDeg = 45f; ring.EndFadeDeg = 40f;
            ring.HotColour = FireWhite; ring.PaleColour = FireYellow; ring.LightColour = FireOrange; ring.DeepColour = FireRed;
            ring.HeatScale = 1f + 0.08f * up; ring.HeadHeat = 1.8f + 0.2f * up;
            ring.BladeThickness = 0.38f + 0.08f * up; ring.BladeReach = 0.2f + 0.12f * up; ring.BladeFrom = 0.3f;
            ring.LightIntensity = 0.45f + 0.12f * up;
            ring.Sparks = 2 + up; ring.LeafChance = 0f;
            float squash = ring.Squash;

            // level 2+: a second ring of fire just inside, a beat behind
            if (lvl >= 2)
            {
                var inner = SlashRing.Play(transform, lift, start, spin);
                inner.Look = SlashRing.FireMaterial; inner.Delay = 0.06f; inner.ShowBlade = false;
                inner.Radius = radius * 0.6f; inner.Width = radius * 0.28f; inner.SweepTime = sweep; inner.Turns = turns * 0.92f;
                inner.TrailLife = 0.6f; inner.Grow = 0.6f; inner.Rise = 0.7f; inner.Drift = 0.05f;
                inner.HeatScale = 0.85f; inner.Sparks = 0; inner.LeafChance = 0f;
            }

            // a flickering orange light on you while it burns
            var glow = DungeonFx.Flash(Body, FireOrange, radius * 2.2f, 0.55f + 0.12f * up, 0f, transform);
            float glowFor = sweep + ring.TrailLife;
            for (int i = 1; i <= 12; i++)
            {
                float at = glowFor * i / 12f;
                After(at, () => { if (glow != null) glow.intensity = (0.55f + 0.12f * up) * (1f - at / glowFor) * Random.Range(0.75f, 1.15f); });
            }
            After(glowFor, () => { if (glow != null) Destroy(glow.gameObject); });

            // as the blade goes: flames lick up off it, smoke rolls off the top, the floor catches; and it hits
            var hitAt = new Dictionary<EnemyController, float>();
            bool twice = lvl >= 3;
            float lastFx = start;
            ring.Swept = (a0, a1) =>
            {
                if (!Alive) return;
                Vector2 c = ring.Centre;
                if (Mathf.Abs(a1 - lastFx) > 9f * Mathf.Deg2Rad)
                {
                    lastFx = a1;
                    Vector2 at = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1) * squash) * radius * Random.Range(0.75f, 0.95f);
                    FlameLicks(at, 2 + up, 1f + 0.15f * up);
                    SmokePuff(at + Vector2.up * 0.6f, 0.9f + 0.2f * up);
                    if (lvl >= 2) DungeonFx.FloorEmbers(at - lift, FireOrange, 2);
                }
                float swept = Mathf.Abs(a1 - start);
                foreach (var e in Combat2D.EnemiesIn(c, radius + 0.8f))
                {
                    if (e == null || e.Dead) continue;
                    float last;
                    bool before = hitAt.TryGetValue(e, out last);
                    if (before && (!twice || swept - last < Mathf.PI * 1.8f)) continue;
                    Vector2 off = e.Body - c;
                    Vector2 flat = new Vector2(off.x, off.y / squash);
                    if (flat.magnitude > radius + 0.6f) continue;
                    float ea = Mathf.Atan2(flat.y, flat.x);
                    float ahead = Mathf.Repeat(spin * (ea - a0), Mathf.PI * 2f);
                    if (ahead > Mathf.Abs(a1 - a0) && flat.magnitude > 0.5f) continue;
                    hitAt[e] = swept;
                    _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : dir, 8f + 2f * up, type);
                    if (!Alive) return;
                    DungeonFx.Sparks(e.Body, off, FireYellow, 10 + 5 * up);
                    FlameLicks(e.Body, 3 + up, 1.1f);
                    if (lvl >= 2 && !before) Burn(e, mult * 0.15f, 3);
                }
            };

            _dc.Shake(0.1f + 0.04f * up, 0.08f + 0.03f * up);
            DungeonFx.Flash(Body, FireYellow, radius * 1.6f, 0.6f + 0.15f * up, 0.3f);
            After(sweep * 0.92f, () => GameLook.Punch(0.3f + 0.15f * up));

            if (lvl >= 3)
            {
                float stormMult = mult * 0.6f;
                After(sweep * 0.95f, () => Firestorm(radius, stormMult, hitAt, lift, squash));
            }
            return true;
        }

        /// Level 3's end: a ring of fire bursts outwards and a column of flame roars up from you.
        void Firestorm(float radius, float mult, Dictionary<EnemyController, float> already, Vector2 lift, float squash)
        {
            if (!Alive) return;
            var wave = BurstRing(transform, lift, radius, radius * 0.5f, 0.75f, 0.75f);
            wave.Look = SlashRing.FireMaterial; wave.Rise = 1.2f; wave.HeatScale = 1.15f;
            var wave2 = BurstRing(transform, lift, radius * 0.5f, radius * 0.45f, 1.2f, 0.6f);
            wave2.Look = SlashRing.FireMaterial; wave2.Rise = 1.4f;
            _dc.Shake(0.3f, 0.2f); GameFeel.HitStop(0.06f); GameLook.Punch(0.6f);
            DungeonFx.Flash(Body, FireWhite, radius * 3f, 2f, 0.5f);
            DungeonFx.Flash(Body, FireOrange, radius * 3.5f, 1.2f, 1.4f);
            // the column: flames pouring upwards from you, smoke billowing from its top
            for (int i = 0; i < 14; i++)
            {
                float d = i * 0.04f; bool smoke = i % 3 == 0;
                After(d, () =>
                {
                    if (!Alive) return;
                    FlameLicks(Feet + new Vector2(Random.Range(-0.35f, 0.35f), 0.2f), 4, 1.8f, 4.5f);
                    if (smoke) SmokePuff(Body + Vector2.up * Random.Range(1.6f, 2.4f), 1.6f);
                });
            }
            for (int i = 0; i < 14; i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized;
                DungeonFx.FloorEmbers(Feet + new Vector2(o.x, o.y * squash) * radius * Random.Range(0.4f, 1.2f), FireOrange, 2);
            }
            DungeonFx.Embers(Body, FireYellow, 40, radius * 0.6f);
            Vector2 c = Body;
            foreach (var e in Combat2D.EnemiesIn(c, radius * 1.7f + 0.5f))
            {
                if (e == null || e.Dead || already.ContainsKey(e)) continue;
                Vector2 off = e.Body - c;
                if (new Vector2(off.x, off.y / squash).magnitude > radius * 1.7f + 0.5f) continue;
                _curType = Element.Fire;
                Hit(e, mult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : Vector2.up, 10f, Element.Fire);
                if (!Alive) return;
                Burn(e, mult * 0.25f, 3);
            }
        }

        /// Tongues of flame rising from a spot: short-lived yellow cores, longer orange and red that climb and shrink.
        static void FlameLicks(Vector2 at, int count, float size, float rise = 2.6f)
        {
            for (int i = 0; i < count; i++)
            {
                float hot = Random.value;
                Color c = hot < 0.35f ? FireYellow : hot < 0.8f ? FireOrange : FireRed;
                float life = hot < 0.35f ? Random.Range(0.18f, 0.3f) : Random.Range(0.3f, 0.55f);
                var f = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)(at + Random.insideUnitCircle * 0.25f * size), c, life,
                                        Random.Range(0.45f, 0.75f) * size, 0.08f, 0f, 20430, true);
                f.velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.6f, 1f) * rise);
                f.accel = new Vector2(0f, rise * 1.2f); f.drag = 1.5f;
                f.sway = Random.Range(0.3f, 0.8f);
            }
        }

        /// Smoke: dark, soft, rolling upwards and spreading as it thins.
        static void SmokePuff(Vector2 at, float size)
        {
            var s = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)(at + Random.insideUnitCircle * 0.2f), new Color(Smoke.r, Smoke.g, Smoke.b, Random.Range(0.4f, 0.6f)),
                                    Random.Range(1.3f, 2.1f), Random.Range(0.6f, 0.9f) * size, Random.Range(2f, 2.8f) * size, Random.Range(0f, 360f), 20390, false);
            s.velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.7f, 1.2f));
            s.drag = 0.6f; s.spin = Random.Range(-30f, 30f); s.sway = 0.25f;
        }

        /// Keep burning a monster: a few ticks of fire damage, a flame licking off it each time.
        void Burn(EnemyController e, float mult, int ticks)
        {
            for (int i = 1; i <= ticks; i++)
                After(0.5f * i, () =>
                {
                    if (!Alive || e == null || e.Dead) return;
                    FlameLicks(e.Body, 3, 0.8f);
                    _curType = Element.Fire;
                    Hit(e, mult, Vector2.up, 0.3f, Element.Fire);
                });
        }
    }
}
