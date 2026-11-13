// Hearth & Hollow — the player's dash (Space): blink, charge, vault, flicker and slick dash.
using System;
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public partial class PlayerController
    {
        // ───────────────────────── dash (Space) ─────────────────────────
        float _noDashHintAt;   // opening arc: the dash comes with the first boots

        void StartDash()
        {
            var m = _dash;
            _dashDir = _move.sqrMagnitude > 0.01f ? _move.normalized : Aim;
            _dashT = _dc.Settings.dashSeconds;
            _dashSpeedMult = 1f;
            _dashCd = DashCooldown;
            _attackT = -1f;   // dash cancels a swing
            _dashHit.Clear();
            _sweepFrom = Body; _dashTail = true;
            _trailT = 0f;
            DungeonFx.Puff(transform.position + new Vector3(0, 0.1f), new Color(0.8f, 0.74f, 0.62f, 0.55f), 6, 0.5f);   // dust kicked up
            _ghostT = 0f;
            if (m == null) return;
            MoveLight(m, Feet + new Vector2(0, 0.3f), 0.6f, 0.35f);
            switch (m.kind)
            {
                case MoveKind.Blink:
                {
                    Vector2 from = Feet;
                    Vector2 to = WalkTo(from, _dashDir, m.range);
                    DungeonFx.Flash(from + new Vector2(0, 0.4f), MoveColour(m), 3f, 1.4f, 3f);   // the light left behind
                    DungeonFx.Puff(Body, A(MoveColour(m), 0.7f), 10, 0.6f);
                    Teleport(to);
                    _dashT = 0.12f; _dashSpeedMult = 0f;
                    DungeonFx.RingAt(Body, MoveColour(m), 0.9f, 0.3f); DungeonFx.Embers(Feet, MoveColour(m), 10, 0.3f);
                    break;
                }
                case MoveKind.ChargeDash:
                    _dashT *= 1.25f; _dashSpeedMult = 0.95f;
                    break;
                case MoveKind.Vault:
                    _dashDir = -Aim;   // flip backwards, keep facing forward
                    _curType = ElementOf(m); _curMove = m;
                    Fan(m, m.power * _mods.attackPower * DamageBuff, Assist(m, Aim), 14f, false, 1.4f, 0);
                    break;
                case MoveKind.Flicker:
                    _dashT *= 0.6f; _dashSpeedMult = 1.35f;
                    break;
                case MoveKind.SlickDash:
                    _dashT *= 1.3f; _dashSpeedMult = 0.85f;
                    break;
            }
        }

        float _ghostT;   // dash after-images

        void DashUpdate(float dt)
        {
            if (Dashing)
            {
                _ghostT -= dt;
                if (_ghostT <= 0f)
                {
                    _ghostT = 0.035f; _ghostN++;
                    Color dc = MoveColour(_dash);
                    DungeonFx.Ghost(_anim.Renderer, A(dc, 0.5f), 0.25f, true, false);
                    DungeonFx.Embers(Feet, (_ghostN & 1) == 0 ? dc : Accent, 3, 0.25f);
                    if ((_ghostN & 1) == 0) DungeonFx.Flash(Feet + new Vector2(0, 0.3f), dc, 2.8f, 0.9f, 0.3f);   // a trail of light behind you
                }
            }
            if ((!Dashing && !_dashTail) || _dash == null) return;
            if (!Dashing) _dashTail = false;
            switch (_dash.kind)
            {
                case MoveKind.ChargeDash:
                    foreach (var e in Swept(0.75f))
                    {
                        if (!_dashHit.Add(e)) continue;
                        _curType = ElementOf(_dash); _curMove = _dash;
                        Hit(e, _dash.power * _mods.attackPower * DamageBuff, _dashDir, 11f);
                        if (!Alive) return;
                        _dc.Shake(0.06f, 0.06f);
                    }
                    break;
                case MoveKind.Flicker:   // singes whatever you pass through
                    foreach (var e in Swept(0.7f))
                    {
                        if (!_dashHit.Add(e)) continue;
                        _curType = ElementOf(_dash); _curMove = _dash;
                        Hit(e, Mathf.Max(0.2f, _dash.power) * _mods.attackPower * DamageBuff, _dashDir, 2f);
                        if (!Alive) return;
                    }
                    break;
                case MoveKind.SlickDash:
                    _trailT -= dt;
                    if (_trailT <= 0f)
                    {
                        _trailT = 0.05f;
                        var slick = _dash;
                        DungeonZone.Spawn(_dc, Feet, 0.6f, slick.duration, 0.1f, A(MoveColour(slick), 0.35f), z =>
                        {
                            foreach (var e in z.Enemies()) e.Slow(1f - slick.value, slick.duration);
                        });
                    }
                    break;
            }
        }
    }
}
