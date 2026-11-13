// Hearth & Hollow — the player's skills (K / L / the ultimate): Ranger. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UseVolley(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            Arrow(mult, Quaternion.Euler(0, 0, -7f) * dir, 13f, true);
            After(0.09f, () => Arrow(mult, Quaternion.Euler(0, 0, 7f) * Aim, 13f, true));
            return true;
        }

        bool UseTrap(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.25f);
            var trapMove = m; float trapMult = mult; var trT = _curType;
            DungeonZone.Spawn(_dc, Feet, m.radius, m.value, 0f, A(MoveColour(m), 0.45f), z =>
            {
                var hit = z.Enemies();
                if (hit.Count == 0) return;
                var e = hit[0];
                z.End();
                DungeonFx.RingAt(z.Centre, A(MoveColour(trapMove), 0.9f), trapMove.radius * 1.3f, 0.35f);
                Hit(e, trapMult, Vector2.zero, 0f, trT);
                if (e != null && !e.Dead) e.Root(trapMove.duration);
            });
            return true;
        }

        bool UseMark(MoveData m, Vector2 dir, float mult)
        {
            var target = Combat2D.Nearest(Feet, m.range);
            if (target == null) { DungeonHud.Number(Body + Vector2.up * 0.8f, "No target", "dnum--info"); return false; }
            _anim.PlayAttack(0.25f);
            target.Mark(m.value * _mods.skillPower, m.duration);
            return true;
        }

        bool UseArrowRain(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            Vector2 at = Feet + dir * AimDistance(m.range);   // mouse: on the cursor
            float tickMult = mult; var rnT = _curType;
            MoveLight(m, at, 1f, m.duration + 0.3f);   // the storm lights the whole area
            Color rainCol = MoveColour(m);
            DungeonZone.Spawn(_dc, at, m.radius * Reach(m, 0), m.duration, 0.25f, A(rainCol, 0.22f), z =>
            {
                for (int i = 0; i < 3; i++)
                    DungeonFx.Spawn(DungeonFx.Arrow, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius + Vector2.up * 0.6f, Color.Lerp(Color.white, rainCol, 0.9f), 0.18f, 1.2f, 1.0f, -90f);
                foreach (var e in z.Enemies()) { Hit(e, tickMult, Vector2.zero, 0f, rnT); if (!Alive) return; }
            });
            return true;
        }

        bool UseSmoke(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.2f);
            DungeonFx.Puff(Body, A(Color.Lerp(new Color(0.7f, 0.7f, 0.72f), MoveColour(m), 0.45f), 0.7f), 16, m.radius * 0.55f);
            DungeonFx.Flash(Body, MoveColour(m), m.radius * 1.7f, 1.0f, 0.6f);
            foreach (var e in Combat2D.EnemiesIn(Feet, m.radius)) e.Slow(1f - m.value, m.duration);
            _hasteUntil = Time.time + m.duration;
            return true;
        }

        bool UseEmberArrow(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            int n = Mathf.Max(1, m.count) + (m.boostShape == BoostShape.Count ? Mathf.RoundToInt(m.boostStep * 0) : 0);
            float sp = m.spread > 0f ? m.spread : 10f;
            var em = m; float burn = SkillMult(m) * m.value;
            for (int i = 0; i < n; i++)
            {
                var p = Arrow(mult, Quaternion.Euler(0, 0, (i - (n - 1) * 0.5f) * sp) * dir, m.range, false, 1.5f);
                if (p == null) continue;
                p.SetLife(AimLife(m.range, 0.7f));   // mouse: lands where you clicked
                p.OnStop = at => EmberLand(em, at, burn);
            }
            return true;
        }

        bool UseFlareArrow(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            var p = Arrow(mult, dir, m.range, false, 1.5f);
            if (p == null) return true;
            var fm = m; float fmult = mult; int flvl = 0;
            p.Tint(Color.Lerp(Color.white, MoveColour(m), 0.9f)).SetLife(AimLife(m.range, 0.6f));   // mouse: bursts where you clicked
            p.OnStop = at => FlareBurst(fm, at, fmult, flvl);
            return true;
        }

        bool UseSpotlight(MoveData m, Vector2 dir, float mult)
        {
            var target = Combat2D.Nearest(Feet, m.range);
            if (target == null) { DungeonHud.Number(Body + Vector2.up * 0.8f, "No target", "dnum--info"); return false; }
            _anim.PlayAttack(0.25f);
            DungeonFx.Flash(target.Body, MoveColour(m), 2.6f, 2.2f, m.duration, target.transform);   // it's lit: a weak spot
            target.Mark(m.value * _mods.skillPower, m.duration);
            DungeonHud.Number(target.Body + Vector2.up * 0.9f, "Spotted!", "dnum--info");
            return true;
        }
    }
}
