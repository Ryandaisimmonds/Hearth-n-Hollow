// Hearth & Hollow — ticket 44: the tray's typed slots. Pure C#.
// A tray holds traySize (3) meals: one 🗡️ Attack meal, one ❤️ Health meal and one ✨ Free meal (anything).
// The slots aren't positions: Profile.packed stays a plain list, and Assign works out which meal fills which slot.
// A meal's kind comes from its stats (attack / crit = Attack; HP, armour, healing = Health; a two-stat meal can be
// either). Camp Bread (an "always" meal) fits any slot: +1 attack in the Attack slot, its own small buffs elsewhere.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public enum TraySlot { Attack, Health, Free }

    public static class Tray
    {
        public static readonly TraySlot[] Slots = { TraySlot.Attack, TraySlot.Health, TraySlot.Free };

        public static bool IsAttackStat(BuffStat s) { return s == BuffStat.Attack || s == BuffStat.DamagePct || s == BuffStat.CritPct; }
        public static bool IsHealthStat(BuffStat s)
        {
            return s == BuffStat.MaxHp || s == BuffStat.MaxHpPct || s == BuffStat.Armour || s == BuffStat.Regen || s == BuffStat.HealOnKill;
        }

        public static bool IsAttackMeal(RecipeData r) { return r != null && r.buffs != null && r.buffs.Exists(b => IsAttackStat(b.stat)); }
        public static bool IsHealthMeal(RecipeData r) { return r != null && r.buffs != null && r.buffs.Exists(b => IsHealthStat(b.stat)); }

        /// Can this recipe sit in this slot?
        public static bool Fits(RecipeData r, TraySlot s)
        {
            if (r == null) return false;
            if (s == TraySlot.Free || r.always) return true;
            return s == TraySlot.Attack ? IsAttackMeal(r) : IsHealthMeal(r);
        }

        public static string SlotEmoji(TraySlot s) { return s == TraySlot.Attack ? "🗡️" : s == TraySlot.Health ? "❤️" : "✨"; }
        public static string SlotName(TraySlot s) { return s == TraySlot.Attack ? "Attack" : s == TraySlot.Health ? "Health" : "Free"; }

        /// Which slot each meal fills (same order as <paramref name="recipeIds"/>). Typed slots are filled first, by real
        /// meals before Camp Bread. A meal that fits nothing left is still placed (Free) so nothing vanishes.
        public static TraySlot[] Assign(GameContent c, IList<string> recipeIds)
        {
            int n = recipeIds != null ? recipeIds.Count : 0;
            var best = new TraySlot[n];
            for (int i = 0; i < n; i++) best[i] = TraySlot.Free;
            if (n == 0) return best;
            var recs = new RecipeData[n];
            for (int i = 0; i < n; i++) recs[i] = c.Recipe(recipeIds[i]);
            int bestScore = int.MinValue;
            var cur = new int[n];
            Search(recs, cur, 0, new bool[Slots.Length], ref bestScore, best);
            return best;
        }

        static void Search(RecipeData[] recs, int[] cur, int i, bool[] used, ref int bestScore, TraySlot[] best)
        {
            if (i == recs.Length)
            {
                int score = 0;
                for (int k = 0; k < recs.Length; k++)
                {
                    if (cur[k] < 0) { score -= 100; continue; }   // overflow (more meals than slots, or nothing fits)
                    var s = Slots[cur[k]];
                    if (s != TraySlot.Free) score += 10 + (recs[k] != null && !recs[k].always ? 1 : 0);   // typed slots filled, by real meals first
                }
                if (score > bestScore) { bestScore = score; for (int k = 0; k < recs.Length; k++) best[k] = cur[k] < 0 ? TraySlot.Free : Slots[cur[k]]; }
                return;
            }
            bool any = false;
            for (int s = 0; s < Slots.Length; s++)
            {
                if (used[s] || !Fits(recs[i], Slots[s])) continue;
                any = true;
                used[s] = true; cur[i] = s;
                Search(recs, cur, i + 1, used, ref bestScore, best);
                used[s] = false;
            }
            if (!any) { cur[i] = -1; Search(recs, cur, i + 1, used, ref bestScore, best); }
        }

        /// The typed slots (Attack / Health) nothing fills yet.
        public static List<TraySlot> Missing(GameContent c, IList<string> recipeIds)
        {
            var got = Assign(c, recipeIds);
            var missing = new List<TraySlot> { TraySlot.Attack, TraySlot.Health };
            foreach (var s in got) missing.Remove(s);
            return missing;
        }

        /// Would the tray still be completable after adding this recipe? Null if yes, else why not.
        public static string CanAdd(GameContent c, IList<string> recipeIds, string recipeId)
        {
            int size = c.tuning.traySize;
            int n = recipeIds != null ? recipeIds.Count : 0;
            if (n >= size) return "The tray is full";
            var next = new List<string>();
            if (recipeIds != null) next.AddRange(recipeIds);
            next.Add(recipeId);
            var missing = Missing(c, next);
            if (missing.Count > size - next.Count)
                return "Your tray needs " + Need(missing);
            return null;
        }

        /// "an 🗡️ Attack meal" / "a ❤️ Health meal" / "an 🗡️ Attack and a ❤️ Health meal".
        public static string Need(List<TraySlot> missing)
        {
            if (missing == null || missing.Count == 0) return "";
            if (missing.Count == 1) return (missing[0] == TraySlot.Attack ? "an " : "a ") + SlotEmoji(missing[0]) + " " + SlotName(missing[0]) + " meal";
            return "an 🗡️ Attack and a ❤️ Health meal";
        }

        /// The buffs a recipe gives in a slot, before quality, helpings and class bonuses. Camp Bread in the Attack slot
        /// gives +1 attack instead of its own buffs.
        public static List<StatMod> RawBuffs(GameContent c, RecipeData r, TraySlot slot)
        {
            if (r == null) return new List<StatMod>();
            if (r.always && slot == TraySlot.Attack) return new List<StatMod> { new StatMod { stat = BuffStat.Attack, value = c.tuning.breadAttack } };
            return r.buffs ?? new List<StatMod>();
        }
    }
}
