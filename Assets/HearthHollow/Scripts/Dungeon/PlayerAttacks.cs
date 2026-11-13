// Hearth & Hollow — the player's attack (left click / J), one handler per MoveKind; anything else is a plain swing.
// StartAttack sets the swing's timing; AttackHit does what every attack shares, then looks the move up here.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        delegate void AttackUse(PlayerController p, MoveData m, Vector2 dir, float mult, int boost);

        static readonly Dictionary<MoveKind, AttackUse> Attacks = new Dictionary<MoveKind, AttackUse>
        {
            { MoveKind.Wisp, (p, m, d, x, b) => p.AttackWisp(m, d, x, b) },
            { MoveKind.Combo, (p, m, d, x, b) => p.AttackCombo(m, d, x, b) },
            { MoveKind.Arrow, (p, m, d, x, b) => p.AttackArrow(m, d, x, b) },
            { MoveKind.SpreadArrow, (p, m, d, x, b) => p.AttackSpreadArrow(m, d, x, b) },
            { MoveKind.PierceArrow, (p, m, d, x, b) => p.AttackPierceArrow(m, d, x, b) },
            { MoveKind.Lunge, (p, m, d, x, b) => p.AttackLunge(m, d, x, b) },
            { MoveKind.Heavy, (p, m, d, x, b) => p.AttackHeavy(m, d, x, b) },
        };

        void AttackWisp(MoveData m, Vector2 dir, float mult, int blv)
        {
            Motes(m, mult, dir, blv);
        }

        void AttackCombo(MoveData m, Vector2 dir, float mult, int blv)
        {
            bool fin = _comboStep >= Mathf.Max(1, m.count) - 1;
            int extra = m.boostShape == BoostShape.Hits ? Mathf.RoundToInt(m.boostStep * blv) : 0;
            ComboHit(m, fin, dir, mult, blv);
            for (int h = 1; h <= extra; h++) { Vector2 d2 = dir; float mm = mult; int lv = blv; After(0.12f * h, () => ComboHit(m, true, d2, mm, lv)); }   // Boost: extra finishers
        }

        void AttackArrow(MoveData m, Vector2 dir, float mult, int blv)
        {
            Fan(m, mult, dir, m.range, false, 1.4f, blv);
        }

        void AttackSpreadArrow(MoveData m, Vector2 dir, float mult, int blv)
        {
            Fan(m, mult, dir, m.range, false, 1.2f, blv);
        }

        void AttackPierceArrow(MoveData m, Vector2 dir, float mult, int blv)
        {
            Fan(m, mult, dir, m.range, true, 1.9f, blv);
            DungeonFx.Puff(Hip + dir * 0.4f, A(MoveColour(m), 0.5f), 4, 0.35f);
        }

        void AttackLunge(MoveData m, Vector2 dir, float mult, int blv)
        {
            _stepVel = dir * (m.range / 0.12f) * 0.5f; _stepT = 0.12f;   // a short step forward
            DungeonFx.SlashAt(Body + dir * 0.4f, dir, A(MoveColour(m), 0.95f), 1.7f);
            DungeonFx.RingAt(Body + dir * m.range, A(Accent, 0.8f), m.radius * 1.2f, 0.25f);
            IgniteNear(Body + dir * m.range, m.radius + 0.4f);
            // a thrust: three small circles along the line
            var done = new HashSet<EnemyController>();
            for (int i = 1; i <= 3; i++)
                foreach (var e in Combat2D.EnemiesIn(Body + dir * (m.range * i / 3f), m.radius * Reach(m, blv)))
                    if (done.Add(e)) { Hit(e, mult, dir); if (!Alive) return; }
        }

        void AttackHeavy(MoveData m, Vector2 dir, float mult, int blv)
        {
            DungeonFx.SlashAt(Body + dir * 0.3f, dir, A(MoveColour(m), 1f), 2.1f);
            DungeonFx.RingAt(Body + dir * m.range, A(MoveColour(m), 0.85f), m.radius, 0.3f);
            DungeonFx.RingAt(Body + dir * m.range, A(Accent, 0.7f), m.radius * 0.6f, 0.4f);
            DungeonFx.Flash(Body + dir * m.range, MoveColour(m), LightRadius(m) * 1.4f, 1.3f, 0.4f);
            _dc.Shake(0.12f, 0.12f);
            HitArea(Body + dir * m.range, m.radius * Reach(m, blv), mult, dir, 10f, m.duration);
        }

        void AttackSwing(MoveData m, Vector2 dir, float mult, int blv)
        {
            // Swing
            DungeonFx.SlashAt(Body + dir * 0.25f, dir, A(MoveColour(m), 0.95f), m.range < 0.8f ? 1.25f : 1.5f);   // in the class's colour
            HitArea(Body + dir * m.range, m.radius * Reach(m, blv), mult, dir);
        }
    }
}
