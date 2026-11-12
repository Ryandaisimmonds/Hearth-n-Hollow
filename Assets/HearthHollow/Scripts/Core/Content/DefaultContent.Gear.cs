// Hearth & Hollow — default content: gear tracks, trinkets, kitchen upgrades and the Lamp-maker.
// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).
// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// Workshop gear tracks, trinkets, kitchen station / pantry levels, and the Lamp-maker's stages.
        static void AddGear(GameContent c)
        {
            // ── weaponTiers (5) ──
            c.weaponTiers.Add(new GearTier { name = "Wooden Spoon", value = 4f });
            c.weaponTiers.Add(new GearTier { tier = 1, name = "Iron Ladle", price = 40, value = 5f });
            c.weaponTiers.Add(new GearTier { tier = 2, name = "Carving Knife", price = 90, value = 6f });
            c.weaponTiers.Add(new GearTier { tier = 3, name = "Camp Cleaver", price = 160, value = 7.5f });
            c.weaponTiers.Add(new GearTier { tier = 4, name = "Drakebone Skillet", price = 260, value = 9f });
            // ── armourTiers (5) ──
            c.armourTiers.Add(new GearTier { name = "Apron" });
            c.armourTiers.Add(new GearTier { tier = 1, name = "Quilted Apron", price = 40, value = 1f });
            c.armourTiers.Add(new GearTier { tier = 2, name = "Leather Smock", price = 90, value = 2f });
            c.armourTiers.Add(new GearTier { tier = 3, name = "Cast-Iron Vest", price = 160, value = 3f });
            c.armourTiers.Add(new GearTier { tier = 4, name = "Hearthguard Mail", price = 260, value = 4f });
            // ── headTiers (5) ──
            c.headTiers.Add(new GearTier { name = "Cloth Cap" });
            c.headTiers.Add(new GearTier { tier = 1, name = "Leather Cap", price = 35, value = 2f });
            c.headTiers.Add(new GearTier { tier = 2, name = "Iron Pot Helm", price = 80, value = 4f });
            c.headTiers.Add(new GearTier { tier = 3, name = "Horned Helm", price = 140, value = 6f });
            c.headTiers.Add(new GearTier { tier = 4, name = "Hearthguard Helm", price = 230, value = 8f });
            // ── bootsTiers (5) ──
            c.bootsTiers.Add(new GearTier { name = "Sandals" });
            c.bootsTiers.Add(new GearTier { tier = 1, name = "Walking Boots", price = 35, value = 0.04f });
            c.bootsTiers.Add(new GearTier { tier = 2, name = "Trail Boots", price = 80, value = 0.07f });
            c.bootsTiers.Add(new GearTier { tier = 3, name = "Ranger's Boots", price = 140, value = 0.1f });
            c.bootsTiers.Add(new GearTier { tier = 4, name = "Windstep Boots", price = 230, value = 0.14f });
            // ── bagTiers (7) ──
            c.bagTiers.Add(new GearTier { name = "Belt Pouch", value = 6f });
            c.bagTiers.Add(new GearTier { tier = 1, name = "Satchel", price = 40, value = 9f });
            c.bagTiers.Add(new GearTier { tier = 2, name = "Forager's Basket", price = 90, value = 12f });
            c.bagTiers.Add(new GearTier { tier = 3, name = "Rucksack", price = 160, value = 16f });
            c.bagTiers.Add(new GearTier { tier = 4, name = "Backpack", price = 260, value = 20f });
            c.bagTiers.Add(new GearTier { tier = 5, name = "Expedition Pack", price = 400, value = 25f });
            c.bagTiers.Add(new GearTier { tier = 6, name = "Hollow-Walker's Pack", price = 600, value = 30f });
            // ── lanternTiers (5) ──
            c.lanternTiers.Add(new GearTier { name = "Tallow Stub" });
            c.lanternTiers.Add(new GearTier { tier = 1, name = "Tin Lantern", price = 40, value = 0.15f });
            c.lanternTiers.Add(new GearTier { tier = 2, name = "Brass Lantern", price = 90, value = 0.3f });
            c.lanternTiers.Add(new GearTier { tier = 3, name = "Glass Lantern", price = 160, value = 0.45f });
            c.lanternTiers.Add(new GearTier { tier = 4, name = "Hearthglow Lantern", price = 260, value = 0.6f });
            // ── trinkets (28) ──
            c.trinkets.Add(new TrinketData
            {
                id = "clover",
                name = "Lucky Clover Charm",
                emoji = "🍀",
                description = "A pressed four-leaf clover in resin.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.05f } },
                price = 60,
                inWorkshop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "purse",
                name = "Copper Coin Purse",
                emoji = "👛",
                description = "Coins seem to find their way into it.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.15f } },
                price = 60,
                inWorkshop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "pendant",
                name = "Hearthstone Pendant",
                emoji = "📿",
                description = "Warm to the touch, like the camp fire.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MaxHp, value = 6f } },
                price = 80,
                inWorkshop = true,
                shopChapter = 2,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "twine",
                name = "Forager's Twine",
                emoji = "🧶",
                description = "Ties bundles tight — nothing slips away.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.GatherZonePct, value = 0.2f } },
                price = 70,
                inWorkshop = true,
                shopChapter = 2,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "feather",
                name = "Feather Charm",
                emoji = "🪶",
                description = "Light on your feet.",
                perk = TrinketPerk.MoveSpeed,
                perkValue = 0.16f,
                price = 120,
                inWorkshop = true,
                shopChapter = 3,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "whetstone",
                name = "Pocket Whetstone",
                emoji = "🪨",
                description = "A quick edge before every swing.",
                perk = TrinketPerk.AttackPower,
                perkValue = 0.08f,
                price = 150,
                inWorkshop = true,
                shopChapter = 4,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "bookmark",
                name = "Scholar's Bookmark",
                emoji = "🔖",
                description = "Every fight is a lesson.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.15f } },
                price = 120,
                inWorkshop = true,
                shopChapter = 4,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "hourglass",
                name = "Hourglass Bead",
                emoji = "⏳",
                description = "Time runs a little kinder.",
                perk = TrinketPerk.SkillCooldown,
                perkValue = 0.1f,
                price = 200,
                inWorkshop = true,
                shopChapter = 5,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "rabbitfoot",
                name = "Rabbit's Foot",
                emoji = "🐇",
                description = "From the fastest hare on the run.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.04f } },
                perk = TrinketPerk.DashCooldown,
                perkValue = 0.15f,
                fromRouteId = "harerun",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "fireflyjar",
                name = "Firefly Jar",
                emoji = "🫙",
                description = "A few glade fireflies, happy in a jar.",
                perk = TrinketPerk.HazardWard,
                perkValue = 1f,
                wardHazard = Hazard.Darkness,
                fromRouteId = "firefly",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "mirecharm",
                name = "Mire Charm",
                emoji = "🍄",
                description = "Bitter spores the poison doesn't like.",
                perk = TrinketPerk.HazardWard,
                perkValue = 1f,
                wardHazard = Hazard.Poison,
                fromRouteId = "spore",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "glowbrooch",
                name = "Glowcap Brooch",
                emoji = "💡",
                description = "Hums when you use a skill.",
                perk = TrinketPerk.SkillPower,
                perkValue = 0.1f,
                fromRouteId = "glowcap",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "signet",
                name = "Warden's Signet",
                emoji = "💍",
                description = "The old watch's seal.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.Armour, value = 1f }, new StatMod { stat = BuffStat.MaxHp, value = 4f } },
                fromRouteId = "watchtower",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "thunderstone",
                name = "Thunder Stone",
                emoji = "⚡",
                description = "Crackles when you strike.",
                buffs = new List<StatMod> { new StatMod { value = 0.12f } },
                fromRouteId = "thunder",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "thornband",
                name = "Thornsilk Band",
                emoji = "🌿",
                description = "Woven in the canopy.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.05f } },
                perk = TrinketPerk.MoveSpeed,
                perkValue = 0.2f,
                fromRouteId = "canopy",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "honeylocket",
                name = "Honey Locket",
                emoji = "🍯",
                description = "A sweet mouthful after every fight.",
                perk = TrinketPerk.HealOnKill,
                perkValue = 1f,
                price = 100,
                inWorkshop = true,
                fromRouteId = "hive",
                slice = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "grannyspoon",
                name = "Granny's Spoon",
                emoji = "🥄",
                description = "Found in the Forgotten Larder.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.3f } },
                fromRouteId = "larder",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "frostamulet",
                name = "Frost Ward Amulet",
                emoji = "❄️",
                description = "Never quite cold.",
                perk = TrinketPerk.HazardWard,
                perkValue = 1f,
                wardHazard = Hazard.Cold,
                fromRouteId = "frosthollow",
            });
            c.trinkets.Add(new TrinketData
            {
                id = "emberward",
                name = "Ember Ward Charm",
                emoji = "🔥",
                description = "A coal that never burns you.",
                perk = TrinketPerk.HazardWard,
                perkValue = 1f,
                wardHazard = Hazard.Heat,
                price = 180,
                merchant = true,
                drop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "slimeheart",
                name = "Slime Heart",
                emoji = "🟢",
                description = "Wobbly, but it keeps you going.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MaxHp, value = 8f } },
                drop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "wolffang",
                name = "Wolf Fang Necklace",
                emoji = "🐺",
                description = "Trophy of a hard fight.",
                buffs = new List<StatMod> { new StatMod { value = 0.1f }, new StatMod { stat = BuffStat.CritPct, value = 0.03f } },
                drop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "dragonscale",
                name = "Dragon Scale",
                emoji = "🐉",
                description = "Still warm.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.Armour, value = 2f } },
                drop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "fetish",
                name = "Shaman's Fetish",
                emoji = "🪬",
                description = "Mutters charms on its own.",
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.05f } },
                perk = TrinketPerk.SkillCooldown,
                perkValue = 0.1f,
                drop = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "compass",
                name = "Traveller's Compass",
                emoji = "🧭",
                description = "Always points to the good stuff.",
                buffs = new List<StatMod>
                {
                    new StatMod { stat = BuffStat.CoinPct, value = 0.1f },
                    new StatMod { stat = BuffStat.GatherZonePct, value = 0.1f },
                },
                price = 150,
                merchant = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "spicetin",
                name = "Merchant's Spice Tin",
                emoji = "🫚",
                description = "Smells of faraway markets.",
                buffs = new List<StatMod> { new StatMod { value = 0.06f }, new StatMod { stat = BuffStat.MaxHp, value = 3f } },
                price = 130,
                merchant = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "ember_locket",
                name = "Ember Locket",
                emoji = "🔥",
                description = "Every kill takes 3 s off Flare's cooldown.",
                perk = TrinketPerk.FlareOnKill,
                perkValue = 3f,
                price = 60,
                inWorkshop = true,
                slice = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "magpie_spoon",
                name = "Magpie Spoon",
                emoji = "🥄",
                description = "Monsters drop ingredients a lot more often.",
                perk = TrinketPerk.LootOnKill,
                perkValue = 0.2f,
                price = 90,
                inWorkshop = true,
                slice = true,
            });
            c.trinkets.Add(new TrinketData
            {
                id = "pocket_hearth",
                name = "Pocket Hearth",
                emoji = "🕯️",
                description = "Once a run, a blow that would fell you leaves you on 1 HP instead.",
                perk = TrinketPerk.SecondWind,
                perkValue = 1f,
                price = 140,
                inWorkshop = true,
                slice = true,
            });
            // ── stationTiers (5) ──
            c.stationTiers.Add(new KitchenTier { level = 1, name = "Camp fire" });
            c.stationTiers.Add(new KitchenTier
            {
                level = 2,
                name = "Stone ring",
                price = 50,
                materials = new List<IngredientCost> { new IngredientCost { ingredientId = "timber", count = 4 } },
            });
            c.stationTiers.Add(new KitchenTier
            {
                level = 3,
                name = "Fieldstone hearth",
                price = 100,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "fieldstone", count = 4 },
                    new IngredientCost { ingredientId = "timber", count = 2 },
                },
            });
            c.stationTiers.Add(new KitchenTier
            {
                level = 4,
                name = "Clay oven-hood",
                price = 180,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "clay", count = 4 },
                    new IngredientCost { ingredientId = "iron_scrap", count = 3 },
                },
            });
            c.stationTiers.Add(new KitchenTier
            {
                level = 5,
                name = "Ember forge-hearth",
                price = 300,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "ember_coal", count = 4 },
                    new IngredientCost { ingredientId = "iron_scrap", count = 4 },
                },
            });
            // ── pantryTiers (4) ──
            c.pantryTiers.Add(new KitchenTier { level = 1, name = "Crate shelf", value = 6f });
            c.pantryTiers.Add(new KitchenTier
            {
                level = 2,
                name = "Plank shelves",
                price = 40,
                materials = new List<IngredientCost> { new IngredientCost { ingredientId = "timber", count = 3 } },
                value = 8f,
            });
            c.pantryTiers.Add(new KitchenTier
            {
                level = 3,
                name = "Stone larder",
                price = 90,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "fieldstone", count = 3 },
                    new IngredientCost { ingredientId = "clay", count = 2 },
                },
                value = 10f,
            });
            c.pantryTiers.Add(new KitchenTier
            {
                level = 4,
                name = "Cool cellar",
                price = 160,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "iron_scrap", count = 3 },
                    new IngredientCost { ingredientId = "ember_coal", count = 2 },
                },
                value = 12f,
            });
            // ── lampMaker (3) ──
            c.lampMaker.Add(new LampStage
            {
                description = "A stall and a lamp-maker who trims your wick properly. Your light reaches a little further, and Flare a little wider.",
                lightReach = 0.1f,
                flareRadius = 0.1f,
                level = 1,
                name = "Lamp-maker's stall",
                price = 30,
                materials = new List<IngredientCost> { new IngredientCost { ingredientId = "timber", count = 6 } },
            });
            c.lampMaker.Add(new LampStage
            {
                description = "A proper workshop with a coal kiln for glass. Brighter carried light, and Flare comes back quicker.",
                lightReach = 0.2f,
                flareRadius = 0.2f,
                flareCooldownCut = 3f,
                level = 2,
                name = "Lamp-maker's workshop",
                price = 80,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "timber", count = 10 },
                    new IngredientCost { ingredientId = "ember_coal", count = 4 },
                },
            });
            c.lampMaker.Add(new LampStage
            {
                description = "A tall workshop with a lamp on top you can see from the gate. The best light in the valley.",
                lightReach = 0.35f,
                flareRadius = 0.35f,
                flareCooldownCut = 6f,
                level = 3,
                name = "Lighthouse workshop",
                price = 160,
                materials = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "timber", count = 12 },
                    new IngredientCost { ingredientId = "ember_coal", count = 8 },
                },
            });
        }
    }
}
