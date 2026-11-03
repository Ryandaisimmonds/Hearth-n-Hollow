// Hearth & Hollow — the vertical slice gate (Design v2, ticket 05). Pure C#.
// While Slice.On, normal play only reaches what the slice needs: the Cook, Meadow Hollow and Chili Ridge,
// no route mutators, no camp events, and the town as the hub (no old camp screen). Everything else is
// hidden, not deleted. The dev panel can switch it off to reach old content for reference; GameBootstrap
// remembers the choice in PlayerPrefs.
using System;

namespace HearthHollow.Core
{
    public static class Slice
    {
        public static bool On = true;

        /// Dungeons in the slice. Placeholders until tickets 12–14 build Meadow Hollow and Chili Ridge.
        public static readonly string[] RouteIds = { "meadow", "chili" };
        public static readonly ClassId[] Classes = { ClassId.Cook };

        public static bool ShowsRoute(string routeId) { return !On || Array.IndexOf(RouteIds, routeId) >= 0; }
        public static bool HasClass(ClassId id) { return !On || Array.IndexOf(Classes, id) >= 0; }
    }
}
