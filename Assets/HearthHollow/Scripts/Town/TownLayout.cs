// Hearth & Hollow — the layout of Hearth's Hollow, the walkable town (milestone 1: grey-box).
// Pure data: which tiles are open ground, where the fences of the future plots run, and every spot you can walk up to.
// Coordinates are in tiles (1 tile = 1 world unit = 32 art pixels); (0,0) is the bottom-left corner.
//
//   north:  lean-to + hammock (you wake here), Old Wren, signpost back to the camp screen
//   centre: the Hearth fire, the cooking area, the larder crates
//   west:   training yard (skills)          east: smithy (Upgrades) + Workshop (kitchen upgrades)
//   south:  the town gate (Route Board)     fenced plots NW / NE / SE are kept for future updates
//   south-west: a little market street (notice board, merchant, armourer, baker, flowers) and the fishing pond
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Town
{
    public enum TownAction { Hearth, Cooking, Larder, Upgrades, Workshop, Training, Gate, Bed, Wren, BackToCamp, Plot,
                             Notice, Merchant, Armourer, Baker, Florist, Fishing, ClassNpc, Forager, LampMaker, SpiceRack }

    public class TownSpot
    {
        public string id, name, emoji, verb;
        public TownAction action;
        public Vector2 pos;          // feet / base of the object
        public Vector2 size;         // placeholder block size (tiles); zero = no block
        public Color color;
        public bool solid = true;    // blocks walking
        public float reach = 1.6f;   // interact distance from pos
        public HearthHollow.Core.ClassId classId;   // ClassNpc: who's standing there
    }

    public static class TownLayout
    {
        public const int Width = 50, Height = 40;
        public static readonly Vector2 Spawn = new Vector2(21f, 32.5f);

        /// Open ground (inclusive tile rects: xMin, yMin, xMax, yMax). Everything else is wall / trees.
        public static readonly RectInt[] Open =
        {
            R(16, 12, 34, 27),   // centre square
            R(17, 28, 33, 37),   // north: lean-to + Old Wren
            R(2, 13, 12, 27),    // west: training yard
            R(13, 17, 15, 23),   // west lane
            R(38, 11, 47, 29),   // east: smithy + workshop
            R(35, 17, 37, 23),   // east lane
            R(22, 3, 28, 11),    // south road to the gate
            R(9, 4, 21, 9),      // market street (off the south road)
            R(2, 3, 8, 11),      // fishing pond meadow (end of the market street)
            // fenced plots for future updates (open ground behind a fence)
            R(2, 28, 12, 37),    // NW plot (fence on row 28)
            R(38, 30, 47, 37),   // NE plot (fence on row 30)
            R(38, 3, 47, 10),    // SE plot (fence on row 10)
        };

        /// Fence runs (tile rows): xMin, xMax, y. Solid; the plots behind them are kept for future buildings.
        public static readonly Vector3Int[] Fences = { new Vector3Int(2, 12, 28), new Vector3Int(38, 47, 30), new Vector3Int(38, 47, 10) };

        /// Paved ground (art only): the stone plaza round the Hearth and the cobbled lanes / road / market street.
        public static readonly RectInt Plaza = R(21, 15, 29, 23);   // 3 × 3 of the big 3×3 slabs
        public static readonly RectInt[] Paths =
        {
            R(13, 18, 19, 22),   // west lane into the square
            R(31, 18, 44, 22),   // east lane, on into the smithy yard
            R(41, 16, 43, 24),   // smithy ↔ workshop
            R(23, 4, 27, 14),    // south road to the gate
            R(9, 5, 22, 7),      // market street
            R(23, 24, 27, 31),   // north path up to the lean-to
        };
        public static bool IsPlaza(int x, int y) { return x >= Plaza.xMin && x < Plaza.xMax && y >= Plaza.yMin && y < Plaza.yMax; }
        public static bool IsPath(int x, int y)
        {
            foreach (var r in Paths) if (x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax) return true;
            return false;
        }
        public static bool IsFence(int x, int y)
        {
            foreach (var f in Fences) if (y == f.z && x >= f.x && x <= f.y) return true;
            return false;
        }

        /// The pond's water (solid; you fish from the jetty on its east side).
        public static readonly Rect Pond = new Rect(2.6f, 5.4f, 4.2f, 4.4f);

        /// Open ground that isn't water — where the little creatures may wander.
        public static bool IsWalkable(float x, float y)
        {
            if (!IsOpen(Mathf.FloorToInt(x), Mathf.FloorToInt(y)) || IsFence(Mathf.FloorToInt(x), Mathf.FloorToInt(y))) return false;
            return !Pond.Contains(new Vector2(x, y));
        }

        /// Where your other two classes hang around in the training yard.
        public static readonly Vector2[] ClassSpots = { new Vector2(4.5f, 23.5f), new Vector2(9.5f, 23.5f) };
        /// Where a passing forager leaves a basket (forager days).
        public static readonly Vector2 ForagerBasket = new Vector2(27.2f, 6f);
        /// Where the visitors stand at their stalls (merchant / armourer days).
        public static readonly Vector2 MerchantStand = new Vector2(14.5f, 7.4f), ArmourerStand = new Vector2(18.5f, 7.4f);   // in front of their stalls

        static RectInt R(int x0, int y0, int x1, int y1) { return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1); }

        public static bool IsOpen(int x, int y)
        {
            foreach (var r in Open) if (x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax) return true;
            return false;
        }

        public static List<TownSpot> Spots()
        {
            var s = new List<TownSpot>();
            s.Add(Spot("hearth", "The Hearth", "🔥", "Look at the fire", TownAction.Hearth, 25f, 19.5f, 2.2f, 0.9f, new Color(0.45f, 0.43f, 0.4f)));
            s.Add(Spot("cooking", "Cooking area", "🍳", "Cook", TownAction.Cooking, 20f, 24f, 3f, 1.2f, new Color(0.55f, 0.36f, 0.2f)));
            s.Add(Spot("larder", "Larder", "🥕", "Open the larder", TownAction.Larder, 30f, 24f, 2.4f, 1.4f, new Color(0.62f, 0.45f, 0.26f)));
            s.Add(Spot("spice", "Spice rack", "🧂", "Season a meal", TownAction.SpiceRack, 17.2f, 24.2f, 1.2f, 1.1f, new Color(0.5f, 0.32f, 0.2f)));   // ticket 27
            s.Add(Spot("smithy", "Smithy", "⚒️", "Gear & trinkets", TownAction.Upgrades, 42f, 25f, 3f, 1.6f, new Color(0.35f, 0.35f, 0.38f)));
            s.Add(Spot("workshop", "Workshop", "🔨", "Kitchen upgrades", TownAction.Workshop, 42f, 15f, 3f, 1.6f, new Color(0.6f, 0.42f, 0.22f)));
            s.Add(Spot("training", "Training yard", "⭐", "Skills & loadout", TownAction.Training, 7f, 20f, 1f, 1.5f, new Color(0.85f, 0.75f, 0.4f)));
            s.Add(Spot("gate", "Route board", "🗺️", "Choose a route & set out", TownAction.Gate, 25f, 3.2f, 3f, 0.8f, new Color(0.45f, 0.3f, 0.18f)));
            s.Add(Spot("bed", "Lean-to & hammock", "🛏️", "Rest", TownAction.Bed, 21f, 35f, 3f, 1.5f, new Color(0.45f, 0.55f, 0.3f)));
            var wren = Spot("wren", "Old Wren", "👵", "Talk", TownAction.Wren, 28f, 34f, 0f, 0f, Color.white);
            wren.reach = 1.4f;
            s.Add(wren);
            s.Add(Spot("signpost", "Signpost", "🪧", "Back to the camp screen", TownAction.BackToCamp, 24.5f, 31.5f, 0.4f, 1f, new Color(0.6f, 0.45f, 0.25f)));
            s.Add(Spot("plot_nw", "Fenced plot", "🚧", "Look", TownAction.Plot, 7f, 27.2f, 0.4f, 0.8f, new Color(0.6f, 0.45f, 0.25f)));
            s.Add(Spot("plot_ne", "Fenced plot", "🚧", "Look", TownAction.Plot, 42.5f, 29.2f, 0.4f, 0.8f, new Color(0.6f, 0.45f, 0.25f)));
            s.Add(Spot("plot_se", "Fenced plot", "🚧", "Look", TownAction.Plot, 42.5f, 11.2f, 0.4f, 0.8f, new Color(0.6f, 0.45f, 0.25f)));
            // market street
            s.Add(Spot("notice", "Notice board", "📜", "Today's news", TownAction.Notice, 11f, 8.6f, 1.4f, 1.2f, new Color(0.55f, 0.4f, 0.22f)));
            s.Add(Spot("merchant", "Merchant's stall", "🛒", "Browse", TownAction.Merchant, 14.5f, 8.4f, 2.4f, 1.3f, new Color(0.5f, 0.3f, 0.6f)));
            s.Add(Spot("armourer", "Armourer's stall", "🛡️", "Browse", TownAction.Armourer, 18.5f, 8.4f, 2.4f, 1.3f, new Color(0.45f, 0.45f, 0.5f)));
            s.Add(Spot("baker", "Baker's cart", "🥖", "Chat", TownAction.Baker, 12.5f, 4.2f, 2f, 1.1f, new Color(0.8f, 0.6f, 0.35f)));
            s.Add(Spot("florist", "Flower cart", "🌼", "Chat", TownAction.Florist, 17f, 4.2f, 2f, 1.1f, new Color(0.85f, 0.5f, 0.6f)));
            // fishing pond
            var fish = Spot("fishing", "Fishing jetty", "🎣", "Fish", TownAction.Fishing, 7.4f, 7.2f, 1.2f, 0.3f, new Color(0.5f, 0.35f, 0.2f));
            fish.solid = false; fish.reach = 1.3f;
            s.Add(fish);
            return s;
        }

        static TownSpot Spot(string id, string name, string emoji, string verb, TownAction a, float x, float y, float w, float h, Color c)
        {
            return new TownSpot { id = id, name = name, emoji = emoji, verb = verb, action = a, pos = new Vector2(x, y), size = new Vector2(w, h), color = c };
        }
    }
}
