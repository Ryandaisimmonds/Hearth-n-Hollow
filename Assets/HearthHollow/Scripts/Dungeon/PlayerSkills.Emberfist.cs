// Hearth & Hollow — the player's skills (K / L / the ultimate): Emberfist. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UsePhoenix(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.4f);
            dir = Assist(m, dir); _aim = dir;
            float dist = m.range * (1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f));
            Vector2 to = WalkTo(Feet, dir, dist);
            float dur = 0.28f;
            _stepVel = (to - Feet) / dur; _stepT = dur;
            _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.1f);
            _phoenixMove = m; _phoenixT = dur; _phoenixMult = mult; _phoenixLvl = 0; _phoenixHit.Clear();
            _phoenixType = _curType; _sweepFrom = Body;
            return true;
        }

        bool UseFlameWheel(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.35f);
            float rad = m.radius * Reach(m, 0); var wm = m; float wmult = mult; var wt = _curType; Color col = MoveColour(m);
            for (int i = 0; i < Mathf.Max(1, m.count); i++)
                After(0.001f + i * 0.18f, () => { _curType = wt; _curMove = wm; DungeonFx.RingAt(Body, col, rad, 0.3f); DungeonFx.Throw(Body, Vector2.up, col, 8, 3f, 7f); MoveLight(wm, Body, 1.2f, 0.3f); HitArea(Body, rad, wmult, Vector2.zero, 7f); });
            return true;
        }

        bool UseDetonate(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.35f);
            const float h = 0.6f;   // Heat was cut in Design v2: a fixed-size blast (what 60 Heat used to give)
            float rad = (m.radius + h * m.range) * _dc.Settings.playerReach;
            float dmult = mult * (0.5f + h * m.value);
            Color col = MoveColour(m);
            DungeonFx.Flash(Body, col, rad * 3f, 2.6f, 0.8f); DungeonFx.RingAt(Body, col, rad, 0.5f); DungeonFx.RingAt(Body, Color.white, rad * 0.6f, 0.35f);
            DungeonFx.Embers(Body, col, 40, rad * 0.5f);
            for (int i = 0; i < 20; i++) DungeonFx.Throw(Body, UnityEngine.Random.insideUnitCircle.normalized, col, 1, 4f, 10f);
            _dc.Shake(0.3f, 0.2f); GameLook.Punch(0.8f); GameFeel.HitStop(0.08f); _dc.FloodLight(0.5f, 1.5f);
            DungeonHud.Number(Body + Vector2.up * 1f, "Detonate!", "dnum--crit");
            HitArea(Body, rad, dmult, Vector2.zero, 11f, 0.4f);
            return true;
        }

        bool UseCounter(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.2f);
            float dur = m.duration * (1f + (m.boostShape == BoostShape.Duration ? m.boostStep * 0 : 0f));
            _counterMove = m; _counterUntil = Time.time + dur; _counterMult = mult;
            DungeonFx.RingAt(Body, MoveColour(m), 1f, dur);
            DungeonFx.Flash(Body, MoveColour(m), 3f, 1.4f, dur, transform);
            return true;
        }
    }
}
