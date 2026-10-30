// Hearth & Hollow — Workshop content (contentVersion 7): head and boots tracks, and trinkets. Pure C#.
// Kept small on purpose — meals are the main source of power. Edit freely in the content asset.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static void BuildGear7(GameContent c)
        {
            if (c.headTiers == null) c.headTiers = new List<GearTier>();
            if (c.bootsTiers == null) c.bootsTiers = new List<GearTier>();
            if (c.trinkets == null) c.trinkets = new List<TrinketData>();

            if (c.headTiers.Count == 0)   // value = max HP
            {
                Gear(c.headTiers, 0, "Cloth Cap", 0, 0f);
                Gear(c.headTiers, 1, "Leather Cap", 35, 2f);
                Gear(c.headTiers, 2, "Iron Pot Helm", 80, 4f);
                Gear(c.headTiers, 3, "Horned Helm", 140, 6f);
                Gear(c.headTiers, 4, "Hearthguard Helm", 230, 8f);
            }
            if (c.bootsTiers.Count == 0)   // value = move speed fraction
            {
                Gear(c.bootsTiers, 0, "Sandals", 0, 0f);
                Gear(c.bootsTiers, 1, "Walking Boots", 35, 0.04f);
                Gear(c.bootsTiers, 2, "Trail Boots", 80, 0.07f);
                Gear(c.bootsTiers, 3, "Ranger's Boots", 140, 0.10f);
                Gear(c.bootsTiers, 4, "Windstep Boots", 230, 0.14f);
            }
            if (c.trinkets.Count > 0) return;

            // ── Workshop stock (unlocks as the story moves on) ──
            Shop(c, "clover", "Lucky Clover Charm", "🍀", "A pressed four-leaf clover in resin.", 60, 1, Mod(BuffStat.CritPct, 0.05f));
            Shop(c, "purse", "Copper Coin Purse", "👛", "Coins seem to find their way into it.", 60, 1, Mod(BuffStat.CoinPct, 0.15f));
            Shop(c, "pendant", "Hearthstone Pendant", "📿", "Warm to the touch, like the camp fire.", 80, 2, Mod(BuffStat.MaxHp, 6f));
            Shop(c, "twine", "Forager's Twine", "🧶", "Ties bundles tight — nothing slips away.", 70, 2, Mod(BuffStat.GatherZonePct, 0.20f));
            Perk(Shop(c, "feather", "Feather Charm", "🪶", "Light on your feet.", 120, 3), TrinketPerk.MoveSpeed, 0.08f);
            Perk(Shop(c, "whetstone", "Pocket Whetstone", "🪨", "A quick edge before every swing.", 150, 4), TrinketPerk.AttackPower, 0.08f);
            Shop(c, "bookmark", "Scholar's Bookmark", "🔖", "Every fight is a lesson.", 120, 4, Mod(BuffStat.XpPct, 0.15f));
            Perk(Shop(c, "hourglass", "Hourglass Bead", "⏳", "Time runs a little kinder.", 200, 5), TrinketPerk.SkillCooldown, 0.10f);

            // ── first clears of the side dungeons ──
            Perk(Clear(c, "rabbitfoot", "Rabbit's Foot", "🐇", "From the fastest hare on the run.", "harerun", Mod(BuffStat.CritPct, 0.04f)), TrinketPerk.DashCooldown, 0.15f);
            Ward(Clear(c, "fireflyjar", "Firefly Jar", "🫙", "A few glade fireflies, happy in a jar.", "firefly"), Hazard.Darkness);
            Ward(Clear(c, "mirecharm", "Mire Charm", "🍄", "Bitter spores the poison doesn't like.", "spore"), Hazard.Poison);
            Perk(Clear(c, "glowbrooch", "Glowcap Brooch", "💡", "Hums when you use a skill.", "glowcap"), TrinketPerk.SkillPower, 0.10f);
            Clear(c, "signet", "Warden's Signet", "💍", "The old watch's seal.", "watchtower", Mod(BuffStat.Armour, 1f), Mod(BuffStat.MaxHp, 4f));
            Clear(c, "thunderstone", "Thunder Stone", "⚡", "Crackles when you strike.", "thunder", Mod(BuffStat.DamagePct, 0.12f));
            Perk(Clear(c, "thornband", "Thornsilk Band", "🌿", "Woven in the canopy.", "canopy", Mod(BuffStat.CritPct, 0.05f)), TrinketPerk.MoveSpeed, 0.10f);
            Perk(Clear(c, "honeylocket", "Honey Locket", "🍯", "A sweet mouthful after every fight.", "hive"), TrinketPerk.HealOnKill, 1f);
            Clear(c, "grannyspoon", "Granny's Spoon", "🥄", "Found in the Forgotten Larder.", "larder", Mod(BuffStat.CoinPct, 0.20f), Mod(BuffStat.XpPct, 0.10f));
            Ward(Clear(c, "frostamulet", "Frost Ward Amulet", "❄️", "Never quite cold.", "frosthollow"), Hazard.Cold);

            // ── rare boss / elite drops and the travelling merchant ──
            var ember = Ward(New(c, "emberward", "Ember Ward Charm", "🔥", "A coal that never burns you."), Hazard.Heat); ember.drop = true; ember.merchant = true; ember.price = 180;
            New(c, "slimeheart", "Slime Heart", "🟢", "Wobbly, but it keeps you going.", Mod(BuffStat.MaxHp, 8f)).drop = true;
            New(c, "wolffang", "Wolf Fang Necklace", "🐺", "Trophy of a hard fight.", Mod(BuffStat.DamagePct, 0.10f), Mod(BuffStat.CritPct, 0.03f)).drop = true;
            New(c, "dragonscale", "Dragon Scale", "🐉", "Still warm.", Mod(BuffStat.Armour, 2f)).drop = true;
            var fetish = Perk(New(c, "fetish", "Shaman's Fetish", "🪬", "Mutters charms on its own.", Mod(BuffStat.XpPct, 0.05f)), TrinketPerk.SkillCooldown, 0.10f); fetish.drop = true;
            var compass = New(c, "compass", "Traveller's Compass", "🧭", "Always points to the good stuff.", Mod(BuffStat.CoinPct, 0.10f), Mod(BuffStat.GatherZonePct, 0.10f));
            compass.merchant = true; compass.price = 150;
            var spices = New(c, "spicetin", "Merchant's Spice Tin", "🫚", "Smells of faraway markets.", Mod(BuffStat.DamagePct, 0.06f), Mod(BuffStat.MaxHp, 3f));
            spices.merchant = true; spices.price = 130;
        }

        /// Meal rarity (contentVersion 8). Hand-picked for the default meals; anything else goes by its rarest ingredient
        /// (tier 0 Common … tier 3 Epic, Legendary if it also has 3+ bonuses). Only sets meals still at Common.
        static readonly string[][] RarityTable =
        {
            new[] { "bread", "herb_stew", "berry_porridge", "mushroom_skewer", "herb_butty", "bramble_jam" },
            new[] { "marsh_chowder", "grilled_fish", "marsh_butty", "nettle_soup", "nettle_skewer", "nettle_wrap", "mint_soup", "mint_fish" },
            new[] { "truffle_risotto", "ember_chili", "lantern_soup", "firecracker_skewer", "mint_butty", "glowcap_broth", "glowcap_kebab", "ginger_stew" },
            new[] { "ember_steak", "honey_ribs", "glow_egg_sandwich", "ginger_skewer", "glowcap_sandwich" },
            new[] { "ember_toastie", "ginger_toastie", "frostberry_crumble" },
        };

        static void AssignRarity(GameContent c)
        {
            foreach (var r in c.recipes)
            {
                if (r.rarity != Rarity.Common) continue;
                int found = -1;
                for (int k = 0; k < RarityTable.Length && found < 0; k++)
                    if (System.Array.IndexOf(RarityTable[k], r.id) >= 0) found = k;
                if (found < 0)
                {
                    int tier = 0;
                    foreach (var ic in r.ingredients) { var ing = c.Ingredient(ic.ingredientId); if (ing != null && ing.tier > tier) tier = ing.tier; }
                    found = tier >= 3 && r.buffs.Count >= 3 ? 4 : System.Math.Min(3, tier);
                }
                r.rarity = (Rarity)found;
            }
        }

        static TrinketData New(GameContent c, string id, string name, string emoji, string desc, params StatMod[] mods)
        {
            var t = new TrinketData { id = id, name = name, emoji = emoji, description = desc };
            if (mods != null) t.buffs.AddRange(mods);
            c.trinkets.Add(t);
            return t;
        }

        static TrinketData Shop(GameContent c, string id, string name, string emoji, string desc, int price, int chapter, params StatMod[] mods)
        {
            var t = New(c, id, name, emoji, desc, mods);
            t.inWorkshop = true; t.price = price; t.shopChapter = chapter;
            return t;
        }

        static TrinketData Clear(GameContent c, string id, string name, string emoji, string desc, string routeId, params StatMod[] mods)
        {
            var t = New(c, id, name, emoji, desc, mods);
            t.fromRouteId = routeId;
            return t;
        }

        static TrinketData Perk(TrinketData t, TrinketPerk perk, float value) { t.perk = perk; t.perkValue = value; return t; }
        static TrinketData Ward(TrinketData t, Hazard h) { t.perk = TrinketPerk.HazardWard; t.perkValue = 1f; t.wardHazard = h; return t; }
    }
}
