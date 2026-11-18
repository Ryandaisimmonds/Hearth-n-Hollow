// Hearth & Hollow — game feel: hit-stop (a few frames' freeze on impact) and slow motion (boss down).
// DungeonController sets Time.timeScale from TimeScale every frame while the dungeon is running.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public static class GameFeel
    {
        static float _stopUntil, _slowUntil, _slowScale = 1f;
        /// The Effects Lab's slow-motion dial (1 = normal speed). Multiplies everything below.
        public static float LabScale = 1f;
        /// The Effects Lab's quick menu is open: everything holds still.
        public static bool LabPaused;

        /// 1 normally; nearly 0 during a hit-stop; the slow-mo scale during slow motion.
        public static float TimeScale
        {
            get
            {
                if (LabPaused) return 0f;
                float t = Time.unscaledTime;
                if (t < _stopUntil) return 0.03f * LabScale;
                if (t < _slowUntil) return _slowScale * LabScale;
                return LabScale;
            }
        }

        /// Freeze for a moment (seconds, real time). Overlapping calls keep the longest.
        public static void HitStop(float seconds)
        {
            if (seconds <= 0f) return;
            _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + Mathf.Min(seconds, 0.25f));
        }

        public static void SlowMo(float scale, float seconds)
        {
            _slowScale = Mathf.Clamp(scale, 0.05f, 1f);
            _slowUntil = Mathf.Max(_slowUntil, Time.unscaledTime + seconds);
        }

        public static void Reset() { _stopUntil = _slowUntil = 0f; _slowScale = 1f; }

        // domain reload is off: a lab slow-mo mustn't carry into the next play session
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() { Reset(); LabScale = 1f; LabPaused = false; }
    }
}
