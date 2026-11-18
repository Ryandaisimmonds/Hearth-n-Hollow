// Hearth & Hollow — the vertical slice gate (Design v2). Pure C#.
// While Slice.On, normal play only reaches what the slice needs: the Cook, Meadow Hollow, Chili Ridge and Frost Hollow,
// no route mutators, no camp events, and the town as the hub (no old camp screen). Everything else is hidden, not
// deleted. The dev panel can switch it off to reach old content for reference; GameBootstrap remembers the choice
// in PlayerPrefs.
//
// The game never asks "is the slice on?" directly: it asks Features a named question, and Features is the one
// place that reads Slice.On. To change what the slice includes, change the answer here.
using System;

namespace HearthHollow.Core
{
    public static class Slice
    {
        /// The switch (GameBootstrap, the dev panel and tests set it). Read it through Features.
        public static bool On = true;

        /// The slice's areas, in story order.
        public static readonly string[] RouteIds = { Ids.Area.Meadow, Ids.Area.Chili, Ids.Area.FrostHollow };
        public static readonly ClassId[] Classes = { ClassId.Cook };

        /// The fall's nameless dungeon never shows on the Route board.
        public static bool ShowsRoute(string routeId) { return routeId != GameSession.FallRoute && (Features.AllRoutes || Array.IndexOf(RouteIds, routeId) >= 0); }
        public static bool HasClass(ClassId id) { return Features.AllClasses || Array.IndexOf(Classes, id) >= 0; }
    }

    /// What's in this build, as named questions. The game reads Slice.On only through here (the dev panel's
    /// switch, which flips and shows it, is the one exception).
    public static class Features
    {
        static bool InSlice { get { return Slice.On; } }

        // ── what you can reach ──
        /// Every route is on the board (otherwise only Slice.RouteIds).
        public static bool AllRoutes { get { return !InSlice; } }
        /// Every class can join (otherwise only Slice.Classes).
        public static bool AllClasses { get { return !InSlice; } }
        /// Every trinket is sold and dropped (otherwise only the ones marked for the slice).
        public static bool AllTrinkets { get { return !InSlice; } }
        /// Any ingredient can be found (otherwise only from the slice's areas, the starter food and the pond).
        public static bool AllIngredients { get { return !InSlice; } }
        /// A third trinket slot (opened by the Cavern Hunt, which isn't in the slice).
        public static bool ThirdTrinketSlot { get { return !InSlice; } }
        /// Routes roll mutators.
        public static bool Mutators { get { return !InSlice; } }
        /// The camp rolls daily events (Storm, Harvest Moon, Merchant, …).
        public static bool CampEvents { get { return !InSlice; } }

        // ── how the game is framed ──
        /// The walkable town is home; there's no old camp screen to go back to.
        public static bool TownIsHome { get { return InSlice; } }
        /// A new game asks you to pick a class (otherwise you're the Cook and it opens with the fall).
        public static bool ClassSelect { get { return !InSlice; } }
        /// A new game opens with the fall into a nameless dungeon.
        public static bool OpensWithFall { get { return InSlice; } }
        /// Old Wren's thread guides you: she comes over, gates setting out, and the town HUD shows her next step.
        public static bool WrenGuides { get { return InSlice; } }
        /// You must fill the tray to set out.
        public static bool FullTrayToSetOut { get { return InSlice; } }
        /// The Hearth grows with the slice's areas cleared (otherwise with the Ember Path's story chapters).
        public static bool HearthGrowsWithAreas { get { return InSlice; } }
        /// The Route board's expedition panel is slimmed down: tray first, route details trimmed.
        public static bool SlimRouteBoard { get { return InSlice; } }
    }
}
