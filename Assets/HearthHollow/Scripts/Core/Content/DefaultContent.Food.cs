// Hearth & Hollow — default content: ingredients, recipes and meal synergies.
// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).
// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// Every ingredient (and building material), every dish, and the tray synergies.
        static void AddFood(GameContent c)
        {
            // ── ingredients (25) ──
            c.ingredients.Add(new IngredientData { id = "herbs", name = "Wild Herbs", emoji = "🌿", guard = Element.Nature });
            c.ingredients.Add(new IngredientData { id = "wheat", name = "Meadow Wheat", emoji = "🌾", guard = Element.Water });
            c.ingredients.Add(new IngredientData { id = "berries", name = "Bramble Berries", emoji = "🫐" });
            c.ingredients.Add(new IngredientData { id = "mushroom", name = "Forest Mushroom", emoji = "🍄", tier = 1 });
            c.ingredients.Add(new IngredientData { id = "salt", name = "Marsh Salt", emoji = "🧂", tier = 1, infuse = Element.Water });
            c.ingredients.Add(new IngredientData { id = "fish", name = "Reed Fish", emoji = "🐟", tier = 1, infuse = Element.Water });
            c.ingredients.Add(new IngredientData { id = "cave_egg", name = "Glow Egg", emoji = "🥚", tier = 2 });
            c.ingredients.Add(new IngredientData { id = "truffle", name = "Cave Truffle", emoji = "🌰", tier = 2 });
            c.ingredients.Add(new IngredientData { id = "chili", name = "Ridge Chili", emoji = "🌶️", tier = 2, infuse = Element.Fire });
            c.ingredients.Add(new IngredientData { id = "garlic", name = "Smoky Garlic", emoji = "🧄", tier = 2, guard = Element.Nature });
            c.ingredients.Add(new IngredientData { id = "ember_meat", name = "Ember Haunch", emoji = "🍖", tier = 3 });
            c.ingredients.Add(new IngredientData { id = "fire_honey", name = "Fire Honey", emoji = "🍯", tier = 3, infuse = Element.Fire });
            c.ingredients.Add(new IngredientData
            {
                id = "nettle",
                name = "Stinging Nettle",
                emoji = "🌱",
                tier = 1,
                accelGuard = Hazard.Poison,
                infuse = Element.Nature,
            });
            c.ingredients.Add(new IngredientData { id = "mint", name = "Glade Mint", emoji = "🍃", tier = 1, accelGuard = Hazard.Heat, guard = Element.Fire });
            c.ingredients.Add(new IngredientData { id = "ginger", name = "Snow Ginger", emoji = "🫚", tier = 2, accelGuard = Hazard.Cold, guard = Element.Frost });
            c.ingredients.Add(new IngredientData { id = "frost_berry", name = "Frostberry", emoji = "🍇", tier = 3, infuse = Element.Frost });
            c.ingredients.Add(new IngredientData { id = "glowcap", name = "Glowcap", emoji = "🍄", tier = 2, accelGuard = Hazard.Darkness, infuse = Element.Light });
            c.ingredients.Add(new IngredientData { id = "timber", name = "Timber", emoji = "🪵", material = true });
            c.ingredients.Add(new IngredientData { id = "fieldstone", name = "Fieldstone", emoji = "🪨", tier = 1, material = true });
            c.ingredients.Add(new IngredientData { id = "clay", name = "River Clay", emoji = "🏺", tier = 2, material = true });
            c.ingredients.Add(new IngredientData { id = "iron_scrap", name = "Iron Scrap", emoji = "🔩", tier = 3, material = true });
            c.ingredients.Add(new IngredientData { id = "ember_coal", name = "Ember Coal", emoji = "⚫", tier = 3, material = true });
            c.ingredients.Add(new IngredientData { id = "starmoss", name = "Starmoss", emoji = "🪴", tier = 2, deep = true });
            c.ingredients.Add(new IngredientData { id = "wild_honeycomb", name = "Wild Honeycomb", emoji = "🍯", tier = 2, deep = true });
            c.ingredients.Add(new IngredientData { id = "golden_wheat", name = "Golden Wheat", emoji = "🌾", tier = 2, deep = true });
            // ── recipes (24) ──
            c.recipes.Add(new RecipeData
            {
                id = "herb_stew",
                name = "Herb Stew",
                emoji = "🍲",
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "herbs", count = 2 },
                    new IngredientCost { ingredientId = "mushroom" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MaxHpPct, value = 0.2f } },
                tags = new List<string> { "stone", "veg" },
                description = "A thick, earthy stew that sits in your belly like a warm stone. Sturdier all day.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "herb_stew_2",
                name = "Hearty Herb Stew",
                emoji = "🍲",
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "herbs", count = 2 },
                    new IngredientCost { ingredientId = "mushroom" },
                    new IngredientCost { ingredientId = "starmoss" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MaxHpPct, value = 0.25f }, new StatMod { stat = BuffStat.Regen, value = 2f } },
                tags = new List<string> { "stone", "veg" },
                description = "A thick stew with Starmoss from deep in the Meadow.",
                tier = 2,
                upgradeOf = "herb_stew",
            });
            c.recipes.Add(new RecipeData
            {
                id = "berry_porridge",
                name = "Berry Porridge",
                emoji = "🥣",
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "wheat" }, new IngredientCost { ingredientId = "berries", count = 2 } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.Regen, value = 2f } },
                tags = new List<string> { "sweet" },
                description = "Creamy oats swirled with bramble berries. Every win feels like a second breakfast.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "marsh_chowder",
                name = "Marsh Chowder",
                emoji = "🍜",
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "fish", count = 2 }, new IngredientCost { ingredientId = "salt" } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.Armour, value = 2f } },
                tags = new List<string> { "stone", "fish" },
                description = "Salty, hearty and steaming. Tastes of foggy mornings.",
                stationLevel = 2,
            });
            c.recipes.Add(new RecipeData
            {
                id = "nettle_soup",
                name = "Nettle Soup",
                emoji = "🥬",
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "nettle", count = 2 }, new IngredientCost { ingredientId = "herbs" } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.HealOnKill, value = 2f } },
                tags = new List<string> { "veg" },
                description = "Sharp, green and cleansing. Patches you up between fights.",
                stationLevel = 3,
            });
            c.recipes.Add(new RecipeData
            {
                id = "truffle_risotto",
                name = "Truffle Risotto",
                emoji = "🍚",
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "truffle" },
                    new IngredientCost { ingredientId = "mushroom" },
                    new IngredientCost { ingredientId = "wheat" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.Armour, value = 3f } },
                tags = new List<string> { "stone", "veg" },
                description = "Slow-stirred by lantern light. Rich enough to shrug off a blow.",
                stationLevel = 3,
            });
            c.recipes.Add(new RecipeData
            {
                id = "lantern_soup",
                name = "Lantern Egg Soup",
                emoji = "🍵",
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "cave_egg" },
                    new IngredientCost { ingredientId = "ginger" },
                    new IngredientCost { ingredientId = "salt" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MaxHpPct, value = 0.3f } },
                tags = new List<string> { "stone" },
                description = "Glowing egg ribbons in a ginger broth. Fortifies body and spirit.",
                stationLevel = 4,
            });
            c.recipes.Add(new RecipeData
            {
                id = "drake_hotpot",
                name = "Drake Hotpot",
                emoji = "🍛",
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "ember_meat", count = 2 },
                    new IngredientCost { ingredientId = "ginger" },
                    new IngredientCost { ingredientId = "herbs" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.HealOnKill, value = 3f } },
                tags = new List<string> { "stone", "fire" },
                description = "Drake haunch simmered all night. The heartiest thing in the valley.",
                stationLevel = 5,
            });
            c.recipes.Add(new RecipeData
            {
                id = "grilled_fish",
                name = "Salt-Crust Fish",
                emoji = "🐟",
                station = Station.BBQ,
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "fish" }, new IngredientCost { ingredientId = "salt" } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.15f } },
                tags = new List<string> { "smoky", "fish" },
                description = "Baked in a salt shell on the coals. Sharpens the senses.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "mushroom_skewer",
                name = "Mushroom Skewer",
                emoji = "🍢",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "mushroom", count = 2 },
                    new IngredientCost { ingredientId = "herbs" },
                },
                buffs = new List<StatMod> { new StatMod { value = 0.2f } },
                tags = new List<string> { "smoky", "veg" },
                description = "Charred caps on a stick, herby and smoky.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "mushroom_skewer_2",
                name = "Honey-Glazed Skewer",
                emoji = "🍢",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "mushroom", count = 2 },
                    new IngredientCost { ingredientId = "herbs" },
                    new IngredientCost { ingredientId = "wild_honeycomb" },
                },
                buffs = new List<StatMod> { new StatMod { value = 0.25f }, new StatMod { stat = BuffStat.CritPct, value = 0.08f } },
                tags = new List<string> { "smoky", "veg" },
                description = "Mushroom skewers glazed with wild honeycomb.",
                tier = 2,
                upgradeOf = "mushroom_skewer",
            });
            c.recipes.Add(new RecipeData
            {
                id = "truffle_caps",
                name = "Charred Truffle Caps",
                emoji = "🍄",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "truffle" },
                    new IngredientCost { ingredientId = "mushroom", count = 2 },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.12f } },
                tags = new List<string> { "smoky", "veg" },
                description = "Truffle shavings melting over blackened caps. Every swing finds the gap.",
                stationLevel = 2,
            });
            c.recipes.Add(new RecipeData
            {
                id = "glowcap_kebab",
                name = "Glowcap Kebab",
                emoji = "🍡",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "glowcap" },
                    new IngredientCost { ingredientId = "mushroom" },
                    new IngredientCost { ingredientId = "garlic" },
                },
                buffs = new List<StatMod> { new StatMod { value = 0.3f } },
                tags = new List<string> { "smoky", "veg" },
                description = "Glowcaps charred until they sparkle.",
                stationLevel = 3,
            });
            c.recipes.Add(new RecipeData
            {
                id = "firecracker_skewer",
                name = "Firecracker Skewer",
                emoji = "🌶️",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "chili", count = 2 },
                    new IngredientCost { ingredientId = "mushroom" },
                },
                buffs = new List<StatMod> { new StatMod { value = 0.3f } },
                tags = new List<string> { "fire", "smoky" },
                description = "Pops and crackles on the grill — and in your mouth.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "honey_ribs",
                name = "Fire-Honey Ribs",
                emoji = "🍖",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "ember_meat" },
                    new IngredientCost { ingredientId = "fire_honey", count = 2 },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CritPct, value = 0.15f } },
                tags = new List<string> { "fire", "sweet" },
                description = "Sticky, spicy-sweet glaze. Every bite feels lucky.",
                stationLevel = 4,
            });
            c.recipes.Add(new RecipeData
            {
                id = "ember_steak",
                name = "Ember Steak",
                emoji = "🥩",
                station = Station.BBQ,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "ember_meat", count = 2 },
                    new IngredientCost { ingredientId = "garlic" },
                    new IngredientCost { ingredientId = "chili" },
                },
                buffs = new List<StatMod> { new StatMod { value = 0.4f } },
                tags = new List<string> { "fire" },
                description = "Seared over drake-coals. Pure, glowing strength.",
                stationLevel = 5,
            });
            c.recipes.Add(new RecipeData
            {
                id = "bread",
                name = "Camp Bread",
                emoji = "🍞",
                station = Station.Sandwich,
                tags = new List<string> { "plain" },
                description = "Free — there's always a loaf by the fire. Simple, light, better than nothing. Always Plain.",
                always = true,
            });
            c.recipes.Add(new RecipeData
            {
                id = "herb_butty",
                name = "Herb Butty",
                emoji = "🥪",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "wheat", count = 2 }, new IngredientCost { ingredientId = "herbs" } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.GatherZonePct, value = 0.3f } },
                tags = new List<string> { "veg" },
                description = "Fresh bread and a fistful of herbs. A forager's favourite.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "herb_butty_2",
                name = "Golden Herb Butty",
                emoji = "🥪",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "wheat", count = 2 },
                    new IngredientCost { ingredientId = "herbs" },
                    new IngredientCost { ingredientId = "golden_wheat" },
                },
                buffs = new List<StatMod>
                {
                    new StatMod { stat = BuffStat.GatherZonePct, value = 0.4f },
                    new StatMod { stat = BuffStat.MoveSpeedPct, value = 0.08f },
                },
                tags = new List<string> { "veg" },
                description = "A herb butty on golden-wheat bread.",
                tier = 2,
                upgradeOf = "herb_butty",
            });
            c.recipes.Add(new RecipeData
            {
                id = "bramble_jam",
                name = "Bramble Jam Sandwich",
                emoji = "🍓",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost> { new IngredientCost { ingredientId = "wheat" }, new IngredientCost { ingredientId = "berries", count = 2 } },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.3f } },
                tags = new List<string> { "sweet" },
                description = "Sticky fingers, sunny mood. Coins seem to find you.",
            });
            c.recipes.Add(new RecipeData
            {
                id = "marsh_butty",
                name = "Marsh Fish Butty",
                emoji = "🥖",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "wheat" },
                    new IngredientCost { ingredientId = "fish" },
                    new IngredientCost { ingredientId = "salt" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MoveSpeedPct, value = 0.1f } },
                tags = new List<string> { "fish" },
                description = "Crispy fish in a crusty roll. Proper wanderer's food.",
                stationLevel = 2,
            });
            c.recipes.Add(new RecipeData
            {
                id = "trail_wrap",
                name = "Minty Trail Wrap",
                emoji = "🌯",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "wheat" },
                    new IngredientCost { ingredientId = "mint" },
                    new IngredientCost { ingredientId = "herbs" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.MoveSpeedPct, value = 0.12f } },
                tags = new List<string> { "veg" },
                description = "Cool mint and herbs rolled tight. Puts a spring in your step.",
                stationLevel = 3,
            });
            c.recipes.Add(new RecipeData
            {
                id = "glow_egg_sandwich",
                name = "Glow-Egg Sandwich",
                emoji = "🥚",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "cave_egg" },
                    new IngredientCost { ingredientId = "truffle" },
                    new IngredientCost { ingredientId = "wheat" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.GatherZonePct, value = 0.4f } },
                tags = new List<string> { "stone" },
                description = "Shimmering egg and truffle shavings. Oddly enlightening.",
                stationLevel = 4,
            });
            c.recipes.Add(new RecipeData
            {
                id = "frostberry_toast",
                name = "Frostberry Toast",
                emoji = "🥞",
                station = Station.Sandwich,
                ingredients = new List<IngredientCost>
                {
                    new IngredientCost { ingredientId = "wheat" },
                    new IngredientCost { ingredientId = "frost_berry", count = 2 },
                    new IngredientCost { ingredientId = "fire_honey" },
                },
                buffs = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.4f } },
                tags = new List<string> { "sweet" },
                description = "Frostberries and fire honey on hot toast. You'll be halfway down the trail before it's cold.",
                stationLevel = 5,
            });
            // ── synergies (7) ──
            c.synergies.Add(new SynergyData
            {
                id = "balanced_feast",
                name = "Balanced Feast",
                emoji = "🍽️",
                description = "One meal from each station: +10% coins.",
                requireAllStations = true,
                requiredTag = "",
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.1f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "twin_flame",
                name = "Twin Flame",
                emoji = "🔥",
                description = "Eat 2 fiery meals: +15% gather zone.",
                requiredTag = "fire",
                requiredCount = 2,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.GatherZonePct, value = 0.15f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "stone_stomach",
                name = "Stone Stomach",
                emoji = "🪨",
                description = "Eat 2 hearty meals: +1 HP per kill.",
                requiredTag = "stone",
                requiredCount = 2,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.HealOnKill, value = 1f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "comfort_food",
                name = "Comfort Food",
                emoji = "🥘",
                description = "3 of the same meal: +5% speed.",
                requiredCount = 3,
                requireSameRecipe = true,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.MoveSpeedPct, value = 0.05f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "slow_simmer",
                name = "Slow Simmer",
                emoji = "🍲",
                description = "All Pot meals: +1 HP per kill.",
                requiredCount = 3,
                useStation = true,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.HealOnKill, value = 1f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "grill_master",
                name = "Grill Master",
                emoji = "🍖",
                description = "All BBQ meals: +15% coins.",
                requiredCount = 3,
                useStation = true,
                requiredStation = Station.BBQ,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.CoinPct, value = 0.15f } },
            });
            c.synergies.Add(new SynergyData
            {
                id = "packed_lunch",
                name = "Packed Lunch",
                emoji = "🥪",
                description = "All Sandwich meals: +20% gather zone.",
                requiredCount = 3,
                useStation = true,
                requiredStation = Station.Sandwich,
                bonus = new List<StatMod> { new StatMod { stat = BuffStat.GatherZonePct, value = 0.2f } },
            });
        }
    }
}
