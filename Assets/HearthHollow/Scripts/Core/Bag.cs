// Hearth & Hollow — run bag (Agent C). Pure C#, no UnityEngine.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Bag
    {
        public static bool IsEmpty(BagSlot s)
        {
            return s == null || string.IsNullOrEmpty(s.ingredientId) || s.count <= 0;
        }

        private static void Normalise(BagSlot s)
        {
            if (IsEmpty(s)) { s.ingredientId = ""; s.count = 0; }
        }

        public static void Init(RunState run, int slots)
        {
            run.bag = new List<BagSlot>();
            for (int i = 0; i < slots; i++) run.bag.Add(new BagSlot { ingredientId = "", count = 0 });
        }

        public static int Add(RunState run, string ingredientId, int count, int stackSize)
        {
            if (string.IsNullOrEmpty(ingredientId) || count <= 0) return 0;
            if (stackSize < 1) stackSize = 1;
            int left = count;
            for (int i = 0; i < run.bag.Count && left > 0; i++)
            {
                BagSlot s = run.bag[i];
                if (s == null) { s = new BagSlot(); run.bag[i] = s; }
                Normalise(s);
                if (s.ingredientId == ingredientId && s.count < stackSize)
                {
                    int n = Math.Min(left, stackSize - s.count);
                    s.count += n; left -= n;
                }
            }
            for (int i = 0; i < run.bag.Count && left > 0; i++)
            {
                BagSlot s = run.bag[i];
                if (IsEmpty(s))
                {
                    int n = Math.Min(left, stackSize);
                    s.ingredientId = ingredientId; s.count = n; left -= n;
                }
            }
            return left;
        }

        public static void Move(RunState run, int from, int to, int stackSize)
        {
            if (from == to || from < 0 || to < 0 || from >= run.bag.Count || to >= run.bag.Count) return;
            BagSlot a = run.bag[from], b = run.bag[to];
            if (a == null) { a = new BagSlot(); run.bag[from] = a; }
            if (b == null) { b = new BagSlot(); run.bag[to] = b; }
            Normalise(a); Normalise(b);
            if (IsEmpty(a)) return;
            if (!IsEmpty(b) && a.ingredientId == b.ingredientId && b.count < stackSize)
            {
                int n = Math.Min(a.count, stackSize - b.count);
                b.count += n; a.count -= n;
                Normalise(a);
                return;
            }
            run.bag[from] = b; run.bag[to] = a;   // swap (also moves into an empty slot)
        }

        public static void Discard(RunState run, int slot)
        {
            if (slot < 0 || slot >= run.bag.Count) return;
            run.bag[slot] = new BagSlot { ingredientId = "", count = 0 };
        }

        public static bool IsFull(RunState run, int stackSize)
        {
            for (int i = 0; i < run.bag.Count; i++)
            {
                BagSlot s = run.bag[i];
                if (IsEmpty(s) || s.count < stackSize) return false;
            }
            return true;
        }

        public static int FreeSlots(RunState run)
        {
            int n = 0;
            for (int i = 0; i < run.bag.Count; i++) if (IsEmpty(run.bag[i])) n++;
            return n;
        }

        public static List<ItemStack> Contents(RunState run)
        {
            var list = new List<ItemStack>();
            for (int i = 0; i < run.bag.Count; i++)
            {
                BagSlot s = run.bag[i];
                if (IsEmpty(s)) continue;
                ItemStack st = list.Find(x => x.ingredientId == s.ingredientId);
                if (st == null) list.Add(new ItemStack { ingredientId = s.ingredientId, count = s.count });
                else st.count += s.count;
            }
            return list;
        }
    }
}
