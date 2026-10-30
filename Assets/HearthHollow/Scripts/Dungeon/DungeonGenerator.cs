// Hearth & Hollow — builds a floor plan: rooms on a grid, joined by N/S/E/W doors.
// No Unity objects here — the controller turns the plan into room prefabs.
//
// Each floor:  Start room → a branching web of event rooms → the far end holds the way on.
//   Floors before the last: the farthest room has the LADDER, the next-farthest dead end the EXIT.
//   Last floor: the farthest room is the BOSS arena; its exit appears when the boss falls.
//   A few rooms are calm: forage groves (more on Trails) and, from floor 2, a campfire.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class RoomNode
    {
        public int index;
        public Vector2Int cell;
        public RoomKind kind = RoomKind.Combat;
        public int depth;
        public readonly bool[] doors = new bool[4];        // indexed by (int)Side
        public bool Has(Side s) { return doors[(int)s]; }
    }

    public class FloorPlan
    {
        public readonly List<RoomNode> rooms = new List<RoomNode>();
        public RoomNode start;
        public RoomNode At(Vector2Int cell) { foreach (var r in rooms) if (r.cell == cell) return r; return null; }
        public RoomNode Neighbour(RoomNode r, Side s) { return r.Has(s) ? At(r.cell + RoomTemplate.Step(s)) : null; }
    }

    public static class DungeonGenerator
    {
        static readonly Side[] Sides = { Side.North, Side.South, Side.East, Side.West };

        /// <param name="roomCount">Total rooms including start.</param>
        /// <param name="lastFloor">Boss floor (no ladder).</param>
        /// <param name="gatherShare">Route's gather chance (≈ share of forage rooms).</param>
        public static FloorPlan Generate(int seed, int roomCount, bool lastFloor, int floor, float gatherShare)
        {
            var rng = new System.Random(seed);
            var plan = new FloorPlan();
            roomCount = Math.Max(lastFloor ? 5 : 6, roomCount);

            var start = Add(plan, Vector2Int.zero);
            plan.start = start;
            start.kind = RoomKind.Start;

            // grow: mostly extend from recent rooms (long corridors), sometimes branch from anywhere
            int guard = 0;
            while (plan.rooms.Count < roomCount && guard++ < 2000)
            {
                RoomNode from = rng.NextDouble() < 0.6
                    ? plan.rooms[Math.Max(0, plan.rooms.Count - 1 - rng.Next(Math.Min(3, plan.rooms.Count)))]
                    : plan.rooms[rng.Next(plan.rooms.Count)];
                Side s = Sides[rng.Next(4)];
                Vector2Int c = from.cell + RoomTemplate.Step(s);
                if (plan.At(c) != null) continue;
                if (CountNeighbours(plan, c) > 1 && rng.NextDouble() < 0.7) continue;   // avoid tight clumps
                var node = Add(plan, c);
                Link(from, node, s);
            }

            // depths from the start (BFS)
            var queue = new Queue<RoomNode>();
            foreach (var r in plan.rooms) r.depth = -1;
            start.depth = 0; queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var r = queue.Dequeue();
                foreach (Side s in Sides)
                {
                    var n = plan.Neighbour(r, s);
                    if (n != null && n.depth < 0) { n.depth = r.depth + 1; queue.Enqueue(n); }
                }
            }

            // the way on: farthest room; exits prefer dead ends
            var byDepth = new List<RoomNode>(plan.rooms);
            byDepth.Remove(start);
            byDepth.Sort((a, b) => b.depth != a.depth ? b.depth.CompareTo(a.depth) : DoorCount(a).CompareTo(DoorCount(b)));
            var far = byDepth[0];
            if (lastFloor) far.kind = RoomKind.Boss;
            else
            {
                far.kind = RoomKind.Ladder;
                RoomNode exit = null;
                foreach (var r in byDepth) if (r != far && DoorCount(r) == 1) { exit = r; break; }
                if (exit == null) exit = byDepth.Count > 1 ? byDepth[1] : null;
                if (exit != null) exit.kind = RoomKind.Exit;
            }

            // calm rooms: forage groves (dead ends first) and a campfire from floor 2
            var spare = byDepth.FindAll(r => r.kind == RoomKind.Combat);
            Shuffle(spare, rng);
            spare.Sort((a, b) => DoorCount(a).CompareTo(DoorCount(b)));
            int groves = Math.Max(1, (int)Math.Round(plan.rooms.Count * gatherShare * 0.6f));
            for (int i = 0; i < spare.Count && groves > 0; i++)
                if (spare[i].kind == RoomKind.Combat && spare[i].depth >= 1) { spare[i].kind = RoomKind.Gather; groves--; }
            if (floor >= 2)
            {
                var mid = spare.FindAll(r => r.kind == RoomKind.Combat);
                if (mid.Count > 2) mid[rng.Next(mid.Count)].kind = RoomKind.Rest;
            }
            // always at least two fights per floor
            int fights = plan.rooms.FindAll(r => r.kind == RoomKind.Combat).Count;
            for (int i = 0; fights < 2 && i < plan.rooms.Count; i++)
                if (plan.rooms[i].kind == RoomKind.Gather || plan.rooms[i].kind == RoomKind.Rest) { plan.rooms[i].kind = RoomKind.Combat; fights++; }
            return plan;
        }

        static RoomNode Add(FloorPlan plan, Vector2Int cell)
        {
            var n = new RoomNode { index = plan.rooms.Count, cell = cell };
            plan.rooms.Add(n);
            return n;
        }

        static void Link(RoomNode a, RoomNode b, Side fromA)
        {
            a.doors[(int)fromA] = true;
            b.doors[(int)RoomTemplate.Opposite(fromA)] = true;
        }

        static int CountNeighbours(FloorPlan plan, Vector2Int c)
        {
            int n = 0;
            foreach (Side s in Sides) if (plan.At(c + RoomTemplate.Step(s)) != null) n++;
            return n;
        }

        static int DoorCount(RoomNode r) { int n = 0; for (int i = 0; i < 4; i++) if (r.doors[i]) n++; return n; }

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; }
        }
    }
}
