// Hearth & Hollow — the player's skills (K / L / the ultimate): Warden. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UseCleave(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.35f);
            DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), m.radius); DungeonFx.RingAt(Body, A(Accent, 0.8f), m.radius * 0.6f, 0.3f);
            _dc.Shake(0.15f, 0.12f);
            HitArea(Body, m.radius * Reach(m, 0), mult, Vector2.zero);
            return true;
        }

        bool UseShockwave(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.4f);
            _dc.Shake(0.2f, 0.15f);
            var origin = Body; var hitOnce = new HashSet<EnemyController>(); var swT = _curType;
            int steps = Mathf.Max(1, Mathf.RoundToInt(m.range));
            for (int i = 1; i <= steps; i++)
            {
                Vector2 at = origin + dir * i;
                After(0.06f * i, () =>
                {
                    DungeonFx.RingAt(at, A(MoveColour(m), 0.85f), m.radius, 0.3f);
                    DungeonFx.Puff(at, new Color(0.7f, 0.6f, 0.45f, 0.6f), 3, 0.4f);
                    foreach (var e in Combat2D.EnemiesIn(at, m.radius * S.MutatorAreaRadius))
                    {
                        if (!hitOnce.Add(e)) continue;
                        Hit(e, mult * S.MutatorAreaDamage, dir, 7f, swT);
                        if (!Alive) return;
                        if (e != null && !e.Dead) e.Stun(m.duration);
                    }
                });
            }
            return true;
        }

        bool UseWarCry(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            _furyUntil = Time.time + m.duration; _furyBonus = m.value;
            DungeonFx.Flash(Body, MoveColour(m), LightRadius(m), 1.8f, m.duration, transform);   // Rally: your light flares up
            DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), 2.4f, 0.5f); DungeonFx.Embers(Body, Accent, 16, 0.8f);
            DungeonHud.Number(Body + Vector2.up * 0.8f, "+" + Mathf.RoundToInt(m.value * 100f) + "% damage", "dnum--info");
            return true;
        }

        bool UseShieldWall(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.25f);
            Guard(m.value, m.duration);
            DungeonFx.RingAt(Body, A(MoveColour(m), 0.9f), 1.3f, 0.5f);
            return true;
        }

        bool UseWhirlwind(MoveData m, Vector2 dir, float mult)
        {
            _whirlMove = m; _whirlUntil = Time.time + m.duration; _whirlTick = 0f;
            return true;
        }

        bool UseSunshield(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            float dur = m.duration * (1f + (m.boostShape == BoostShape.Duration ? m.boostStep * 0 : 0f));
            Guard(m.value, dur);
            _sunMove = m; _sunUntil = Time.time + dur; _sunLvl = 0; _sunHit.Clear();
            if (_sunLight != null) Destroy(_sunLight.gameObject);
            _sunLight = DungeonFx.Flash(Body + Aim * 2f, MoveColour(m), LightRadius(m) * (1f + 0.2f * 0), 2.2f, 0f, transform);
            LightSetup.CastShadows(_sunLight, 0.7f, 0.4f);
            DungeonFx.RingAt(Body, MoveColour(m), 1.4f, 0.4f);
            return true;
        }

        bool UseLeapSlam(MoveData m, Vector2 dir, float mult)
        {
            Vector2 to = DungeonInput.MouseAim ? WalkTo(Feet, dir, AimDistance(m.range)) : LeapTarget(m, dir);   // mouse: land on the cursor
            float dur = 0.36f;
            _anim.PlayAttack(dur + 0.15f);
            _stepVel = (to - Feet) / dur; _stepT = dur;
            _hopT = _hopDur = dur;
            _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.1f);
            var slam = m; float slamMult = mult; int slamLvl = 0; var slamT = _curType;
            DungeonFx.Puff(Feet, new Color(0.8f, 0.74f, 0.62f, 0.55f), 6, 0.5f);
            Vector2 slamTo = to;
            After(dur, () => { if (Vector2.Distance(Feet, slamTo) > 0.1f) Teleport(slamTo); Slam(slam, slamMult, slamLvl, slamT); });   // land exactly where it aimed
            return true;
        }
    }
}
