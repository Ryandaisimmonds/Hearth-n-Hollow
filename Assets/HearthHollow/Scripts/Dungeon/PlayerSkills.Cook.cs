// Hearth & Hollow — the player's skills (K / L / the ultimate): Cook. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UseSnack(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            int healed = S.DungeonHealPlayer(Mathf.RoundToInt(m.value * _mods.skillPower));
            if (healed > 0) DungeonHud.Number(Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
            Guard(T.guardDamageMult, m.duration);
            DungeonFx.RingAt(Body, new Color(0.6f, 1f, 0.6f, 0.8f), 1.2f);
            return true;
        }

        bool UsePepperToss(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            MoveLight(m, Body + dir * m.range * 0.6f, 1.4f, 0.5f);   // Flambé: a huge burst of firelight
            DungeonFx.Throw(Body + dir * 0.5f, dir, MoveColour(m), 10, 3f, 7f);
            for (int i = 0; i < 8; i++)
            {
                Vector2 d = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-m.radius, m.radius)) * dir;
                var fx = DungeonFx.Spawn(DungeonFx.Circle, Body + d * 0.4f, A((i & 1) == 0 ? MoveColour(m) : Accent, 0.8f), 0.4f, 0.35f, 0.15f);
                fx.velocity = d * UnityEngine.Random.Range(m.range * 1.5f, m.range * 2.6f);
            }
            foreach (var e in Combat2D.EnemiesInCone(Body, dir, m.range * S.MutatorAreaRadius, m.radius))
            {
                Hit(e, mult * S.MutatorAreaDamage, dir, 3f);
                if (!Alive) return true;
                if (e != null && !e.Dead) e.Slow(1f - m.value, m.duration);
            }
            return true;
        }

        bool UseHotPot(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            Vector2 at = Feet + dir * AimDistance(m.range);   // mouse: on the cursor
            var pot = DungeonFx.Spawn(DungeonFx.Circle, Body, new Color(0.55f, 0.4f, 0.3f, 1f), 0.35f, 0.7f, 0.7f);
            pot.velocity = (at - Body) * 6f;   // FxSprite damps velocity by ~6/s, so this lands near the target
            var potMove = m; float potMult = mult; var ptT = _curType; float potK = Reach(m, 0);   // Boost: a bigger pool
            After(0.35f, () =>
            {
                DungeonFx.Puff(at, A(MoveColour(potMove), 0.8f), 8, 1f);
                DungeonFx.Flash(at, MoveColour(potMove), potMove.radius * potK * 2.4f, 1.3f, potMove.duration);   // the pool glows like a fire
                Color potCol = MoveColour(potMove), potAcc = Accent;
                DungeonZone.Spawn(_dc, at, potMove.radius * potK, potMove.duration, potMove.value, A(potCol, 0.35f), z =>
                {
                    DungeonFx.Spawn(DungeonFx.Circle, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius, A(potAcc, 0.7f), 0.3f, 0.3f, 0.05f);
                    foreach (var e in z.Enemies()) { Hit(e, potMult, Vector2.zero, 0f, ptT); if (!Alive) return; }
                });
            });
            return true;
        }

        bool UsePicnic(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            int perTick = Mathf.Max(1, Mathf.RoundToInt(m.value * _mods.skillPower));
            DungeonZone.Spawn(_dc, Feet, m.radius, m.duration, 0.5f, new Color(1f, 0.55f, 0.6f, 0.3f), z =>
            {
                var pl = _dc.Player;
                if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                int healed = S.DungeonHealPlayer(perTick);
                if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
            });
            return true;
        }

        bool UseSkewerToss(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            dir = Assist(m, dir); _aim = dir;
            Fan(m, mult, dir, m.range, false, 1.3f, 0);
            return true;
        }

        bool UseCookfire(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            var room = _dc.CurrentRoom;
            Vector2 at = Feet + dir * 0.9f;
            if (room != null) at = room.NearestWalkable(at);
            float k = 1f + (m.boostShape == BoostShape.Radius ? m.boostStep * 0 : 0f);
            float rad = m.radius * k;
            int perTick = Mathf.Max(1, Mathf.RoundToInt(m.value * _mods.skillPower * k));
            Color fire = MoveColour(m);
            var l = DungeonFx.Flash(at + new Vector2(0, 0.4f), fire, rad * 2.4f, 1.6f, m.duration);   // a real light source: light-shy monsters keep away
            LightSetup.CastShadows(l, 0.7f, 0.45f);
            DungeonFx.Spawn(DungeonFx.Flame, at + new Vector2(0, 0.3f), fire, m.duration, 1.3f * k, 1.0f * k, 0f, DungeonController.SortOrder(at.y) + 2, true);
            DungeonFx.Embers(at, fire, 14, 0.4f);
            if (room != null && room.Lights != null) room.Lights.IgniteNear(at, rad);
            DungeonZone.Spawn(_dc, at, rad, m.duration, 0.5f, new Color(1f, 0.55f, 0.25f, 0.18f), z =>
            {
                DungeonFx.Embers(z.Centre, fire, 1, 0.25f);
                var pl = _dc.Player;
                if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                int healed = S.DungeonHealPlayer(perTick);
                if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
            });
            return true;
        }

        bool UseRollingPin(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.3f);
            var p = Projectile.Spawn(_dc, true, Hip + dir * 0.4f, dir, m.value, mult, 0f, DungeonFx.Arrow, Color.Lerp(new Color(0.85f, 0.65f, 0.45f), MoveColour(m), 0.5f), true, 2.2f);
            if (p != null) DungeonFx.Flash(p.transform.position, MoveColour(m), LightRadius(m) * 0.6f, 1.0f, 0f, p.transform);   // the pin glows as it flies
            if (p != null) { p.Boomerang(m.range, 900f); p.SetHit(_curType); }
            return true;
        }
    }
}
