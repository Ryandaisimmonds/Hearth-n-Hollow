// Hearth & Hollow — the player's skills (K / L / the ultimate): new ones being made in the Effects Lab. Registered in
// PlayerSkills.cs. Their MoveData is added by the lab (EffectsLab.LabMoves) until they go into the content.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        /// Verdant Crescent: one huge slash all the way round you. The blade burns white where it is, the trail cools
        /// to light green and spreads as it hangs; everything the blade passes over is hit once, flung outwards.
        bool UseVerdantCrescent(MoveData m, Vector2 dir, float mult)
        {
            float sweep = m.duration > 0f ? m.duration : 0.5f;
            float radius = m.radius * Reach(m, 0);
            int spin = dir.x >= 0f ? -1 : 1;   // facing right it comes over the top and down, like a forehand
            float aim = Mathf.Atan2(dir.y, dir.x);
            float start = aim - spin * 0.35f * Mathf.PI;   // starts a little behind your aim, so the bright edge crosses it early

            _anim.PlayAttack(sweep);
            Vector2 lift = Body - Feet;
            var ring = SlashRing.Play(transform, lift, start, spin);
            ring.Radius = radius; ring.Width = radius * 0.62f; ring.SweepTime = sweep;
            ring.Turns = m.value > 0f ? m.value : 1.15f;

            var hit = new HashSet<EnemyController>(); var type = _curType; float squash = ring.Squash;
            ring.Swept = (a0, a1) =>
            {
                if (!Alive) return;
                Vector2 c = ring.Centre;
                foreach (var e in Combat2D.EnemiesIn(c, radius + 0.8f))
                {
                    if (e == null || e.Dead || hit.Contains(e)) continue;
                    Vector2 off = e.Body - c;
                    Vector2 flat = new Vector2(off.x, off.y / squash);   // where it stands on the ring's ellipse
                    if (flat.magnitude > radius + 0.6f) continue;
                    float ea = Mathf.Atan2(flat.y, flat.x);
                    float ahead = Mathf.Repeat(spin * (ea - a0), Mathf.PI * 2f);   // how far ahead of the blade it was
                    if (ahead > Mathf.Abs(a1 - a0) && flat.magnitude > 0.5f) continue;   // not reached yet (anyone right on top of you is hit at once)
                    hit.Add(e);
                    _curType = type;
                    Hit(e, mult * S.MutatorAreaDamage, off.sqrMagnitude > 0.001f ? off.normalized : dir, 8f, type);
                    if (!Alive) return;
                    DungeonFx.Sparks(e.Body, off, new Color(0.85f, 1f, 0.7f), 10);
                }
            };

            _dc.Shake(0.1f, 0.08f);
            DungeonFx.RingAt(Body, new Color(0.9f, 1f, 0.8f, 0.8f), radius * 0.35f, 0.25f);
            After(sweep * 0.92f, () =>   // the blade drags to a stop: a last pulse of green
            {
                DungeonFx.RingAt(Body, new Color(0.55f, 1f, 0.45f, 0.75f), radius * 1.1f, 0.4f);
                GameLook.Punch(0.3f);
            });
            return true;
        }
    }
}
