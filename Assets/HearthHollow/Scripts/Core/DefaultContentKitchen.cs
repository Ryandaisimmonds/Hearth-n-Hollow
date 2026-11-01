// Hearth & Hollow — kitchen upgrades content (contentVersion 13). Pure C#.
// Station levels 1..5 (every station uses the same costs) unlock 1–2 dishes each and raise that station's better-meal
// chance (tuning.stationJackpotPerLevel). Meal-pantry shelves 6 → 8 → 10 → 12. Costs are coins + building materials,
// which monsters drop: easy routes give Timber/Fieldstone, harder ones River Clay/Iron Scrap, the hardest Ember Coal.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static readonly string[] MaterialIds = { "timber", "fieldstone", "clay", "iron_scrap", "ember_coal" };

        static void BuildKitchen13(GameContent c)
        {
            // ── building materials ──
            Mat(c, "timber", "Timber", "🪵", 0);
            Mat(c, "fieldstone", "Fieldstone", "🪨", 1);
            Mat(c, "clay", "River Clay", "🏺", 2);
            Mat(c, "iron_scrap", "Iron Scrap", "🔩", 3);
            Mat(c, "ember_coal", "Ember Coal", "⚫", 3);

            // ── which routes drop which: by difficulty (its own tier + the one below) ──
            foreach (var r in c.routes)
            {
                if (r.materialIds == null) r.materialIds = new List<string>();
                if (r.materialIds.Count > 0) continue;
                int t = Math.Max(0, Math.Min(MaterialIds.Length - 1, r.baseDifficulty));
                r.materialIds.Add(MaterialIds[t]);
                if (t > 0) r.materialIds.Add(MaterialIds[t - 1]);
            }

            // ── which station level unlocks each dish (1–2 per level) ──
            Lv(c, 1, "herb_stew", "berry_porridge", "mushroom_skewer", "grilled_fish", "herb_butty", "bramble_jam", "bread");
            Lv(c, 2, "marsh_chowder", "truffle_caps", "marsh_butty");
            Lv(c, 3, "nettle_soup", "truffle_risotto", "glowcap_kebab", "firecracker_skewer", "trail_wrap");
            Lv(c, 4, "lantern_soup", "honey_ribs", "glow_egg_sandwich");
            Lv(c, 5, "drake_hotpot", "ember_steak", "frostberry_toast");

            // ── station levels (same for Pot, BBQ and Sandwich) ──
            if (c.stationTiers == null) c.stationTiers = new List<KitchenTier>();
            if (c.stationTiers.Count == 0)
            {
                Tier(c.stationTiers, 1, "Camp fire", 0, 0);
                Tier(c.stationTiers, 2, "Stone ring", 50, 0, Cost("timber", 4));
                Tier(c.stationTiers, 3, "Fieldstone hearth", 100, 0, Cost("fieldstone", 4), Cost("timber", 2));
                Tier(c.stationTiers, 4, "Clay oven-hood", 180, 0, Cost("clay", 4), Cost("iron_scrap", 3));
                Tier(c.stationTiers, 5, "Ember forge-hearth", 300, 0, Cost("ember_coal", 4), Cost("iron_scrap", 4));
            }

            // ── meal-pantry shelves ──
            if (c.pantryTiers == null) c.pantryTiers = new List<KitchenTier>();
            if (c.pantryTiers.Count == 0)
            {
                Tier(c.pantryTiers, 1, "Crate shelf", 0, 6);
                Tier(c.pantryTiers, 2, "Plank shelves", 40, 8, Cost("timber", 3));
                Tier(c.pantryTiers, 3, "Stone larder", 90, 10, Cost("fieldstone", 3), Cost("clay", 2));
                Tier(c.pantryTiers, 4, "Cool cellar", 160, 12, Cost("iron_scrap", 3), Cost("ember_coal", 2));
            }
        }

        /// contentVersion 14: the placeholder drop numbers become the real ones (only where still at the placeholder values;
        /// the new fields arrive with their defaults).
        static void RiskReward14(GameContent c)
        {
            var t = c.tuning;
            if (t.dropUncommon == 0.12f) t.dropUncommon = 0.10f;
            if (t.dropUncommonPerFloor == 0.12f) t.dropUncommonPerFloor = 0.15f;
            if (t.dropRare == 0.02f) t.dropRare = 0f;
            if (t.dropRarePerFloor == 0.06f) t.dropRarePerFloor = 0.08f;
            if (t.dropBossRareBonus == 0.30f) t.dropBossRareBonus = 0.35f;
            if (t.materialDropChance == 0.2f) t.materialDropChance = 0.15f;
        }

        static void Mat(GameContent c, string id, string name, string emoji, int tier)
        {
            var ing = c.Ingredient(id);
            if (ing == null) { ing = new IngredientData { id = id, name = name, emoji = emoji, tier = tier }; c.ingredients.Add(ing); }
            ing.material = true;
        }

        static void Lv(GameContent c, int level, params string[] ids)
        {
            foreach (var id in ids) { var r = c.Recipe(id); if (r != null) r.stationLevel = level; }
        }

        static void Tier(List<KitchenTier> list, int level, string name, int price, float value, params IngredientCost[] mats)
        {
            list.Add(new KitchenTier { level = level, name = name, price = price, value = value, materials = new List<IngredientCost>(mats) });
        }
    }
}
