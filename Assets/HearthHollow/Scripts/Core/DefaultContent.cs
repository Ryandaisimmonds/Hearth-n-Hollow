// Hearth & Hollow — default designer content (matches the Game Bible).
// Pure C#, Unity-free. Used by GameContentAsset "Reset To Defaults" and as a fallback at runtime.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        public static GameContent Create()
        {
            var c = new GameContent();
            c.tuning = new TuningData();
            // Design change (Oct 2026): meals are pre-run buffs now, so deeper routes hit harder to keep depth tense.
            c.tuning.monsterAtkPerDiff = 2f;   // bible prototype value: 1.5
            // Design change (Oct 2026): the player is weak on their own; meals (and each class's favourite station) carry a run.
            c.starterCoins = 30;

            BuildClasses(c);
            BuildMovesAndTrees(c);   // DefaultContentMoves.cs
            BuildIngredients(c);
            BuildMonsters(c);
            BuildRoutes(c);
            BuildRecipes(c);
            BuildSynergies(c);
            BuildGear(c);
            ApplyStory(c);           // DefaultContentStory.cs: story chapters, side dungeons, key items, hazard meals
            ApplyStory5(c);          // DefaultContentStory5.cs: darkness, mutators, 4 more side dungeons
            BuildGear7(c);           // DefaultContentGear.cs: head + boots tracks, trinkets
            AssignRarity(c);         // DefaultContentGear.cs: meal rarity
            BuildCooking9(c);        // DefaultContentCooking.cs: the 21-dish menu + accelerators (replaces the recipes above)
            BuildKitchen13(c);       // DefaultContentKitchen.cs: station levels, pantry shelves, building materials

            // Enough for Herb Stew (Pot) + Mushroom Skewer (BBQ) + Herb Butty (Sandwich) → Balanced Feast on day one,
            // or swap in Berry Porridge / Bramble Jam Sandwich.
            c.starterPantry.Add(Cost("herbs", 4));
            c.starterPantry.Add(Cost("wheat", 4));
            c.starterPantry.Add(Cost("berries", 3));
            c.starterPantry.Add(Cost("mushroom", 3));
            Speed24(c);              // DefaultContentArc.cs: speed gear doubled for the slower start
            DesignV2Cuts25(c);       // DefaultContentDesignV2.cs: no Darkness hazard, no cut-system text
            Elements26(c);           // DefaultContentDesignV2.cs: moves carry an element
            Cooking27(c);            // DefaultContentDesignV2.cs: cooking v10 (element slot, quality, tiers, light combos)
            Classes28(c);            // DefaultContentDesignV2.cs: minimal base stats, per-stat class meal lens
            return c;
        }

        // ───────────── helpers ─────────────
        static IngredientCost Cost(string id, int n) { return new IngredientCost { ingredientId = id, count = n }; }
        static StatMod Mod(BuffStat s, float v) { return new StatMod { stat = s, value = v }; }
        static List<string> L(params string[] items) { return new List<string>(items); }

        // ───────────── classes ─────────────
        static void BuildClasses(GameContent c)
        {
            c.classes.Add(new ClassData
            {
                id = ClassId.Warden, name = "Warden", emoji = "⚔️", role = "Bruiser",
                description = "A steady guardian of the campfire. Hits 15% harder. Loves the BBQ: grilled meals are 50% stronger.",
                baseMaxHp = 16, damageMult = 1.15f, gatherZoneMult = 1f, mealBuffMult = 1f,
                skill = new SkillData { kind = SkillKind.Cleave, name = "Cleave", emoji = "🪓", cooldownTurns = 3, value = 1.75f,
                    description = "One mighty swing for ×1.75 damage. Can crit." }
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Ranger, name = "Ranger", emoji = "🏹", role = "Forager",
                description = "Knows the wilds by heart. Much wider golden gather zone. Lives on trail food: sandwiches are 50% stronger.",
                baseMaxHp = 12, damageMult = 1f, gatherZoneMult = 1.6f, mealBuffMult = 1f,
                skill = new SkillData { kind = SkillKind.Volley, name = "Volley", emoji = "🎯", cooldownTurns = 3, value = 0.75f,
                    description = "Two quick arrows for ×0.75 damage each. Each can crit." }
            });
            c.classes.Add(new ClassData
            {
                id = ClassId.Cook, name = "Cook", emoji = "🍳", role = "Support",
                description = "Every meal tastes like home: all meals 25% stronger, and pot meals 50% on top.",
                baseMaxHp = 14, damageMult = 1f, gatherZoneMult = 1f, mealBuffMult = 1.25f,
                skill = new SkillData { kind = SkillKind.HeartySnack, name = "Hearty Snack", emoji = "🥐", cooldownTurns = 3, value = 12f,
                    description = "Munch a snack: heal 12 and guard this turn." }
            });
        }

        // ───────────── ingredients (2 per route, tiers rising) ─────────────
        static void Ing(GameContent c, string id, string name, string emoji, int tier)
        {
            c.ingredients.Add(new IngredientData { id = id, name = name, emoji = emoji, tier = tier });
        }

        static void BuildIngredients(GameContent c)
        {
            Ing(c, "herbs", "Wild Herbs", "🌿", 0);        // meadow
            Ing(c, "wheat", "Meadow Wheat", "🌾", 0);      // meadow
            Ing(c, "berries", "Bramble Berries", "🫐", 0); // berry
            Ing(c, "mushroom", "Forest Mushroom", "🍄", 1);// berry
            Ing(c, "salt", "Marsh Salt", "🧂", 1);         // salt
            Ing(c, "fish", "Reed Fish", "🐟", 1);          // salt
            Ing(c, "cave_egg", "Glow Egg", "🥚", 2);       // cavern
            Ing(c, "truffle", "Cave Truffle", "🌰", 2);    // cavern
            Ing(c, "chili", "Ridge Chili", "🌶️", 2);      // chili
            Ing(c, "garlic", "Smoky Garlic", "🧄", 2);     // chili
            Ing(c, "ember_meat", "Ember Haunch", "🍖", 3); // ember
            Ing(c, "fire_honey", "Fire Honey", "🍯", 3);   // ember
        }

        // ───────────── monsters ─────────────
        static void Mon(GameContent c, string id, string name, string emoji, MonsterBehaviour b, float hpMult, bool eliteOnly)
        {
            c.monsters.Add(new MonsterData { id = id, name = name, emoji = emoji, behaviour = b, hpMult = hpMult, eliteOnly = eliteOnly });
        }

        static void BuildMonsters(GameContent c)
        {
            Mon(c, "slime", "Moss Slime", "🟢", MonsterBehaviour.None, 1f, false);
            Mon(c, "boar", "Bramble Boar", "🐗", MonsterBehaviour.None, 1f, false);
            Mon(c, "bat", "Lantern Bee", "🐝", MonsterBehaviour.None, 1f, false);
            Mon(c, "crab", "Marsh Viper", "🐍", MonsterBehaviour.None, 1f, false);
            Mon(c, "wolf", "Fog Wolf", "🐺", MonsterBehaviour.DoubleHit, 1f, false);
            Mon(c, "shaman", "Bog Shaman", "🧙", MonsterBehaviour.Heal, 1f, false);
            Mon(c, "dragon", "Ember Drake", "🐉", MonsterBehaviour.WindUp, 1.5f, true);
        }

        // ───────────── routes (linear unlock chain) ─────────────
        static void BuildRoutes(GameContent c)
        {
            c.routes.Add(new RouteData
            {
                id = "meadow", name = "Meadow Trail", emoji = "🌼", kind = RouteKind.Trail,
                description = "Sunlit grass and sleepy slimes. A gentle first stroll.",
                baseDifficulty = 0, gatherChance = 0.30f, coinMult = 1.0f, clearReward = 20, clearXp = 15,
                unlockedByRouteId = "",
                monsterIds = L("slime", "boar"),
                gatherIngredientIds = L("herbs", "wheat")
            });
            c.routes.Add(new RouteData
            {
                id = "berry", name = "Berry Hunt", emoji = "🫐", kind = RouteKind.Hunt,
                description = "Thorny thickets heavy with fruit — and things that guard it.",
                baseDifficulty = 1, gatherChance = 0.15f, coinMult = 1.3f, clearReward = 30, clearXp = 20,
                unlockedByRouteId = "meadow",
                monsterIds = L("boar", "bat", "wolf"),
                gatherIngredientIds = L("berries", "mushroom", "herbs")
            });
            c.routes.Add(new RouteData
            {
                id = "salt", name = "Salt Marsh Trail", emoji = "🧂", kind = RouteKind.Trail,
                description = "Boardwalks over misty pools. Crabs click in the reeds.",
                baseDifficulty = 2, gatherChance = 0.28f, coinMult = 1.05f, clearReward = 40, clearXp = 28,
                unlockedByRouteId = "berry",
                monsterIds = L("crab", "slime", "shaman"),
                gatherIngredientIds = L("salt", "fish")
            });
            c.routes.Add(new RouteData
            {
                id = "cavern", name = "Cavern Hunt", emoji = "🕳️", kind = RouteKind.Hunt,
                description = "Glowing tunnels, echoing wings, and something warm sleeping deeper.",
                baseDifficulty = 3, gatherChance = 0.15f, coinMult = 1.4f, clearReward = 55, clearXp = 36,
                unlockedByRouteId = "salt",
                monsterIds = L("bat", "wolf", "shaman", "dragon"),
                gatherIngredientIds = L("cave_egg", "truffle", "mushroom")
            });
            c.routes.Add(new RouteData
            {
                id = "chili", name = "Chili Ridge Trail", emoji = "🌶️", kind = RouteKind.Trail,
                description = "Hot winds along a red ridge. Bring water, bring courage.",
                baseDifficulty = 4, gatherChance = 0.27f, coinMult = 1.1f, clearReward = 70, clearXp = 45,
                unlockedByRouteId = "cavern",
                monsterIds = L("boar", "wolf", "shaman"),
                gatherIngredientIds = L("chili", "garlic", "wheat")
            });
            c.routes.Add(new RouteData
            {
                id = "ember", name = "Ember Hunt", emoji = "🔥", kind = RouteKind.Hunt,
                description = "Smouldering hollows where the drakes nest. Big risks, rich pickings.",
                baseDifficulty = 5, gatherChance = 0.15f, coinMult = 1.6f, clearReward = 90, clearXp = 60,
                unlockedByRouteId = "chili",
                monsterIds = L("wolf", "shaman", "crab", "dragon"),
                gatherIngredientIds = L("ember_meat", "fire_honey", "chili")
            });
        }

        // ───────────── recipes (exactly 16: Pot 6 / BBQ 5 / Sandwich 5) ─────────────
        static void Rec(GameContent c, string id, string name, string emoji, Station st, IngredientCost[] cost,
                        StatMod[] buffs, string[] tags, string desc)
        {
            c.recipes.Add(new RecipeData
            {
                id = id, name = name, emoji = emoji, station = st,
                ingredients = new List<IngredientCost>(cost),
                buffs = new List<StatMod>(buffs),
                tags = new List<string>(tags),
                description = desc
            });
        }

        static void BuildRecipes(GameContent c)
        {
            // Pot (6)
            Rec(c, "herb_stew", "Herb Stew", "🍲", Station.Pot,
                new[] { Cost("herbs", 2), Cost("mushroom", 1) },
                new[] { Mod(BuffStat.MaxHp, 20) },
                new[] { "stone", "veg" }, "A thick, earthy stew that sits in your belly like a warm stone. Sturdier all day.");
            Rec(c, "berry_porridge", "Berry Porridge", "🥣", Station.Pot,
                new[] { Cost("wheat", 1), Cost("berries", 2) },
                new[] { Mod(BuffStat.XpPct, 0.25f), Mod(BuffStat.MaxHp, 8), Mod(BuffStat.DamagePct, 0.15f) },
                new[] { "sweet" }, "Creamy oats swirled with bramble berries. Clears the head.");
            Rec(c, "marsh_chowder", "Marsh Chowder", "🍜", Station.Pot,
                new[] { Cost("fish", 2), Cost("salt", 1) },
                new[] { Mod(BuffStat.MaxHp, 20), Mod(BuffStat.Armour, 2) },
                new[] { "stone", "fish" }, "Salty, hearty and steaming. Tastes of foggy mornings.");
            Rec(c, "truffle_risotto", "Truffle Risotto", "🍚", Station.Pot,
                new[] { Cost("truffle", 1), Cost("mushroom", 1), Cost("wheat", 1) },
                new[] { Mod(BuffStat.Armour, 3), Mod(BuffStat.MaxHp, 14), Mod(BuffStat.DamagePct, 0.2f) },
                new[] { "stone", "veg" }, "Slow-stirred by lantern light. Rich enough to shrug off a blow.");
            Rec(c, "ember_chili", "Ember Chili", "🫕", Station.Pot,
                new[] { Cost("chili", 2), Cost("garlic", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.5f), Mod(BuffStat.MaxHp, 12) },
                new[] { "fire" }, "A bubbling pot of fire. Your eyes water; your arms tingle.");
            Rec(c, "lantern_soup", "Lantern Egg Soup", "🍵", Station.Pot,
                new[] { Cost("cave_egg", 1), Cost("herbs", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.MaxHp, 32), Mod(BuffStat.Armour, 2) },
                new[] { "stone" }, "Glowing egg ribbons in clear broth. Fortifies body and spirit.");

            // BBQ (5)
            Rec(c, "mushroom_skewer", "Mushroom Skewer", "🍢", Station.BBQ,
                new[] { Cost("mushroom", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.35f), Mod(BuffStat.CritPct, 0.06f) },
                new[] { "smoky", "veg" }, "Charred caps on a stick, herby and smoky.");
            Rec(c, "grilled_fish", "Salt-Crust Fish", "🐟", Station.BBQ,
                new[] { Cost("fish", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.CritPct, 0.12f), Mod(BuffStat.DamagePct, 0.25f), Mod(BuffStat.CoinPct, 0.15f) },
                new[] { "smoky", "fish" }, "Baked in a salt shell on the coals. Sharpens the senses.");
            Rec(c, "firecracker_skewer", "Firecracker Skewer", "🌶️", Station.BBQ,
                new[] { Cost("chili", 2), Cost("mushroom", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.6f), Mod(BuffStat.CritPct, 0.06f) },
                new[] { "fire", "smoky" }, "Pops and crackles on the grill — and in your mouth.");
            Rec(c, "ember_steak", "Ember Steak", "🥩", Station.BBQ,
                new[] { Cost("ember_meat", 2), Cost("garlic", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.8f), Mod(BuffStat.MaxHp, 10) },
                new[] { "fire" }, "Seared over drake-coals. Pure, glowing strength.");
            Rec(c, "honey_ribs", "Fire-Honey Ribs", "🍖", Station.BBQ,
                new[] { Cost("ember_meat", 1), Cost("fire_honey", 2) },
                new[] { Mod(BuffStat.CritPct, 0.18f), Mod(BuffStat.DamagePct, 0.45f) },
                new[] { "fire", "sweet" }, "Sticky, spicy-sweet glaze. Every bite feels lucky.");

            // Sandwich (5 + free bread)
            Rec(c, "bread", "Camp Bread", "🍞", Station.Sandwich,
                new IngredientCost[0],
                new[] { Mod(BuffStat.MaxHp, 6), Mod(BuffStat.DamagePct, 0.1f) },
                new[] { "plain" }, "Free — there's always a loaf by the fire. Plain, filling, better than nothing.");
            Rec(c, "herb_butty", "Herb Butty", "🥪", Station.Sandwich,
                new[] { Cost("wheat", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.GatherZonePct, 0.3f), Mod(BuffStat.MaxHp, 8), Mod(BuffStat.DamagePct, 0.2f) },
                new[] { "veg" }, "Fresh bread and a fistful of herbs. A forager's favourite.");
            Rec(c, "bramble_jam", "Bramble Jam Sandwich", "🍓", Station.Sandwich,
                new[] { Cost("wheat", 1), Cost("berries", 2) },
                new[] { Mod(BuffStat.CoinPct, 0.2f), Mod(BuffStat.MaxHp, 8), Mod(BuffStat.CritPct, 0.08f) },
                new[] { "sweet" }, "Sticky fingers, sunny mood. Coins seem to find you.");
            Rec(c, "marsh_butty", "Marsh Fish Butty", "🥖", Station.Sandwich,
                new[] { Cost("wheat", 1), Cost("fish", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.Armour, 3), Mod(BuffStat.DamagePct, 0.25f), Mod(BuffStat.CoinPct, 0.15f) },
                new[] { "fish" }, "Crispy fish in a crusty roll. Proper wanderer's food.");
            Rec(c, "glow_egg_sandwich", "Glow-Egg Sandwich", "🥚", Station.Sandwich,
                new[] { Cost("cave_egg", 1), Cost("truffle", 1), Cost("wheat", 1) },
                new[] { Mod(BuffStat.MaxHp, 16), Mod(BuffStat.DamagePct, 0.3f), Mod(BuffStat.XpPct, 0.25f), Mod(BuffStat.GatherZonePct, 0.2f) },
                new[] { "stone" }, "Shimmering egg and truffle shavings. Oddly enlightening.");
            Rec(c, "ember_toastie", "Ember Toastie", "🔥", Station.Sandwich,
                new[] { Cost("wheat", 1), Cost("ember_meat", 1), Cost("chili", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.5f), Mod(BuffStat.CritPct, 0.12f), Mod(BuffStat.MaxHp, 8) },
                new[] { "fire" }, "Pressed until it smoulders. Handle with oven gloves.");
        }

        // ───────────── synergies ─────────────
        static void BuildSynergies(GameContent c)
        {
            c.synergies.Add(new SynergyData
            {
                id = "balanced_feast", name = "Balanced Feast", emoji = "🍽️",
                description = "One meal from each station: all meal bonuses ×1.5.",
                requireAllStations = true, requiredTag = "", requiredCount = 0, allBuffMultiplier = 1.5f
            });
            var twin = new SynergyData
            {
                id = "twin_flame", name = "Twin Flame", emoji = "🔥",
                description = "Eat 2 fiery meals: +10% crit chance, +25% damage.",
                requireAllStations = false, requiredTag = "fire", requiredCount = 2, allBuffMultiplier = 1f
            };
            twin.bonus.Add(Mod(BuffStat.CritPct, 0.10f));
            twin.bonus.Add(Mod(BuffStat.DamagePct, 0.25f));
            c.synergies.Add(twin);
            var stone = new SynergyData
            {
                id = "stone_stomach", name = "Stone Stomach", emoji = "🪨",
                description = "Eat 2 hearty meals: +2 armour, +14 max HP.",
                requireAllStations = false, requiredTag = "stone", requiredCount = 2, allBuffMultiplier = 1f
            };
            stone.bonus.Add(Mod(BuffStat.Armour, 2));
            stone.bonus.Add(Mod(BuffStat.MaxHp, 14));
            c.synergies.Add(stone);

            // Combos for how the tray is packed
            c.synergies.Add(new SynergyData
            {
                id = "comfort_food", name = "Comfort Food", emoji = "🥘",
                description = "3 of the same meal: that meal's bonus counts an extra half again.",
                requireSameRecipe = true, requiredCount = 3, sameRecipeBonus = 0.5f, allBuffMultiplier = 1f
            });
            var simmer = new SynergyData
            {
                id = "slow_simmer", name = "Slow Simmer", emoji = "🍲",
                description = "All Pot meals: +18 max HP, +2 armour.",
                useStation = true, requiredStation = Station.Pot, requiredCount = 3, allBuffMultiplier = 1f
            };
            simmer.bonus.Add(Mod(BuffStat.MaxHp, 18));
            simmer.bonus.Add(Mod(BuffStat.Armour, 2));
            c.synergies.Add(simmer);
            var grill = new SynergyData
            {
                id = "grill_master", name = "Grill Master", emoji = "🍖",
                description = "All BBQ meals: +40% damage, +8% crit.",
                useStation = true, requiredStation = Station.BBQ, requiredCount = 3, allBuffMultiplier = 1f
            };
            grill.bonus.Add(Mod(BuffStat.DamagePct, 0.40f));
            grill.bonus.Add(Mod(BuffStat.CritPct, 0.08f));
            c.synergies.Add(grill);
            var lunch = new SynergyData
            {
                id = "packed_lunch", name = "Packed Lunch", emoji = "🥪",
                description = "All Sandwich meals: +25% damage, +10 max HP, +25% gather zone, +15% coins.",
                useStation = true, requiredStation = Station.Sandwich, requiredCount = 3, allBuffMultiplier = 1f
            };
            lunch.bonus.Add(Mod(BuffStat.DamagePct, 0.25f));
            lunch.bonus.Add(Mod(BuffStat.MaxHp, 10));
            lunch.bonus.Add(Mod(BuffStat.GatherZonePct, 0.25f));
            lunch.bonus.Add(Mod(BuffStat.CoinPct, 0.15f));
            c.synergies.Add(lunch);
        }

        // ───────────── gear ─────────────
        static void Gear(List<GearTier> list, int tier, string name, int price, float value)
        {
            list.Add(new GearTier { tier = tier, name = name, price = price, value = value });
        }

        static void BuildGear(GameContent c)
        {
            // Gear is a small step each tier — meals are where the real power comes from.
            Gear(c.weaponTiers, 0, "Wooden Spoon", 0, 4f);
            Gear(c.weaponTiers, 1, "Iron Ladle", 40, 5f);
            Gear(c.weaponTiers, 2, "Carving Knife", 90, 6f);
            Gear(c.weaponTiers, 3, "Camp Cleaver", 160, 7.5f);
            Gear(c.weaponTiers, 4, "Drakebone Skillet", 260, 9f);

            Gear(c.armourTiers, 0, "Apron", 0, 0f);
            Gear(c.armourTiers, 1, "Quilted Apron", 40, 1f);
            Gear(c.armourTiers, 2, "Leather Smock", 90, 2f);
            Gear(c.armourTiers, 3, "Cast-Iron Vest", 160, 3f);
            Gear(c.armourTiers, 4, "Hearthguard Mail", 260, 4f);
            BuildBags(c);
        }

        /// Bag upgrades (value = slots; each slot stacks 5 of one ingredient). Start tiny, grow with every trip.
        static void BuildBags(GameContent c)
        {
            if (c.bagTiers == null) c.bagTiers = new List<GearTier>();
            if (c.bagTiers.Count > 0) return;
            Gear(c.bagTiers, 0, "Belt Pouch", 0, 6f);
            Gear(c.bagTiers, 1, "Satchel", 40, 9f);
            Gear(c.bagTiers, 2, "Forager's Basket", 90, 12f);
            Gear(c.bagTiers, 3, "Rucksack", 160, 16f);
            Gear(c.bagTiers, 4, "Backpack", 260, 20f);
            Gear(c.bagTiers, 5, "Expedition Pack", 400, 25f);
            Gear(c.bagTiers, 6, "Hollow-Walker's Pack", 600, 30f);
        }
    }
}
