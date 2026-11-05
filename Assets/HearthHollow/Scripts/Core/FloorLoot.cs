// Hearth & Hollow — ticket 48: loot on the floor. Pure C#.
// Anything that won't fit in the bag lands on the floor (it glows there) and stays until you leave that floor.
// Pick it up with E: it goes straight in if there's room; otherwise the bag opens with it in a "holding" box and you
// swap it with a bag slot (the swapped-out item goes on the floor where the held one was). Close the bag without
// swapping and it stays where it was. X (or dragging an item out of the bag) drops it at your feet.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    [Serializable] public class FloorItem
    {
        public int id;
        public string ingredientId; public int count;
        public int floor;
        public float x, y;
        public bool placed;   // false = the dungeon puts it at your feet next frame
    }

    public partial class RunState
    {
        public List<FloorItem> floorLoot = new List<FloorItem>();
        public int floorLootNext = 1;
    }

    public partial class GameSession
    {
        bool _lootSpotSet; float _lootX, _lootY;

        /// The dungeon tells us where the next drops land (a monster's body), until ClearLootSpot.
        public void SetLootSpot(float x, float y) { _lootSpotSet = true; _lootX = x; _lootY = y; }
        public void ClearLootSpot() { _lootSpotSet = false; }

        /// Loot lying on this floor.
        public List<FloorItem> FloorLoot
        {
            get
            {
                var list = new List<FloorItem>();
                if (Run != null && Run.floorLoot != null) foreach (var it in Run.floorLoot) if (it != null && it.floor == Run.floor && it.count > 0) list.Add(it);
                return list;
            }
        }

        /// The floor item you picked up with a full bag (the bag is open to swap it in), or null.
        public FloorItem HeldItem { get; private set; }

        FloorItem DropOnFloor(string ingredientId, int count)
        {
            if (Run == null || string.IsNullOrEmpty(ingredientId) || count <= 0) return null;
            if (Run.floorLoot == null) Run.floorLoot = new List<FloorItem>();
            var it = new FloorItem { id = Run.floorLootNext++, ingredientId = ingredientId, count = count, floor = Run.floor };
            if (_lootSpotSet) { it.x = _lootX; it.y = _lootY; it.placed = true; }
            Run.floorLoot.Add(it);
            return it;
        }

        /// E on a floor item. Fills what fits; if anything's left, holds it and returns true so the bag opens to swap.
        public bool DungeonPickUp(FloorItem it)
        {
            if (!InDungeon || it == null || it.count <= 0 || Run.floorLoot == null || !Run.floorLoot.Contains(it)) return false;
            int left = Bag.Add(Run, it.ingredientId, it.count, T.stackSize);
            int got = it.count - left;
            if (got > 0)
            {
                var ing = Content.Ingredient(it.ingredientId);
                Emit(new GameEvent(GameEventType.Loot, got, true, "+" + got + " " + (ing != null ? ing.emoji + " " + ing.name : it.ingredientId)));
            }
            it.count = left;
            if (left <= 0) { Run.floorLoot.Remove(it); HeldItem = null; Touch(false); return false; }
            HeldItem = it;   // the bag is full: choose what to swap it for
            Touch(false);
            return true;
        }

        /// Swap the held floor item with a bag slot: the slot's contents go on the floor where the held item lay.
        public bool SwapHeld(int slot)
        {
            var h = HeldItem;
            if (h == null || Run == null || slot < 0 || slot >= Run.bag.Count) return false;
            var b = Run.bag[slot];
            bool empty = Bag.IsEmpty(b);
            if (!empty && b.ingredientId == h.ingredientId && b.count < T.stackSize)
            {
                int n = Math.Min(h.count, T.stackSize - b.count);
                b.count += n; h.count -= n;
            }
            else
            {
                string oldId = empty ? null : b.ingredientId; int oldCount = empty ? 0 : b.count;
                int n = Math.Min(h.count, Math.Max(1, T.stackSize));
                Run.bag[slot] = new BagSlot { ingredientId = h.ingredientId, count = n };
                h.count -= n;
                if (oldId != null)
                {
                    var put = new FloorItem { id = Run.floorLootNext++, ingredientId = oldId, count = oldCount, floor = Run.floor, x = h.x, y = h.y, placed = h.placed };
                    Run.floorLoot.Add(put);
                }
            }
            var ing = Content.Ingredient(h.ingredientId);
            Emit(new GameEvent(GameEventType.Loot, 0, true, "Swapped in " + (ing != null ? ing.emoji + " " + ing.name : h.ingredientId)));
            if (h.count <= 0) Run.floorLoot.Remove(h);
            HeldItem = null;
            Touch(false);
            return true;
        }

        /// Closed the bag without swapping: the held item stays on the floor where it was.
        public void CancelHeld() { if (HeldItem != null) { HeldItem = null; Touch(false); } }

        /// X / drag out: drop a bag slot at your feet (you can pick it up again until you leave the floor).
        public bool BagDrop(int slot)
        {
            if (!InDungeon || slot < 0 || slot >= Run.bag.Count || Bag.IsEmpty(Run.bag[slot])) return false;
            var b = Run.bag[slot];
            ClearLootSpot();
            DropOnFloor(b.ingredientId, b.count);
            Bag.Discard(Run, slot);
            Touch(false);
            return true;
        }

        /// Leaving a floor leaves its loot behind.
        void ForgetFloorLoot()
        {
            HeldItem = null;
            if (Run != null && Run.floorLoot != null) Run.floorLoot.RemoveAll(it => it == null || it.floor != Run.floor);
        }
    }
}
