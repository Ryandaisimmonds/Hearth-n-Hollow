// Hearth & Hollow — a hand-made dungeon room. Put this on the root of a room prefab.
//
// HOW TO MAKE A ROOM
//   1. Duplicate a room in Assets/HearthHollow/Dungeon/Rooms (or the "Room_Blank" one) and open it.
//   2. Paint with the MARKER tiles (palette: Dungeon/Tiles/Dungeon Markers):
//        Floor tilemap  → Floor marker everywhere you can walk.
//        Walls tilemap  → Wall marker for walls / pillars, Wall Face for the front face of the north wall,
//                         Hazard for water / lava / pits (blocks walking).
//        Decor tilemap  → anything purely visual (optional; themes also scatter their own decor).
//      At runtime each dungeon's theme swaps the markers for its own tiles and colours, so one layout
//      works for every route. You can also paint theme-specific tiles directly — they're left alone.
//   3. Keep the standard footprint (Width × Height cells) and leave the four door gaps open
//      (centre of each side). Unused doors are sealed automatically.
//   4. Add child objects with a SpawnPoint component for enemies, forage spots, the campfire, ladder, exit.
//   5. Tick which kinds of room this layout can be used as, then add the prefab to a theme's room list
//      or to the shared pool in Dungeon Settings.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    public enum RoomKind { Start, Combat, Gather, Rest, Ladder, Exit, Boss }
    public enum Side { North, South, East, West }

    public class RoomTemplate : MonoBehaviour
    {
        /// Standard footprint in cells: x 0..Width-1, y 0..Height-1 (walls included).
        public const int Width = 21, Height = 13;   // Moonlighter-style single-screen rooms, sized for cameraSize 7 (31 Oct; was 23 × 14, briefly 19 × 11)

        [Tooltip("What this layout can be used as. A room can serve several roles.")]
        public List<RoomKind> usableAs = new List<RoomKind> { RoomKind.Combat };
        [Tooltip("Higher = picked more often among rooms of the same kind.")]
        [Min(0)] public int weight = 1;

        [Header("Tilemaps (painted with marker tiles)")]
        public Tilemap floor;
        public Tilemap walls;
        public Tilemap decor;

        /// Cells of the opening on one side (3 wide; the north opening goes through the face and top rows).
        public static List<Vector3Int> DoorCells(Side side)
        {
            var list = new List<Vector3Int>();
            int cx = Width / 2, cy = (Height - 3) / 2 + 1;   // cx = 10, cy = 6 (floor rows are 1..Height-3)
            switch (side)
            {
                case Side.North: for (int x = cx - 1; x <= cx + 1; x++) { list.Add(new Vector3Int(x, Height - 2, 0)); list.Add(new Vector3Int(x, Height - 1, 0)); } break;
                case Side.South: for (int x = cx - 1; x <= cx + 1; x++) list.Add(new Vector3Int(x, 0, 0)); break;
                case Side.West: for (int y = cy - 1; y <= cy + 1; y++) list.Add(new Vector3Int(0, y, 0)); break;
                case Side.East: for (int y = cy - 1; y <= cy + 1; y++) list.Add(new Vector3Int(Width - 1, y, 0)); break;
            }
            return list;
        }

        /// Local-space centre of a door opening (on the wall line).
        public static Vector2 DoorCentre(Side side)
        {
            int cx = Width / 2, cy = (Height - 3) / 2 + 1;
            switch (side)
            {
                case Side.North: return new Vector2(cx + 0.5f, Height - 1.0f);
                case Side.South: return new Vector2(cx + 0.5f, 0.5f);
                case Side.West: return new Vector2(0.5f, cy + 0.5f);
                default: return new Vector2(Width - 0.5f, cy + 0.5f);
            }
        }

        /// Where the player appears when entering through <paramref name="side"/> (just inside the door).
        public static Vector2 EntryPoint(Side side)
        {
            Vector2 c = DoorCentre(side);
            switch (side)
            {
                case Side.North: return c + new Vector2(0, -2.6f);
                case Side.South: return c + new Vector2(0, 1.6f);
                case Side.West: return c + new Vector2(1.6f, 0);
                default: return c + new Vector2(-1.6f, 0);
            }
        }

        public static Vector2 Centre { get { return new Vector2(Width / 2f, (Height - 2) / 2f + 0.5f); } }

        public static Side Opposite(Side s)
        {
            switch (s) { case Side.North: return Side.South; case Side.South: return Side.North; case Side.East: return Side.West; default: return Side.East; }
        }

        public static Vector2Int Step(Side s)
        {
            switch (s) { case Side.North: return Vector2Int.up; case Side.South: return Vector2Int.down; case Side.East: return Vector2Int.right; default: return Vector2Int.left; }
        }

        public List<SpawnPoint> SpawnPoints(SpawnKind kind)
        {
            var list = new List<SpawnPoint>();
            foreach (var sp in GetComponentsInChildren<SpawnPoint>(true)) if (sp.kind == kind) list.Add(sp);
            return list;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Vector3 o = transform.position;
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireCube(o + new Vector3(Width / 2f, Height / 2f), new Vector3(Width, Height));
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.8f);
            foreach (Side s in System.Enum.GetValues(typeof(Side)))
            {
                Vector2 c = DoorCentre(s);
                bool vertical = s == Side.East || s == Side.West;
                Gizmos.DrawWireCube(o + (Vector3)c, vertical ? new Vector3(1, 3) : new Vector3(3, s == Side.North ? 2 : 1));
            }
        }
#endif
    }
}
