// Hearth & Hollow — the main story (the Ember Path) and the side dungeons. Pure C#. contentVersion 4.
//
// Story chapters (RouteData.chapter 1–8) run in order; some also need a KEY ITEM found in a side dungeon:
//   1 Meadow Trail → 2 Berry Hunt → 3 Salt Marsh → 4 Cavern (🏮 Firefly Lantern) → 5 Chili Ridge (🥾 Bog-Walker Boots)
//   → 6 Ember Hunt (🗝️ Watchtower Key, Heat) → 7 Frostfang Pass (🪢 Thornsilk Rope, Cold) → 8 The Hollow Heart (📜 Everflame Recipe)
// Side dungeons:
//   Firefly Glade (after ch3) → Firefly Lantern · Spore Mire (after ch4, Poison) → Bog-Walker Boots
//   Old Watchtower (after ch5, WARDEN only) → Watchtower Key · Thornwood Canopy (after ch6, RANGER only) → Thornsilk Rope
//   Forgotten Larder (after ch7, COOK only) → Everflame Recipe · Frost Hollow (after ch7, Cold) → optional, rich rewards
// Hazards: Poison (antidote: nettle meals), Heat (cooling: mint meals), Cold (warming: ginger meals). One counter meal
// halves the hazard, two stop it (TuningData.hazardMitigationPerMeal). Hazards never take you below 1 HP.
//
// ApplyStory is used both by Create() and by Upgrade(): it adds anything missing (by id) and only fills story fields on
// routes that don't have them yet, so edits made in the content asset are kept.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        static void ApplyStory(GameContent c)
        {
            if (c.keyItems == null) c.keyItems = new List<KeyItemData>();
            if (string.IsNullOrEmpty(c.storyTitle)) c.storyTitle = "The Everflame";
            if (string.IsNullOrEmpty(c.storyPrologue))
                c.storyPrologue = "Every hearth in the valley was lit from the Everflame, a fire that has burned in the Hollow Heart for a thousand winters. " +
                                  "This autumn it guttered. The fog crept in, the hearths went cold, and the old roads closed. " +
                                  "Old Wren, keeper of the camp, has one map left: the Ember Path. Cook well, travel light, and bring the flame home.";

            // ───── key items ─────
            Key(c, "glow_lantern", "Firefly Lantern", "🏮", "A jar of sleepy fireflies. Bright enough to light the Cavern's dark.");
            Key(c, "bog_boots", "Bog-Walker Boots", "🥾", "Waxed boots that never sink. The only way across Chili Ridge's red mud.");
            Key(c, "watch_key", "Watchtower Key", "🗝️", "A heavy iron key from the Wardens' old post. It fits the Ember Gate.");
            Key(c, "climbing_rope", "Thornsilk Rope", "🪢", "Woven from canopy silk by the Thornwood Rangers. Strong enough for Frostfang's cliffs.");
            Key(c, "everflame_recipe", "Everflame Recipe", "📜", "Grandmother Hob's stew, hot enough to relight the Everflame itself.");

            // ───── new ingredients ─────
            IngOnce(c, "nettle", "Stinging Nettle", "🌱", 1);
            IngOnce(c, "mint", "Glade Mint", "🍃", 1);
            IngOnce(c, "ginger", "Snow Ginger", "🫚", 2);
            IngOnce(c, "frost_berry", "Frostberry", "🍇", 3);

            // ───── hazard counter meals (one per station, so every class's favourite has one) ─────
            RecOnce(c, "nettle_soup", "Nettle Soup", "🥬", Station.Pot, new[] { Cost("nettle", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.MaxHp, 16), Mod(BuffStat.Armour, 1) }, new[] { "stone", "veg" }, Hazard.Poison,
                "Sharp, green and cleansing. Spores can't settle in a belly full of nettle. Protects against poison.");
            RecOnce(c, "nettle_skewer", "Nettle-Wrapped Skewer", "🍡", Station.BBQ, new[] { Cost("nettle", 1), Cost("mushroom", 2) },
                new[] { Mod(BuffStat.DamagePct, 0.35f), Mod(BuffStat.CritPct, 0.05f) }, new[] { "smoky", "veg" }, Hazard.Poison,
                "Mushrooms grilled in nettle leaves. Protects against poison.");
            RecOnce(c, "nettle_wrap", "Nettle & Herb Wrap", "🌯", Station.Sandwich, new[] { Cost("nettle", 1), Cost("wheat", 1), Cost("herbs", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.2f), Mod(BuffStat.MaxHp, 8), Mod(BuffStat.GatherZonePct, 0.15f) }, new[] { "veg" }, Hazard.Poison,
                "A forager's wrap, wilted nettle and herbs. Protects against poison.");

            RecOnce(c, "mint_soup", "Chilled Mint Soup", "🥗", Station.Pot, new[] { Cost("mint", 2), Cost("berries", 1) },
                new[] { Mod(BuffStat.MaxHp, 18), Mod(BuffStat.XpPct, 0.1f) }, new[] { "sweet" }, Hazard.Heat,
                "Cold, sweet and green. Keeps your head clear in the heat. Protects against heat.");
            RecOnce(c, "mint_fish", "Mint-Glazed Fish", "🐠", Station.BBQ, new[] { Cost("mint", 1), Cost("fish", 1), Cost("salt", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.45f), Mod(BuffStat.CritPct, 0.08f) }, new[] { "smoky", "fish" }, Hazard.Heat,
                "Charred outside, cool mint inside. Protects against heat.");
            RecOnce(c, "mint_butty", "Mint & Berry Butty", "🥙", Station.Sandwich, new[] { Cost("mint", 1), Cost("wheat", 1), Cost("berries", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.25f), Mod(BuffStat.CritPct, 0.06f), Mod(BuffStat.MaxHp, 8) }, new[] { "sweet" }, Hazard.Heat,
                "Squashed berries and fresh mint. Protects against heat.");

            RecOnce(c, "ginger_stew", "Ginger Hotpot", "🍛", Station.Pot, new[] { Cost("ginger", 1), Cost("fish", 1), Cost("garlic", 1) },
                new[] { Mod(BuffStat.MaxHp, 26), Mod(BuffStat.Armour, 2) }, new[] { "stone", "fire" }, Hazard.Cold,
                "Warms you from the toes up. Protects against the cold.");
            RecOnce(c, "ginger_skewer", "Ginger-Glazed Skewer", "🍗", Station.BBQ, new[] { Cost("ginger", 1), Cost("ember_meat", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.7f), Mod(BuffStat.MaxHp, 8) }, new[] { "fire" }, Hazard.Cold,
                "Sticky, fiery ginger glaze over drake-coals. Protects against the cold.");
            RecOnce(c, "ginger_toastie", "Ginger & Honey Toastie", "🥞", Station.Sandwich, new[] { Cost("ginger", 1), Cost("wheat", 1), Cost("fire_honey", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.4f), Mod(BuffStat.CritPct, 0.1f), Mod(BuffStat.MaxHp, 10) }, new[] { "fire", "sweet" }, Hazard.Cold,
                "Toasted until the honey bubbles. Protects against the cold.");

            RecOnce(c, "frostberry_crumble", "Frostberry Crumble", "🥧", Station.Pot, new[] { Cost("frost_berry", 2), Cost("wheat", 1), Cost("fire_honey", 1) },
                new[] { Mod(BuffStat.MaxHp, 30), Mod(BuffStat.DamagePct, 0.4f), Mod(BuffStat.CritPct, 0.08f) }, new[] { "sweet" }, Hazard.None,
                "Frostberries only grow in Frost Hollow. Worth every shiver.");

            // ───── story chapters (existing routes get their story; new ones are added) ─────
            Story(c, "meadow", 1, null, null,
                "Old Wren needs herbs and wheat for travelling bread before anyone can walk the Ember Path. The meadow is gentle: a few sleepy slimes, nothing more.",
                "Your bag smells of fresh herbs. From the hill you can see the bramble woods. The Ember Path runs right through them.");
            Story(c, "berry", 2, null, null,
                "The Path bends into thorny woods heavy with fruit, and with the things that guard it.",
                "Past the brambles the ground turns to salt and reed. Wren says the marsh folk once lived beyond it.");
            Story(c, "salt", 3, null, null,
                "Boardwalks over misty pools. The Bog Shamans have been stirring the fog, and the vipers are restless.",
                "Beyond the marsh, the Path drops into a cavern black as soot. You'll need a light. Fireflies drift above a glade to the east…");
            Story(c, "cavern", 4, "glow_lantern", null,
                "With the Firefly Lantern held high, the Cavern's crystals wake around you. Something warm is sleeping deeper down.",
                "At the far end of the caves the Path climbs onto a ridge of red mud. Normal boots would sink. The bog-walkers of the Spore Mire might know a trick.");
            Story(c, "chili", 5, "bog_boots", null,
                "Bog-Walker Boots squelch over the red mud. Chilies grow wild here, and so do tempers.",
                "The ridge ends at a gate of black iron: the Ember Gate, locked tight. Wren remembers that the Wardens of the Old Watchtower kept its key.");
            Story(c, "ember", 6, "watch_key", null,
                "The Watchtower Key turns with a groan. Beyond the Ember Gate the air shimmers; the heat will sap you. Pack something cooling.",
                "Past the fire fields, the Path climbs into ice: Frostfang Pass. Its cliffs are sheer. Only the Rangers of the Thornwood weave a rope strong enough.");
            var ember = c.Route("ember");
            if (ember != null && ember.hazard == Hazard.None) { ember.hazard = Hazard.Heat; ember.hazardPct = 0.012f; }
            var salt = c.Route("salt");
            if (salt != null && salt.description != null && salt.description.Contains("Crabs click")) salt.description = "Boardwalks over misty pools. Vipers hiss in the reeds.";

            RouteOnce(c, new RouteData
            {
                id = "frost", name = "Frostfang Pass", emoji = "❄️", kind = RouteKind.Trail,
                description = "An icy climb where the wind howls. The cold slows every step.",
                baseDifficulty = 6, gatherChance = 0.24f, coinMult = 1.4f, clearReward = 115, clearXp = 75,
                unlockedByRouteId = "ember", chapter = 7, requiredKeyItem = "climbing_rope",
                hazard = Hazard.Cold, hazardPct = 0.012f,
                monsterIds = L("wolf", "crab", "shaman"),
                gatherIngredientIds = L("ginger", "frost_berry", "salt"),
                storyIntro = "Thornsilk rope over your shoulder, you climb. The cold bites and slows every step; warm food helps.",
                storyOutro = "At the top of the pass lies the Hollow Heart. The Everflame is a single blue spark. Nothing you carry burns hot enough to relight it. Grandmother Hob's lost recipe might.",
            });
            RouteOnce(c, new RouteData
            {
                id = "heart", name = "The Hollow Heart", emoji = "💛", kind = RouteKind.Hunt,
                description = "The source of the fog, and of every hearth's fire. Something in the mist doesn't want it lit.",
                baseDifficulty = 7, gatherChance = 0.12f, coinMult = 2f, clearReward = 200, clearXp = 120,
                unlockedByRouteId = "frost", chapter = 8, requiredKeyItem = "everflame_recipe",
                monsterIds = L("wolf", "shaman", "bat", "dragon"),
                gatherIngredientIds = L("ember_meat", "frost_berry", "fire_honey"),
                storyIntro = "With the Everflame Recipe in your pack, you step into the Hollow Heart. The fog thickens, and the great drake that feeds on it wakes.",
                storyOutro = "You stir Grandmother Hob's stew into the embers and the Everflame roars back to life. Across the valley the hearths are lit again, one by one. Wren is waiting at camp with a bowl of something warm. The end, for now.",
            });

            // ───── side dungeons ─────
            RouteOnce(c, new RouteData
            {
                id = "firefly", name = "Firefly Glade", emoji = "✨", kind = RouteKind.Trail,
                description = "A hush of glowing grass where fireflies nest. The Lantern Bees guard it fiercely.",
                baseDifficulty = 2, gatherChance = 0.32f, coinMult = 1.1f, clearReward = 35, clearXp = 22,
                unlockedByRouteId = "salt", rewardKeyItem = "glow_lantern",
                monsterIds = L("bat", "slime"),
                gatherIngredientIds = L("nettle", "mint", "berries"),
                storyIntro = "The fireflies here sleep in jars left by an old lantern-maker. Clear the glade and one of those jars is yours.",
                storyOutro = "You found the Firefly Lantern! Its warm glow will light the Cavern on the Ember Path.",
            });
            RouteOnce(c, new RouteData
            {
                id = "spore", name = "Spore Mire", emoji = "🍄", kind = RouteKind.Hunt,
                description = "A bog of puffing spore-caps. The air itself is poison; nettle dishes help.",
                baseDifficulty = 3, gatherChance = 0.22f, coinMult = 1.3f, clearReward = 55, clearXp = 34,
                unlockedByRouteId = "cavern", rewardKeyItem = "bog_boots",
                hazard = Hazard.Poison, hazardPct = 0.012f,
                monsterIds = L("slime", "crab", "shaman"),
                gatherIngredientIds = L("mushroom", "nettle", "fish"),
                storyIntro = "The bog-walkers left their boots at the bottom of the mire when the spores came. Pack an antidote: nettle soup, skewers or wraps.",
                storyOutro = "You found the Bog-Walker Boots! Chili Ridge's red mud can't swallow you now.",
            });
            RouteOnce(c, new RouteData
            {
                id = "watchtower", name = "Old Watchtower", emoji = "🏰", kind = RouteKind.Hunt,
                description = "The Wardens' old post, overrun by boars and wolves. Its doors only open for a Warden.",
                baseDifficulty = 4, gatherChance = 0.18f, coinMult = 1.35f, clearReward = 75, clearXp = 45,
                unlockedByRouteId = "chili", rewardKeyItem = "watch_key",
                classLocked = true, requiredClass = ClassId.Warden,
                monsterIds = L("boar", "wolf", "shaman"),
                gatherIngredientIds = L("garlic", "salt", "herbs"),
                storyIntro = "Only a Warden's crest opens the tower gate. The Ember Gate's key still hangs in the captain's room at the top.",
                storyOutro = "You found the Watchtower Key! It fits the Ember Gate at the end of Chili Ridge.",
            });
            RouteOnce(c, new RouteData
            {
                id = "canopy", name = "Thornwood Canopy", emoji = "🌳", kind = RouteKind.Trail,
                description = "A forest that lives in the treetops. Only a Ranger can find the paths between the branches.",
                baseDifficulty = 5, gatherChance = 0.3f, coinMult = 1.25f, clearReward = 90, clearXp = 55,
                unlockedByRouteId = "ember", rewardKeyItem = "climbing_rope",
                classLocked = true, requiredClass = ClassId.Ranger,
                monsterIds = L("bat", "crab", "wolf"),
                gatherIngredientIds = L("ginger", "berries", "fire_honey"),
                storyIntro = "The Thornwood Rangers left a spool of thornsilk in their highest lookout. Only a Ranger can climb that far.",
                storyOutro = "You found the Thornsilk Rope! Now Frostfang Pass can be climbed.",
            });
            RouteOnce(c, new RouteData
            {
                id = "larder", name = "Forgotten Larder", emoji = "🥫", kind = RouteKind.Hunt,
                description = "Grandmother Hob's cellar under the old inn. Only a Cook knows the larder's secret door.",
                baseDifficulty = 6, gatherChance = 0.26f, coinMult = 1.5f, clearReward = 110, clearXp = 68,
                unlockedByRouteId = "frost", rewardKeyItem = "everflame_recipe",
                classLocked = true, requiredClass = ClassId.Cook,
                monsterIds = L("slime", "boar", "shaman"),
                gatherIngredientIds = L("truffle", "cave_egg", "ginger"),
                storyIntro = "Somewhere among the jars and barrels is Grandmother Hob's recipe book. Only a Cook would know where she hid it.",
                storyOutro = "You found the Everflame Recipe! The way into the Hollow Heart is open.",
            });
            RouteOnce(c, new RouteData
            {
                id = "frosthollow", name = "Frost Hollow", emoji = "🧊", kind = RouteKind.Hunt,
                description = "An ice cave off the pass. Bitter cold, rich rewards: frostberries grow nowhere else.",
                baseDifficulty = 7, gatherChance = 0.2f, coinMult = 1.9f, clearReward = 150, clearXp = 90,
                unlockedByRouteId = "frost",
                hazard = Hazard.Cold, hazardPct = 0.016f,
                monsterIds = L("crab", "wolf", "shaman", "dragon"),
                gatherIngredientIds = L("frost_berry", "ginger", "fish"),
                storyIntro = "Not on the Ember Path. Just very cold, very dangerous, and full of frostberries. Pack something warming.",
                storyOutro = "You made it out of Frost Hollow with frostberries to spare. Try them in a crumble.",
            });

            // keep the board in a sensible order: story chapters first, then side dungeons in unlock order
            var order = new List<string> { "meadow", "berry", "salt", "cavern", "chili", "ember", "frost", "heart",
                                           "firefly", "spore", "watchtower", "canopy", "larder", "frosthollow" };
            c.routes.Sort((a, b) =>
            {
                int ia = order.IndexOf(a.id), ib = order.IndexOf(b.id);
                if (ia < 0) ia = 1000; if (ib < 0) ib = 1000;
                return ia.CompareTo(ib);
            });
        }

        static void Key(GameContent c, string id, string name, string emoji, string desc)
        {
            if (c.keyItems.Exists(k => k.id == id)) return;
            c.keyItems.Add(new KeyItemData { id = id, name = name, emoji = emoji, description = desc });
        }

        static void IngOnce(GameContent c, string id, string name, string emoji, int tier)
        {
            if (c.ingredients.Exists(i => i.id == id)) return;
            Ing(c, id, name, emoji, tier);
        }

        static void RecOnce(GameContent c, string id, string name, string emoji, Station st, IngredientCost[] cost,
                            StatMod[] buffs, string[] tags, Hazard counters, string desc)
        {
            if (c.recipes.Exists(r => r.id == id)) return;
            Rec(c, id, name, emoji, st, cost, buffs, tags, desc);
            c.recipes[c.recipes.Count - 1].counters = counters;
        }

        static void RouteOnce(GameContent c, RouteData r)
        {
            if (c.routes.Exists(x => x.id == r.id)) return;
            c.routes.Add(r);
        }

        /// Gives an existing route its chapter, gate and story text — only if it has no story yet.
        static void Story(GameContent c, string id, int chapter, string key, string reward, string intro, string outro)
        {
            var r = c.Route(id);
            if (r == null || r.chapter > 0) return;
            r.chapter = chapter;
            if (string.IsNullOrEmpty(r.requiredKeyItem)) r.requiredKeyItem = key;
            if (string.IsNullOrEmpty(r.rewardKeyItem)) r.rewardKeyItem = reward;
            if (string.IsNullOrEmpty(r.storyIntro)) r.storyIntro = intro;
            if (string.IsNullOrEmpty(r.storyOutro)) r.storyOutro = outro;
        }
    }
}
