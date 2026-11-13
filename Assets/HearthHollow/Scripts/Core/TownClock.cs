// Hearth & Hollow — ticket 58: the town's time of day (it moves on with each day) and how far the Hearth has come.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public enum DayTime { Morning, Afternoon, Evening }

    public static class TownClock
    {
        /// Day 1 is a morning, day 2 an afternoon, day 3 an evening (lanterns lit), then round again.
        public static DayTime For(int day) { return (DayTime)((Math.Max(1, day) - 1) % 3); }

        public static string Emoji(DayTime t) { return t == DayTime.Morning ? "🌅" : t == DayTime.Afternoon ? "☀️" : "🌇"; }
        public static string Name(DayTime t) { return t == DayTime.Morning ? "Morning" : t == DayTime.Afternoon ? "Afternoon" : "Evening"; }

        /// How brightly the lanterns burn (0 = unlit in the morning sun).
        public static float LanternGlow(DayTime t) { return t == DayTime.Morning ? 0f : t == DayTime.Afternoon ? 0.25f : 1f; }

        /// The areas whose clearing grows the Hearth: the slice's three, or every story chapter in the full game.
        public static List<string> ProgressRoutes(GameContent c)
        {
            var ids = new List<string>();
            if (Features.HearthGrowsWithAreas) ids.AddRange(Slice.RouteIds);
            else foreach (var r in c.routes) if (r.chapter > 0) ids.Add(r.id);
            return ids;
        }

        public static int Cleared(GameContent c, Profile p)
        {
            int n = 0;
            foreach (var id in ProgressRoutes(c)) if (p.clearedRouteIds.Contains(id)) n++;
            return n;
        }

        /// 0 at the start, 1 when everything's cleared: drives the Hearth's size and colour.
        public static float Progress(GameContent c, Profile p)
        {
            int total = ProgressRoutes(c).Count;
            return total > 0 ? Cleared(c, p) / (float)total : 0f;
        }
    }
}
