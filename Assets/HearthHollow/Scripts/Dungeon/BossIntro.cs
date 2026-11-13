// Hearth & Hollow — a boss's entrance: its title card on the HUD, the camera pushing in on it, the room's lights
// flaring and its braziers roaring up, then a beat of slow motion to take it in.
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public sealed class BossIntro
    {
        readonly DungeonController _dc;
        readonly DungeonCamera _camera;

        /// The boss the camera leans towards (null when there's no entrance on).
        public EnemyController Boss { get; private set; }
        /// How far the camera is pushed in right now (0 = normal, 1 = full).
        public float Push { get; private set; }

        public BossIntro(DungeonController dc, DungeonCamera camera) { _dc = dc; _camera = camera; }

        /// How far in the camera is pushed (0 = normal, 1 = full) at `t` seconds into the entrance.
        public static float PushAt(float t)
        {
            if (t < 0f || t >= DungeonHudState.IntroLength) return 0f;
            float inK = Mathf.Clamp01(t / 0.6f), outK = Mathf.Clamp01((DungeonHudState.IntroLength - t) / 0.8f);
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(inK, outK));
        }

        public void Begin(RoomInstance room, EnemyController boss)
        {
            if (boss == null) return;
            var s = _dc.Session; var hud = _dc.Hud;
            var route = s.Content.Route(s.Run != null ? s.Run.routeId : null);
            hud.IntroName = boss.DisplayName;
            hud.IntroSub = route != null && !string.IsNullOrEmpty(route.name) ? "Guardian of " + route.name : "It has been waiting for you";
            hud.IntroT = 0f;
            Boss = boss;
            _camera.StartPush();
            Vector3 at = boss.Body;
            Color col = Color.Lerp(EnemyController.ElementColour(boss.Data != null ? boss.Data.element : Element.None), new Color(1f, 0.45f, 0.2f), 0.5f);
            DungeonFx.Flash(at, col, 10f, 2.4f, 1.6f);                            // the room flares
            DungeonFx.RingAt(at, new Color(col.r, col.g, col.b, 0.9f), 2.6f, 0.7f);
            if (room != null && room.Lights != null) room.Lights.IgniteWave(at);   // its braziers roar up from the boss outward
            _camera.Shake(0.35f, 0.4f);
            GameLook.Punch(0.8f);
            HearthHollow.Runtime.GameAudio.Play("boss");
            GameFeel.SlowMo(0.4f, 1.2f);                                           // a beat to take it in
        }

        /// Once a frame (unscaled time): runs the title card's clock and the push-in.
        public void Tick(float dt)
        {
            var hud = _dc.Hud;
            if (hud.IntroT < 0f) { Push = 0f; return; }
            hud.IntroT += dt;
            Push = PushAt(hud.IntroT);
            if (hud.IntroT >= DungeonHudState.IntroLength) { hud.IntroT = -1f; Boss = null; Push = 0f; }
        }
    }
}
