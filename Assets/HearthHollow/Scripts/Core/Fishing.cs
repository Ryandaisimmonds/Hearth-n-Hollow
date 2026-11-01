// Hearth & Hollow — the fishing pond in Hearth's Hollow. Pure C#.
// One quick catch a day (the day moves on after each expedition). Cast, wait for the bite, then stop a sweeping marker:
// in the centre (perfect) zone = Prime fish, in the wider good zone = Fine, anywhere else = Plain. You always catch something.
using System;

namespace HearthHollow.Core
{
    public static class Fishing
    {
        public static bool CanFish(Profile p) { return p != null && p.lastFishDay != p.day; }

        /// Where the sweeping marker is (0..1, ping-pong) after <paramref name="seconds"/>.
        public static float MarkerAt(TuningData t, float seconds)
        {
            float x = seconds * Math.Max(0.1f, t.fishBarSpeed) % 2f;
            return x <= 1f ? x : 2f - x;
        }

        /// Grade of the catch: 2 Prime (perfect zone), 1 Fine (good zone), 0 Plain.
        public static int GradeFor(TuningData t, float marker, float centre)
        {
            float d = Math.Abs(marker - centre);
            if (d <= t.fishPerfectWidth / 2f) return 2;
            if (d <= t.fishGoodWidth / 2f) return 1;
            return 0;
        }

        /// Land the day's catch into the larder. False if you've already fished today.
        public static bool Catch(GameContent c, Profile p, int grade)
        {
            if (!CanFish(p)) return false;
            Economy.AddToPantry(p, c.tuning.fishIngredient, Math.Max(1, c.tuning.fishCatchCount), Cooking.ClampGrade(grade));
            p.lastFishDay = p.day;
            return true;
        }
    }
}
