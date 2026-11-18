// Hearth & Hollow — the player's skills (K / L / the ultimate): new ones being made in the Effects Lab. Registered in
// PlayerSkills.cs. Their MoveData is added by the lab (EffectsLab.LabMoves) until they go into the content.
// Each has three levels in a run (GameSession.MoveLevel): level 1 is the base, and each level up adds to it.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        /// Verdant Crescent: one huge slash all the way round you. The blade burns white where it is, the trail cools
        /// to light green and spreads as it hangs; everything the blade passes over is hit, flung outwards.
        ///   Level 1  the swing.
        ///   Level 2  further reach, harder hits, a richer green, more sparks and leaves; an echo swing just inside it
        ///            and two thin wind lines just outside.
        ///   Level 3  further again and fiercer green; the swing goes round twice as far (hitting a second time), four
        ///            wind lines, and as it ends a ring of green bursts outwards, hitting what the blade didn't reach.
        bool UseVerdantCrescent(MoveData m, Vector2 dir, float mult)
        {
            int lvl = Mathf.Clamp(S.MoveLevel(m.id), 1, GameSession.MaxMoveLevel);
            int up = lvl - 1;
            float sweep = (m.duration > 0f ? m.duration : 0.5f) * (1f + 0.15f * up);   // a bigger swing takes a touch longer
            float radius = m.radius * Reach(m, 0) * (1f + 0.25f * up);                 // 100% → 125% → 150% reach
            mult *= 1f + 0.35f * up;                                                  // ×1 → ×1.35 → ×1.7 damage
            float turns = (m.value > 0f ? m.value : 1.15f) + (lvl >= 3 ? 0.6f : 0.15f * up);
            int spin = dir.x >= 0f ? -1 : 1;   // facing right it comes over the top and down, like a forehand
            float aim = Mathf.Atan2(dir.y, dir.x);
            float start = aim - spin * 0.35f * Mathf.PI;   // starts a little behind your aim, so the bright edge crosses it early
            Vector2 lift = Body - Feet;

            _anim.PlayAttack(sweep);

            // the swing itself
            var ring = SlashRing.Play(transform, lift, start, spin);
            ring.Radius = radius; ring.SweepTime = sweep; ring.Turns = turns;
            ring.Width = radius * (0.62f + 0.06f * up);
            CrescentGreen(ring, lvl);
            ring.BladeThickness = 0.36f + 0.08f * up; ring.BladeReach = 0.18f + 0.15f * up; ring.BladeFrom = 0.3f - 0.05f * up;
            ring.LightIntensity = 0.55f + 0.15f * up;
            ring.Sparks = 1 + up; ring.LeafChance = 0.35f + 0.25f * up;
            ring.Grow = 0.45f + 0.15f * up;

            // level 2+: an echo swing just inside, a beat behind
            if (lvl >= 2)
            {
                var echo = SlashRing.Play(transform, lift, start, spin);
                echo.Delay = 0.05f; echo.ShowBlade = false;
                echo.Radius = radius * 0.62f; echo.Width = radius * 0.3f; echo.SweepTime = sweep; echo.Turns = turns * 0.92f;
                echo.TrailLife = 0.45f; echo.Sparks = 0; echo.LeafChance = 0f;
                CrescentGreen(echo, lvl); echo.HeatScale *= 0.8f;
            }
            // level 2+: thin wind lines riding just outside the swing (two, then four)
            int lines = lvl >= 3 ? 4 : lvl >= 2 ? 2 : 0;
            for (int i = 0; i < lines; i++)
            {
                var w = SlashRing.Play(transform, lift, start + spin * (0.25f + 0.12f * i), spin);
                w.Delay = 0.03f + 0.025f * i; w.ShowBlade = false;
                w.Radius = radius * (1.1f + 0.075f * i); w.Width = radius * 0.05f;
                w.SweepTime = sweep * 0.95f; w.Turns = turns * (0.8f - 0.08f * i);
                w.TrailLife = 0.32f; w.Drift = 0.35f; w.Grow = 0.2f; w.StartFadeDeg = 70f;
                w.Sparks = 0; w.LeafChance = 0f;
                CrescentGreen(w, lvl); w.HeatScale *= 1.1f;
            }

            // hits: the blade hits what it passes (once; at level 3, again on the second time round)
            var hitAt = new Dictionary<EnemyController, float>(); var type = _curType; float squash = ring.Squash;
            bool twice = lvl >= 3;
            ring.Swept = (a0, a1) =>
            {
                if (!Alive) return;
                Vector2 c = ring.Centre;
                float swept = Mathf.Abs(a1 - start);
                foreach (var e in Combat2D.EnemiesIn(c, radius + 0.8f))
                {
                    if (e == null || e.Dead) continue;
                    float last;
                    bool before = hitAt.TryGetValue(e, out last);
                    if (before && (!twice || swept - last < Mathf.PI * 1.8f)) continue;
                    Vector2 off = e.Body - c;
                    Vector2 flat = new Vector2(off.x, off.y / squash);   // where it stands on the ring's ellipse
                    if (flat.magnitude > radius + 0.6f) continue;
                    float ea = Mathf.Atan2(flat.y, flat.x);
                    float ahead = Mathf.Repeat(spin * (ea - a0), Mathf.PI * 2f);   // how far ahead of the blade it was
                    if (ahead > Mathf.Abs(a1 - a0) && flat.magnitude > 0.5f) continue;   // not reached yet (anyone right on top of you is hit at once)
                    hitAt[e] = swept;
                    _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : dir, 8f + 2f * up, type);
                    if (!Alive) return;
                    DungeonFx.Sparks(e.Body, off, new Color(0.85f, 1f, 0.7f), 10 + 5 * up);
                }
            };

            _dc.Shake(0.1f + 0.04f * up, 0.08f + 0.03f * up);
            DungeonFx.RingAt(Body, new Color(0.9f, 1f, 0.8f, 0.8f), radius * 0.35f, 0.25f);
            After(sweep * 0.92f, () => GameLook.Punch(0.3f + 0.15f * up));   // the blade drags to a stop

            // level 3: as the swing ends, a ring of green bursts outwards and hits what the blade didn't reach
            if (lvl >= 3)
            {
                float burstMult = mult * 0.6f;
                After(sweep * 0.95f, () =>
                {
                    var wave = SlashRing.Play(transform, lift, aim, spin);
                    wave.ShowBlade = false; wave.Radius = radius; wave.Width = radius * 0.35f;
                    wave.SweepTime = 0.03f; wave.Turns = 1f; wave.StartFadeDeg = 0f;
                    wave.TrailLife = 0.5f; wave.Drift = 0.75f; wave.Grow = 0.9f; wave.Sparks = 0; wave.LeafChance = 0f;
                    CrescentGreen(wave, lvl);
                    _dc.Shake(0.25f, 0.18f);
                    GameFeel.HitStop(0.06f);
                    DungeonFx.Flash(Body, new Color(0.45f, 1f, 0.35f), radius * 2f, 0.9f, 0.4f);
                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 o = Random.insideUnitCircle.normalized;
                        var leaf = DungeonFx.Spawn(DungeonFx.Leaf, (Vector3)(Body + new Vector2(o.x, o.y * squash) * radius), Color.Lerp(new Color(0.4f, 1f, 0.25f), new Color(0.1f, 0.75f, 0.2f), Random.value),
                                                   Random.Range(0.7f, 1.1f), 1.2f, 0.9f, Random.Range(0f, 360f), 20460, false);
                        leaf.velocity = new Vector2(o.x, o.y * squash) * Random.Range(3f, 6f); leaf.drag = 3f;
                        leaf.spin = Random.Range(-400f, 400f); leaf.sway = Random.Range(0.6f, 1f);
                    }
                    Vector2 c = Body;
                    foreach (var e in Combat2D.EnemiesIn(c, radius * 1.75f + 0.5f))
                    {
                        if (e == null || e.Dead || hitAt.ContainsKey(e)) continue;   // the blade already had these
                        Vector2 off = e.Body - c;
                        if (new Vector2(off.x, off.y / squash).magnitude > radius * 1.75f + 0.5f) continue;
                        _curType = type;
                        Hit(e, burstMult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : dir, 10f, type);
                        if (!Alive) return;
                    }
                });
            }
            return true;
        }

        /// Verdant Crescent's greens: each level a deeper, stronger green that burns brighter.
        static void CrescentGreen(SlashRing r, int lvl)
        {
            switch (lvl)
            {
                case 1: break;   // SlashRing's own: white → very light green → light green → green
                case 2:
                    r.PaleColour = new Color(0.66f, 1f, 0.38f); r.LightColour = new Color(0.3f, 1f, 0.12f); r.DeepColour = new Color(0.04f, 0.74f, 0.1f);
                    r.HeatScale = 1.15f; r.HeadHeat = 3.6f;
                    break;
                default:
                    r.PaleColour = new Color(0.55f, 1f, 0.28f); r.LightColour = new Color(0.2f, 1f, 0.06f); r.DeepColour = new Color(0.0f, 0.82f, 0.12f);
                    r.HeatScale = 1.35f; r.HeadHeat = 4.2f;
                    break;
            }
        }
    }
}
