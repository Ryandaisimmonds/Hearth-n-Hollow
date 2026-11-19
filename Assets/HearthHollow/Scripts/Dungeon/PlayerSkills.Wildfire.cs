// Hearth & Hollow — Wildfire Crescent: halfway between Verdant Crescent and Inferno Crescent. The same great swing
// all the way round you, but two layers: Verdant's sharp, streaked green slash at the front (it cuts) that catches
// alight behind the blade, burning on as green-gold fire (Inferno's shader, SlashRing.GreenFireMaterial) that rises
// and tears into tongues. Leaves fly off it and burn as they fall; embers and pale smoke drift up.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        static readonly Color WildGold = new Color(1f, 0.92f, 0.5f), WildLime = new Color(0.78f, 1f, 0.3f),
                              WildGreen = new Color(0.3f, 0.92f, 0.15f), WildDeep = new Color(0.04f, 0.5f, 0.12f),
                              WildSmoke = new Color(0.32f, 0.36f, 0.3f);

        /// Wildfire Crescent: one huge swing round you that cuts like Verdant's and leaves green fire like Inferno's.
        ///   Level 1  the swing: a green slash that catches into green-gold flame.
        ///   Level 2  further, harder; Verdant's echo swing inside and wind lines outside; burning leaves; what it hits
        ///            keeps burning (three ticks).
        ///   Level 3  further again, nearly twice round (hitting a second time), and it ends in a green firestorm — a
        ///            ring of green fire and a storm of burning leaves bursting outwards.
        bool UseWildfireCrescent(MoveData m, Vector2 dir, float mult)
        {
            int lvl = LabLevel(m), up = lvl - 1;
            float sweep = (m.duration > 0f ? m.duration : 0.5f) * (1f + 0.15f * up);
            float radius = m.radius * Reach(m, 0) * (1f + 0.24f * up);
            mult *= 1f + 0.35f * up;
            float turns = (m.value > 0f ? m.value : 1.15f) + (lvl >= 3 ? 0.6f : 0.12f * up);
            int spin = dir.x >= 0f ? -1 : 1;
            float aim = Mathf.Atan2(dir.y, dir.x);
            float start = aim - spin * 0.35f * Mathf.PI;
            Vector2 lift = Body - Feet;
            var type = Element.Nature;
            _anim.PlayAttack(sweep);

            // the cut: Verdant's slash, sharp and short-lived, with the blade
            var ring = SlashRing.Play(transform, lift, start, spin);
            ring.Radius = radius; ring.SweepTime = sweep; ring.Turns = turns;
            ring.Width = radius * (0.42f + 0.05f * up); ring.TrailLife = 0.32f; ring.Grow = 0.35f;
            ring.HotColour = new Color(1f, 1f, 0.9f); ring.PaleColour = WildLime; ring.LightColour = WildGreen; ring.DeepColour = WildDeep;
            ring.HeatScale = 1.05f + 0.12f * up; ring.HeadHeat = 3f + 0.4f * up;
            ring.BladeThickness = 0.36f + 0.08f * up; ring.BladeReach = 0.18f + 0.15f * up; ring.BladeFrom = 0.3f - 0.05f * up;
            ring.LightIntensity = 0.45f + 0.12f * up;
            ring.Sparks = 1 + up; ring.LeafChance = 0f;   // its leaves burn: thrown below
            float squash = ring.Squash;

            // the fire it leaves: the same sweep a beat behind, burning on green-gold, rising
            var fire = SlashRing.Play(transform, lift, start, spin);
            fire.Look = SlashRing.GreenFireMaterial; fire.ShowBlade = false; fire.Delay = 0.03f;
            fire.Radius = radius * 0.97f; fire.SweepTime = sweep; fire.Turns = turns * 0.97f;
            fire.Width = radius * (0.5f + 0.05f * up); fire.TrailLife = 0.75f + 0.1f * up;
            fire.Grow = 0.45f; fire.Drift = 0.07f; fire.Rise = 0.75f + 0.2f * up;
            fire.StartFadeDeg = 60f; fire.EndFadeDeg = 45f; fire.HeatScale = 0.95f + 0.08f * up;
            fire.Sparks = 0; fire.LeafChance = 0f;

            // level 2+: Verdant's echo inside and wind lines outside
            if (lvl >= 2)
            {
                var echo = SlashRing.Play(transform, lift, start, spin);
                echo.Delay = 0.05f; echo.ShowBlade = false;
                echo.Radius = radius * 0.62f; echo.Width = radius * 0.28f; echo.SweepTime = sweep; echo.Turns = turns * 0.92f;
                echo.TrailLife = 0.4f; echo.Sparks = 0; echo.LeafChance = 0f;
                CrescentGreen(echo, lvl); echo.HeatScale *= 0.8f;
                int lines = lvl >= 3 ? 4 : 2;
                for (int i = 0; i < lines; i++)
                {
                    var w = SlashRing.Play(transform, lift, start + spin * (0.25f + 0.12f * i), spin);
                    w.Delay = 0.03f + 0.025f * i; w.ShowBlade = false;
                    w.Radius = radius * (1.1f + 0.075f * i); w.Width = radius * 0.05f;
                    w.SweepTime = sweep * 0.95f; w.Turns = turns * (0.8f - 0.08f * i);
                    w.TrailLife = 0.3f; w.Drift = 0.35f; w.Grow = 0.2f; w.StartFadeDeg = 70f;
                    w.Sparks = 0; w.LeafChance = 0f;
                    CrescentGreen(w, lvl);
                }
            }

            // a flickering green-gold light while it burns
            var glow = DungeonFx.Flash(Body, WildGreen, radius * 2.2f, 0.5f + 0.12f * up, 0f, transform);
            float glowFor = sweep + fire.TrailLife;
            for (int i = 1; i <= 12; i++)
            {
                float at = glowFor * i / 12f;
                After(at, () => { if (glow != null) glow.intensity = (0.5f + 0.12f * up) * (1f - at / glowFor) * Random.Range(0.75f, 1.15f); });
            }
            After(glowFor, () => { if (glow != null) Destroy(glow.gameObject); });

            var hitAt = new Dictionary<EnemyController, float>();
            bool twice = lvl >= 3;
            float lastFx = start;
            ring.Swept = (a0, a1) =>
            {
                if (!Alive) return;
                Vector2 c = ring.Centre;
                if (Mathf.Abs(a1 - lastFx) > 10f * Mathf.Deg2Rad)
                {
                    lastFx = a1;
                    Vector2 at = c + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1) * squash) * radius * Random.Range(0.75f, 0.95f);
                    GreenLicks(at, 1 + up, 0.9f + 0.1f * up);
                    BurningLeaf(at, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1) * squash));
                    if (Random.value < 0.5f) WildSmokePuff(at + Vector2.up * 0.6f, 0.8f + 0.15f * up);
                    if (lvl >= 2) DungeonFx.FloorEmbers(at - lift, WildLime, 1);
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
                    DungeonFx.Sparks(e.Body, off, WildLime, 10 + 5 * up);
                    GreenLicks(e.Body, 3 + up, 1f);
                    if (lvl >= 2 && !before) GreenBurn(e, mult * 0.15f, 3);
                }
            };

            _dc.Shake(0.1f + 0.04f * up, 0.08f + 0.03f * up);
            DungeonFx.Flash(Body, WildLime, radius * 1.6f, 0.6f + 0.15f * up, 0.3f);
            After(sweep * 0.92f, () => GameLook.Punch(0.3f + 0.15f * up));
            if (lvl >= 3)
            {
                float stormMult = mult * 0.6f;
                After(sweep * 0.95f, () => GreenFirestorm(radius, stormMult, hitAt, lift, squash));
            }
            return true;
        }

        /// Level 3's end: a ring of green fire and a storm of burning leaves burst outwards.
        void GreenFirestorm(float radius, float mult, Dictionary<EnemyController, float> already, Vector2 lift, float squash)
        {
            if (!Alive) return;
            var cut = BurstRing(transform, lift, radius, radius * 0.3f, 0.8f, 0.4f);   // Verdant's ring of green…
            CrescentGreen(cut, 3);
            var wave = BurstRing(transform, lift, radius * 0.8f, radius * 0.45f, 0.85f, 0.75f);   // …and the fire behind it
            wave.Look = SlashRing.GreenFireMaterial; wave.Rise = 1.1f; wave.HeatScale = 1.1f;
            _dc.Shake(0.28f, 0.2f); GameFeel.HitStop(0.06f); GameLook.Punch(0.55f);
            DungeonFx.Flash(Body, WildGold, radius * 3f, 1.6f, 0.45f);
            DungeonFx.Flash(Body, WildGreen, radius * 3.5f, 1f, 1.3f);
            for (int i = 0; i < 26; i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized;
                BurningLeaf(Body + new Vector2(o.x, o.y * squash) * radius * 0.6f, new Vector2(o.x, o.y * squash) * 2.2f);
            }
            for (int i = 0; i < 10; i++)
            {
                float d = i * 0.04f; bool smoke = i % 3 == 0;
                After(d, () =>
                {
                    if (!Alive) return;
                    GreenLicks(Feet + new Vector2(Random.Range(-0.35f, 0.35f), 0.2f), 3, 1.6f, 4f);
                    if (smoke) WildSmokePuff(Body + Vector2.up * Random.Range(1.4f, 2.2f), 1.4f);
                });
            }
            Vector2 c = Body;
            foreach (var e in Combat2D.EnemiesIn(c, radius * 1.75f + 0.5f))
            {
                if (e == null || e.Dead || already.ContainsKey(e)) continue;
                Vector2 off = e.Body - c;
                if (new Vector2(off.x, off.y / squash).magnitude > radius * 1.75f + 0.5f) continue;
                _curType = Element.Nature;
                Hit(e, mult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : Vector2.up, 10f, Element.Nature);
                if (!Alive) return;
                GreenBurn(e, mult * 0.25f, 3);
            }
        }

        /// Green-gold tongues of flame rising from a spot.
        static void GreenLicks(Vector2 at, int count, float size, float rise = 2.4f)
        {
            for (int i = 0; i < count; i++)
            {
                float hot = Random.value;
                Color c = hot < 0.3f ? WildGold : hot < 0.75f ? WildLime : WildGreen;
                float life = hot < 0.3f ? Random.Range(0.18f, 0.3f) : Random.Range(0.3f, 0.55f);
                var f = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)(at + Random.insideUnitCircle * 0.25f * size), c, life,
                                        Random.Range(0.4f, 0.7f) * size, 0.08f, 0f, 20430, true);
                f.velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.6f, 1f) * rise);
                f.accel = new Vector2(0f, rise * 1.2f); f.drag = 1.5f; f.sway = Random.Range(0.3f, 0.8f);
            }
        }

        /// A leaf flung off the swing, alight: it tumbles out with a little flame and embers trailing off it.
        void BurningLeaf(Vector2 at, Vector2 outward)
        {
            var leaf = DungeonFx.Spawn(DungeonFx.Leaf, (Vector3)at, Color.Lerp(WildLime, WildGold, Random.value * 0.6f), Random.Range(0.7f, 1.1f), 1.2f, 0.5f, Random.Range(0f, 360f), 20460, true);
            leaf.velocity = outward.normalized * Random.Range(1.5f, 3.5f) + Vector2.up * Random.Range(0.5f, 1.5f);
            leaf.drag = 2.5f; leaf.accel = new Vector2(0f, -1f); leaf.spin = Random.Range(-300f, 300f); leaf.sway = Random.Range(0.5f, 1f);
            var flame = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)at, WildLime, 0.5f, 0.45f, 0.1f, 0f, 20455, true);
            flame.velocity = leaf.velocity * 0.9f + Vector2.up * 0.8f; flame.drag = 2.5f;
            if (Random.value < 0.5f) DungeonFx.Embers(at, WildGold, 1, 0.15f);
        }

        /// Paler smoke than Inferno's: a wood-fire haze.
        static void WildSmokePuff(Vector2 at, float size)
        {
            var s = DungeonFx.Spawn(DungeonFx.Soft, (Vector3)(at + Random.insideUnitCircle * 0.2f), new Color(WildSmoke.r, WildSmoke.g, WildSmoke.b, Random.Range(0.3f, 0.45f)),
                                    Random.Range(1.3f, 2f), Random.Range(0.6f, 0.9f) * size, Random.Range(2f, 2.6f) * size, Random.Range(0f, 360f), 20390, false);
            s.velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.7f, 1.1f));
            s.drag = 0.6f; s.spin = Random.Range(-30f, 30f); s.sway = 0.25f;
        }

        void GreenBurn(EnemyController e, float mult, int ticks)
        {
            for (int i = 1; i <= ticks; i++)
                After(0.5f * i, () =>
                {
                    if (!Alive || e == null || e.Dead) return;
                    GreenLicks(e.Body, 3, 0.8f);
                    _curType = Element.Nature;
                    Hit(e, mult, Vector2.up, 0.3f, Element.Nature);
                });
        }
    }
}
