// Hearth & Hollow — the player's skills (K / L / the ultimate): Lamplighter. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UseLantern(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * 0) : 0);
            for (int i = 0; i < n; i++)
            {
                float ang = n == 1 ? 0f : (i - (n - 1) * 0.5f) * 50f;
                Vector2 at = Feet + (Vector2)(Quaternion.Euler(0, 0, ang) * dir) * 1.2f + new Vector2(0, 0.9f);
                HoverLantern.Place(_dc, at, m, mult, MoveColour(m), LightRadius(m));
            }
            return true;
        }

        bool UseSunbeam(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(m.duration);
            StartBeam(m, mult, 1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f), m.duration);
            return true;
        }

        bool UsePrism(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            dir = Assist(m, dir); _aim = dir;
            var p = Projectile.Spawn(_dc, true, Body + dir * 0.4f, dir, m.range, mult, 0f, DungeonFx.Orb, MoveColour(m), false, 1.3f);
            if (p == null) return true;
            var pm = m; float pmult = mult; int plvl = 0;
            p.SetHit(Element.Light); p.SetLife(AimLife(m.range, 1.1f));
            p.OnStop = at => PrismBurst(pm, at, pmult, plvl);
            return true;
        }

        bool UseKindle(MoveData m, Vector2 dir, float mult)
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
    }
}
