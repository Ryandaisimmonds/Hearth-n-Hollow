// Hearth & Hollow — kitchen upgrades (contentVersion 13). Pure C#, no UnityEngine.
// Each station (Pot, BBQ, Sandwich) has levels 1..5: a level unlocks 1–2 more dishes and adds to that station's
// better-meal (jackpot) chance. The meal pantry's shelves grow 6 → 8 → 10 → 12. Upgrades cost coins plus building
// materials (Timber, Fieldstone, River Clay, Iron Scrap, Ember Coal) that monsters drop in the dungeons.
// Everyone starts at level 1. Built as data so a future town can put each upgrade inside a building.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Kitchen
    {
        static void Ensure(Profile p)
        {
            if (p.stationTier == null) p.stationTier = new List<int>();
            int n = Enum.GetValues(typeof(Station)).Length;
            while (p.stationTier.Count < n) p.stationTier.Add(0);
        }

        public static int MaxLevel(GameContent c) { return Math.Max(1, c.stationTiers != null ? c.stationTiers.Count : 1); }

        /// 1..MaxLevel
        public static int Level(GameContent c, Profile p, Station st)
        {
            Ensure(p);
            return Math.Max(1, Math.Min(MaxLevel(c), p.stationTier[(int)st] + 1));
        }

        public static KitchenTier NextStation(GameContent c, Profile p, Station st)
        {
            int lv = Level(c, p, st);
            return c.stationTiers != null && lv < c.stationTiers.Count ? c.stationTiers[lv] : null;
        }

        public static int PantrySlots(GameContent c, Profile p)
        {
            if (c.pantryTiers == null || c.pantryTiers.Count == 0) return Math.Max(1, c.tuning.mealPantrySlots);
            return Math.Max(1, (int)c.pantryTiers[Math.Max(0, Math.Min(p.pantryTier, c.pantryTiers.Count - 1))].value);
        }

        public static KitchenTier NextPantry(GameContent c, Profile p)
        {
            return c.pantryTiers != null && p.pantryTier + 1 < c.pantryTiers.Count ? c.pantryTiers[p.pantryTier + 1] : null;
        }

        /// The station's chance that a cooked meal lands one grade higher.
        public static float Jackpot(GameContent c, Profile p, Station st)
        {
            float lv = p != null ? Level(c, p, st) : 1;
            return Math.Max(0f, c.tuning.jackpotChance + c.tuning.stationJackpotPerLevel * (lv - 1));
        }

        public static bool Unlocked(GameContent c, Profile p, RecipeData r)
        {
            return r != null && (r.always || Level(c, p, r.station) >= Math.Max(1, r.stationLevel));
        }

        public static string LockText(RecipeData r) { return "🔒 " + r.station + " level " + r.stationLevel; }

        /// Dishes a station level unlocks.
        public static List<RecipeData> DishesAt(GameContent c, Station st, int level)
        {
            return c.recipes.FindAll(r => !r.always && r.station == st && Math.Max(1, r.stationLevel) == level);
        }

        public static bool CanAfford(GameContent c, Profile p, KitchenTier t, out string reason)
        {
            if (t == null) { reason = "Already at the top level"; return false; }
            if (p.coins < t.price) { reason = "Not enough coins"; return false; }
            if (t.materials != null)
                foreach (var m in t.materials)
                    if (Economy.PantryCount(p, m.ingredientId) < m.count)
                    {
                        var ing = c.Ingredient(m.ingredientId);
                        reason = "Not enough " + (ing != null ? ing.name : m.ingredientId) + " — find it in the dungeons";
                        return false;
                    }
            reason = null; return true;
        }

        static void Pay(Profile p, KitchenTier t)
        {
            p.coins -= t.price;
            if (t.materials != null) foreach (var m in t.materials) Economy.RemoveFromPantry(p, m.ingredientId, m.count);
        }

        public static bool UpgradeStation(GameContent c, Profile p, Station st, out string reason)
        {
            var next = NextStation(c, p, st);
            if (!CanAfford(c, p, next, out reason)) return false;
            Pay(p, next);
            Ensure(p);
            p.stationTier[(int)st]++;
            return true;
        }

        public static bool UpgradePantry(GameContent c, Profile p, out string reason)
        {
            var next = NextPantry(c, p);
            if (!CanAfford(c, p, next, out reason)) return false;
            Pay(p, next);
            p.pantryTier++;
            return true;
        }

        public static bool IsMaterial(GameContent c, string id) { var i = c.Ingredient(id); return i != null && i.material; }
    }
}
