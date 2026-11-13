// Hearth & Hollow — the player's skills (K / L / the ultimate): Ultimates. Registered in PlayerSkills.cs.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        bool UseSunfall(MoveData m, Vector2 dir, float mult)
        {
            Vector2 to = DungeonInput.MouseAim ? WalkTo(Feet, dir, AimDistance(m.range)) : LeapTarget(m, dir);
            float dur = 0.45f;
            _anim.PlayAttack(dur + 0.2f);
            _stepVel = (to - Feet) / dur; _stepT = dur;
            _hopT = _hopDur = dur;
            _invulnUntil = Mathf.Max(_invulnUntil, Time.time + dur + 0.3f);
            var sfm = m; float sfMult = mult; var sfT = _curType; Vector2 sfTo = to;
            DungeonFx.Flash(Body, MoveColour(m), 4f, 2f, dur, transform);
            After(dur, () =>
            {
                if (Vector2.Distance(Feet, sfTo) > 0.1f) Teleport(sfTo);
                Slam(sfm, sfMult, 1, sfT);
                _furyUntil = Time.time + 6f; _furyBonus = sfm.value;
                _dc.FloodLight(0.8f, 2.5f); GameLook.Punch(0.8f);
            });
            return true;
        }

        bool UseStarfall(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.4f);
            Vector2 at = Feet + dir * AimDistance(m.range);
            var sroom = _dc.CurrentRoom; if (sroom != null) at = sroom.NearestWalkable(at);
            float tickMult = mult; var stT = _curType; var stm = m; Color star = new Color(1f, 0.95f, 0.65f);
            MoveLight(m, at, 1.4f, m.duration + 0.5f); _dc.FloodLight(0.5f, m.duration);
            DungeonZone.Spawn(_dc, at, m.radius * Reach(m, 0), m.duration, 0.25f, A(MoveColour(m), 0.25f), z =>
            {
                for (int i = 0; i < 4; i++)
                    DungeonFx.Spawn(DungeonFx.Arrow, z.Centre + UnityEngine.Random.insideUnitCircle * z.Radius + Vector2.up * 0.6f, star, 0.18f, 1.4f, 1.0f, -90f);
                foreach (var e in z.Enemies())
                {
                    if (e == null || e.Dead) continue;
                    e.Mark(stm.value * _mods.skillPower, 1.5f);
                    Hit(e, tickMult, Vector2.zero, 0f, stT);
                    if (!Alive) return;
                }
            });
            return true;
        }

        bool UseGrandFeast(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.45f);
            Color fire = MoveColour(m);
            float frad = m.radius * Reach(m, 0);
            DungeonFx.RingAt(Body, fire, frad, 0.5f); DungeonFx.RingAt(Body, Accent, frad * 0.6f, 0.35f);
            DungeonFx.Embers(Body, fire, 40, frad * 0.5f);
            _dc.Shake(0.25f, 0.18f); GameLook.Punch(0.6f); _dc.FloodLight(0.6f, 2f);
            HitArea(Body, frad, mult, Vector2.zero, 10f);
            if (!Alive) return true;
            foreach (var e in Combat2D.EnemiesIn(Feet, frad)) if (e != null && !e.Dead) e.Slow(0.5f, 4f);
            int fh = S.DungeonHealPlayer(Mathf.RoundToInt(m.value * _mods.skillPower));
            if (fh > 0) DungeonHud.Number(Body + Vector2.up * 0.8f, "+" + fh, "dnum--heal");
            // and a great cookfire where you stand
            var froom = _dc.CurrentRoom; Vector2 fat = froom != null ? froom.NearestWalkable(Feet) : Feet;
            float crad = 2.8f; float life = m.duration; int perTick = Mathf.Max(1, Mathf.RoundToInt(3f * _mods.skillPower));
            var fl = DungeonFx.Flash(fat + new Vector2(0, 0.4f), fire, crad * 2.4f, 1.8f, life);
            LightSetup.CastShadows(fl, 0.7f, 0.45f);
            DungeonFx.Spawn(DungeonFx.Flame, fat + new Vector2(0, 0.3f), fire, life, 1.7f, 1.3f, 0f, DungeonController.SortOrder(fat.y) + 2, true);
            if (froom != null && froom.Lights != null) froom.Lights.IgniteNear(fat, crad);
            DungeonZone.Spawn(_dc, fat, crad, life, 0.5f, A(fire, 0.2f), z =>
            {
                DungeonFx.Embers(z.Centre, fire, 1, 0.3f);
                var pl = _dc.Player;
                if (pl == null || Vector2.Distance(pl.Feet, z.Centre) > z.Radius) return;
                int healed = S.DungeonHealPlayer(perTick);
                if (healed > 0) DungeonHud.Number(pl.Body + Vector2.up * 0.6f, "+" + healed, "dnum--heal");
            });
            return true;
        }

        bool UseDawn(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.5f);
            Color col = MoveColour(m);
            var droom = _dc.CurrentRoom;
            if (droom != null && droom.Lights != null) droom.Lights.IgniteWave(Feet);
            _dc.FloodLight(1f, 3f);
            DungeonFx.Flash(Body, col, 14f, 3f, 1.2f);
            DungeonFx.RingAt(Body, col, 4f, 0.6f); DungeonFx.RingAt(Body, Accent, 7f, 0.8f);
            DungeonFx.Embers(Body, col, 40, 2f);
            _dc.Shake(0.2f, 0.15f); GameLook.Punch(0.6f);
            _curType = Element.Light;
            HitArea(Body, m.radius, mult, Vector2.zero, 4f, m.duration);
            return true;
        }

        bool UseSupernova(MoveData m, Vector2 dir, float mult)
        {
            _anim.PlayAttack(0.5f);
            Color col = MoveColour(m);
            DungeonFx.Flash(Body, col, 6f, 2f, 0.4f, transform); DungeonFx.Embers(Body, col, 30, 0.6f);
            var snm = m; float snMult = mult; var snT = _curType;
            _invulnUntil = Mathf.Max(_invulnUntil, Time.time + 0.45f);
            After(0.35f, () =>
            {
                _curType = snT; _curMove = snm;
                float rad = snm.radius * _dc.Settings.playerReach;
                DungeonFx.Flash(Body, col, rad * 3f, 3f, 0.9f); DungeonFx.RingAt(Body, col, rad, 0.6f); DungeonFx.RingAt(Body, Accent, rad * 0.6f, 0.4f);
                DungeonFx.Embers(Body, col, 50, rad * 0.5f);
                for (int i = 0; i < 24; i++) DungeonFx.Throw(Body, UnityEngine.Random.insideUnitCircle.normalized, col, 1, 4f, 12f);
                _dc.Shake(0.35f, 0.25f); GameLook.Punch(0.9f); GameFeel.HitStop(0.1f); _dc.FloodLight(0.7f, 2f);
                HitArea(Body, rad, snMult, Vector2.zero, 12f, snm.duration);
            });
            return true;
        }
    }
}
