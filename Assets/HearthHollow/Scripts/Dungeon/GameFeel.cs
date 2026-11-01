// Hearth & Hollow — game feel: hit-stop (a few frames' freeze on impact) and slow motion (boss down).
// DungeonController sets Time.timeScale from TimeScale every frame while the dungeon is running.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public static class GameFeel
    {
        static float _stopUntil, _slowUntil, _slowScale = 1f;

        /// 1 normally; nearly 0 during a hit-stop; the slow-mo scale during slow motion.
        public static float TimeScale
        {
            get
            {
                float t = Time.unscaledTime;
                if (t < _stopUntil) return 0.03f;
                if (t < _slowUntil) return _slowScale;
                return 1f;
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
    }
}
