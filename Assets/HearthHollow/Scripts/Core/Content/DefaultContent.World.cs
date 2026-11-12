// Hearth & Hollow — default content: monsters, areas (routes) and key items.
// The current design, written out directly (refactor 03 collapsed the old 44 upgrade steps into this).
// Edit freely: these files ARE the design now. Pure C#, no UnityEngine.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        /// The monsters (with how each one fights), every area and story chapter, and the story's key items.
        static void AddWorld(GameContent c)
        {
            // ── monsters (10) ──
            c.monsters.Add(new MonsterData
            {
                id = "slime",
                name = "Moss Slime",
                emoji = "🟢",
                moveSpeed = 1.8f,
                attackRange = 0.9f,
                windup = 0.45f,
                cooldownMult = 1.1f,
                lightReaction = LightReaction.Drawn,
                element = Element.Nature,
                splits = true,
            });
            c.monsters.Add(new MonsterData
            {
                id = "boar",
                name = "Bramble Boar",
                emoji = "🐗",
                archetype = EnemyArchetype.Charger,
                windup = 0.6f,
                cooldownMult = 1.3f,
                element = Element.Nature,
            });
            c.monsters.Add(new MonsterData
            {
                id = "bat",
                name = "Lantern Bee",
                emoji = "🐝",
                archetype = EnemyArchetype.Flyer,
                moveSpeed = 3.2f,
                attackRange = 2.4f,
                windup = 0.3f,
                cooldownMult = 0.9f,
                lightReaction = LightReaction.Shy,
                element = Element.Fire,
                swoops = true,
            });
            c.monsters.Add(new MonsterData { id = "crab", name = "Marsh Viper", emoji = "🐍", moveSpeed = 2.9f, windup = 0.3f, element = Element.Water });
            c.monsters.Add(new MonsterData
            {
                id = "wolf",
                name = "Fog Wolf",
                emoji = "🐺",
                behaviour = MonsterBehaviour.DoubleHit,
                moveSpeed = 3.3f,
                windup = 0.35f,
                cooldownMult = 1.3f,
                hits = 2,
                lightReaction = LightReaction.Shy,
                element = Element.Frost,
                circles = true,
            });
            c.monsters.Add(new MonsterData
            {
                id = "shaman",
                name = "Bog Shaman",
                emoji = "🧙",
                behaviour = MonsterBehaviour.Heal,
                archetype = EnemyArchetype.Caster,
                moveSpeed = 2f,
                attackRange = 6f,
                windup = 0.6f,
                cooldownMult = 1.5f,
                healsAllies = true,
                element = Element.Water,
            });
            c.monsters.Add(new MonsterData
            {
                id = "dragon",
                name = "Ember Drake",
                emoji = "🐉",
                behaviour = MonsterBehaviour.WindUp,
                hpMult = 1.5f,
                eliteOnly = true,
                archetype = EnemyArchetype.Brute,
                moveSpeed = 1.7f,
                attackRange = 1.4f,
                windup = 0.7f,
                cooldownMult = 1.2f,
                projectileSpeed = 7f,
                element = Element.Fire,
            });
            c.monsters.Add(new MonsterData
            {
                id = "bramble_sow",
                name = "Bramble Sow",
                emoji = "🐗",
                hpMult = 1.3f,
                eliteOnly = true,
                artId = "boar",
                archetype = EnemyArchetype.Charger,
                windup = 0.6f,
                cooldownMult = 1.3f,
                element = Element.Nature,
            });
            c.monsters.Add(new MonsterData
            {
                id = "hoarfrost_alpha",
                name = "Hoarfrost Alpha",
                emoji = "🐺",
                behaviour = MonsterBehaviour.DoubleHit,
                hpMult = 1.5f,
                eliteOnly = true,
                artId = "wolf",
                moveSpeed = 3.3f,
                windup = 0.35f,
                cooldownMult = 1.3f,
                hits = 2,
                lightReaction = LightReaction.Shy,
                element = Element.Frost,
                weakTo = new List<Element> { Element.Fire },
                resists = new List<Element> { Element.Frost },
            });
            c.monsters.Add(new MonsterData
            {
                id = "ridge_drake",
                name = "Ridge Drake",
                emoji = "🐉",
                behaviour = MonsterBehaviour.WindUp,
                hpMult = 1.3f,
                eliteOnly = true,
                artId = "dragon",
                archetype = EnemyArchetype.Brute,
                moveSpeed = 1.7f,
                attackRange = 1.4f,
                windup = 0.7f,
                cooldownMult = 1.2f,
                projectileSpeed = 7f,
                element = Element.Fire,
            });
            // ── routes (19) ──
            c.routes.Add(new RouteData
            {
                id = "meadow",
                name = "Meadow Hollow",
                emoji = "🌼",
                description = "Sunlit grass and sleepy slimes. Three floors down, the Bramble Sow snores.",
                gatherChance = 0.3f,
                unlockedByRouteId = "",
                monsterIds = new List<string> { "slime", "boar" },
                floors = 3,
                bossIds = new List<string> { "bramble_sow" },
                floorMonsters = new List<FloorMonster> { new FloorMonster { monsterId = "bat", minFloor = 2 } },
                floorDrops = new List<FloorDrop> { new FloorDrop { ingredientId = "ginger", minFloor = 2 } },
                gatherIngredientIds = new List<string> { "herbs", "wheat", "mushroom", "berries" },
                materialIds = new List<string> { "timber" },
                chapter = 1,
                storyIntro = "Sunlit grass and sleepy slimes. Three floors down, something big and bristly snores.",
                storyOutro = "Your bag smells of fresh herbs. From the hill you can see the bramble woods. The Ember Path runs right through them.",
            });
            c.routes.Add(new RouteData
            {
                id = "berry",
                name = "Berry Hunt",
                emoji = "🫐",
                kind = RouteKind.Hunt,
                description = "Thorny thickets heavy with fruit — and things that guard it.",
                baseDifficulty = 1,
                gatherChance = 0.15f,
                coinMult = 1.3f,
                clearReward = 30,
                clearXp = 20,
                unlockedByRouteId = "meadow",
                monsterIds = new List<string> { "boar", "bat", "wolf" },
                gatherIngredientIds = new List<string> { "berries", "mushroom", "herbs" },
                materialIds = new List<string> { "fieldstone", "timber" },
                chapter = 2,
                storyIntro = "The Path bends into thorny woods heavy with fruit, and with the things that guard it.",
                storyOutro = "Past the brambles the ground turns to salt and reed. Wren says the marsh folk once lived beyond it.",
            });
            c.routes.Add(new RouteData
            {
                id = "salt",
                name = "Salt Marsh Trail",
                emoji = "🧂",
                description = "Boardwalks over misty pools. Vipers hiss in the reeds.",
                baseDifficulty = 2,
                gatherChance = 0.28f,
                coinMult = 1.05f,
                clearReward = 40,
                clearXp = 28,
                unlockedByRouteId = "berry",
                monsterIds = new List<string> { "crab", "slime", "shaman" },
                gatherIngredientIds = new List<string> { "salt", "fish" },
                materialIds = new List<string> { "clay", "fieldstone" },
                chapter = 3,
                storyIntro = "Boardwalks over misty pools. The Bog Shamans have been stirring the fog, and the vipers are restless.",
                storyOutro = "Beyond the marsh, the Path drops into a cavern black as soot. You'll need a light. Fireflies drift above a glade to the east…",
            });
            c.routes.Add(new RouteData
            {
                id = "cavern",
                name = "Cavern Hunt",
                emoji = "🕳️",
                kind = RouteKind.Hunt,
                description = "Glowing tunnels, echoing wings, and something warm sleeping deeper.",
                baseDifficulty = 3,
                gatherChance = 0.15f,
                coinMult = 1.4f,
                clearReward = 55,
                clearXp = 36,
                unlockedByRouteId = "salt",
                monsterIds = new List<string> { "bat", "wolf", "shaman", "dragon" },
                gatherIngredientIds = new List<string> { "cave_egg", "truffle", "mushroom", "glowcap" },
                materialIds = new List<string> { "iron_scrap", "clay" },
                chapter = 4,
                requiredKeyItem = "glow_lantern",
                storyIntro = "With the Firefly Lantern held high, the Cavern's crystals wake around you. Something warm is sleeping deeper down.",
                storyOutro = "At the far end of the caves the Path climbs onto a ridge of red mud. Normal boots would sink. The bog-walkers of the Spore Mire might know a trick.",
            });
            c.routes.Add(new RouteData
            {
                id = "chili",
                name = "Chili Ridge",
                emoji = "🌶️",
                description = "Red hills where chilies grow wild. Quiet: a good place to gather.",
                baseDifficulty = 1,
                gatherChance = 0.45f,
                coinMult = 1.1f,
                clearReward = 40,
                clearXp = 45,
                unlockedByRouteId = "",
                monsterIds = new List<string> { "bat", "boar" },
                floors = 2,
                noBoss = true,
                enemyMult = 0.6f,
                floorDrops = new List<FloorDrop> { new FloorDrop { ingredientId = "mint", minFloor = 2, weight = 0.5f } },
                requiredFlag = "wren_fire_hint",
                lockHint = "Old Wren might know the way",
                gatherIngredientIds = new List<string> { "chili", "garlic", "wheat" },
                materialIds = new List<string> { "ember_coal" },
                requiredKeyItem = "",
                storyIntro = "Red hills where chilies grow wild. Not much lives up here: a good place to gather. Two floors, and an exit at the top.",
                storyOutro = "The ridge ends at a gate of black iron: the Ember Gate, locked tight. Wren remembers that the Wardens of the Old Watchtower kept its key.",
            });
            c.routes.Add(new RouteData
            {
                id = "ember",
                name = "Ember Hunt",
                emoji = "🔥",
                kind = RouteKind.Hunt,
                description = "Smouldering hollows where the drakes nest. Big risks, rich pickings.",
                baseDifficulty = 5,
                gatherChance = 0.15f,
                coinMult = 1.6f,
                clearReward = 90,
                clearXp = 60,
                unlockedByRouteId = "chili",
                monsterIds = new List<string> { "wolf", "shaman", "crab", "dragon" },
                gatherIngredientIds = new List<string> { "ember_meat", "fire_honey", "chili" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                chapter = 6,
                requiredKeyItem = "watch_key",
                hazard = Hazard.Heat,
                hazardPct = 0.012f,
                storyIntro = "The Watchtower Key turns with a groan. Beyond the Ember Gate the air shimmers; the heat will sap you. Pack something cooling.",
                storyOutro = "Past the fire fields, the Path climbs into ice: Frostfang Pass. Its cliffs are sheer. Only the Rangers of the Thornwood weave a rope strong enough.",
            });
            c.routes.Add(new RouteData
            {
                id = "frost",
                name = "Frostfang Pass",
                emoji = "❄️",
                description = "An icy climb where the wind howls. The cold slows every step.",
                baseDifficulty = 6,
                gatherChance = 0.24f,
                coinMult = 1.4f,
                clearReward = 115,
                clearXp = 75,
                unlockedByRouteId = "ember",
                monsterIds = new List<string> { "wolf", "crab", "shaman" },
                gatherIngredientIds = new List<string> { "ginger", "frost_berry", "salt" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                chapter = 7,
                requiredKeyItem = "climbing_rope",
                hazard = Hazard.Cold,
                hazardPct = 0.012f,
                storyIntro = "Thornsilk rope over your shoulder, you climb. The cold bites and slows every step; warm food helps.",
                storyOutro = "At the top of the pass lies the Hollow Heart. The Everflame is a single blue spark. Nothing you carry burns hot enough to relight it. Grandmother Hob's lost recipe might.",
            });
            c.routes.Add(new RouteData
            {
                id = "heart",
                name = "The Hollow Heart",
                emoji = "💛",
                kind = RouteKind.Hunt,
                description = "The source of the fog, and of every hearth's fire. Something in the mist doesn't want it lit.",
                baseDifficulty = 7,
                gatherChance = 0.12f,
                coinMult = 2f,
                clearReward = 200,
                clearXp = 120,
                unlockedByRouteId = "frost",
                monsterIds = new List<string> { "wolf", "shaman", "bat", "dragon" },
                gatherIngredientIds = new List<string> { "ember_meat", "frost_berry", "fire_honey" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                chapter = 8,
                requiredKeyItem = "everflame_recipe",
                storyIntro = "With the Everflame Recipe in your pack, you step into the Hollow Heart. The fog is so thick you can barely see your own hands; glowcap dishes from the Glowcap Grotto will light your way. Somewhere in the dark, the great drake that feeds on the fog wakes.",
                storyOutro = "You stir Grandmother Hob's stew into the embers and the Everflame roars back to life. Across the valley the hearths are lit again, one by one. Wren is waiting at camp with a bowl of something warm. The end, for now.",
            });
            c.routes.Add(new RouteData
            {
                id = "harerun",
                name = "Hare's Run",
                emoji = "🐇",
                description = "Windy hillsides where everything runs at twice the speed — you included.",
                baseDifficulty = 1,
                gatherChance = 0.3f,
                coinMult = 1.4f,
                clearReward = 35,
                clearXp = 20,
                unlockedByRouteId = "berry",
                monsterIds = new List<string> { "boar", "wolf", "slime" },
                gatherIngredientIds = new List<string> { "wheat", "berries", "herbs" },
                materialIds = new List<string> { "fieldstone", "timber" },
                mutators = new List<Mutator> { Mutator.Swift },
                storyIntro = "Old Wren swears the hares here are enchanted. Everything on these hills moves at double speed, so keep your feet light.",
                storyOutro = "You outran the hares! Wren laughs that you're the first cook to come back with your hat still on.",
            });
            c.routes.Add(new RouteData
            {
                id = "firefly",
                name = "Firefly Glade",
                emoji = "✨",
                description = "A hush of glowing grass where fireflies nest. The Lantern Bees guard it fiercely.",
                baseDifficulty = 2,
                gatherChance = 0.32f,
                coinMult = 1.1f,
                clearReward = 35,
                clearXp = 22,
                unlockedByRouteId = "salt",
                monsterIds = new List<string> { "bat", "slime" },
                gatherIngredientIds = new List<string> { "nettle", "mint", "berries" },
                materialIds = new List<string> { "clay", "fieldstone" },
                rewardKeyItem = "glow_lantern",
                storyIntro = "The fireflies here sleep in jars left by an old lantern-maker. Clear the glade and one of those jars is yours.",
                storyOutro = "You found the Firefly Lantern! Its warm glow will light the Cavern on the Ember Path.",
            });
            c.routes.Add(new RouteData
            {
                id = "spore",
                name = "Spore Mire",
                emoji = "🍄",
                kind = RouteKind.Hunt,
                description = "A bog of puffing spore-caps. The air itself is poison; nettle dishes help.",
                baseDifficulty = 3,
                gatherChance = 0.22f,
                coinMult = 1.3f,
                clearReward = 55,
                clearXp = 34,
                unlockedByRouteId = "cavern",
                monsterIds = new List<string> { "slime", "crab", "shaman" },
                gatherIngredientIds = new List<string> { "mushroom", "nettle", "fish" },
                materialIds = new List<string> { "iron_scrap", "clay" },
                rewardKeyItem = "bog_boots",
                hazard = Hazard.Poison,
                hazardPct = 0.012f,
                mutators = new List<Mutator> { Mutator.Swarm },
                storyIntro = "The bog-walkers left their boots at the bottom of the mire when the spores came. Pack an antidote: nettle soup, skewers or wraps.",
                storyOutro = "You found the Bog-Walker Boots! Chili Ridge's red mud can't swallow you now.",
            });
            c.routes.Add(new RouteData
            {
                id = "glowcap",
                name = "Glowcap Grotto",
                emoji = "🕯️",
                kind = RouteKind.Hunt,
                description = "A pitch-black grotto where glowcap mushrooms sprout. Without light you'll see only a few steps ahead.",
                baseDifficulty = 4,
                gatherChance = 0.3f,
                coinMult = 1.35f,
                clearReward = 70,
                clearXp = 42,
                unlockedByRouteId = "cavern",
                monsterIds = new List<string> { "bat", "crab", "slime" },
                gatherIngredientIds = new List<string> { "glowcap", "cave_egg", "truffle" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                storyIntro = "The Grotto is utterly dark. Glowcaps grow here and nowhere else: cook them and your eyes will glow for a day. You'll need that light in the Hollow Heart.",
                storyOutro = "Your bag glows softly with glowcaps. Cook them into broth, kebabs or sandwiches to light the darkest places.",
            });
            c.routes.Add(new RouteData
            {
                id = "watchtower",
                name = "Old Watchtower",
                emoji = "🏰",
                kind = RouteKind.Hunt,
                description = "The Wardens' old post, overrun by boars and wolves. Its doors only open for a Warden.",
                baseDifficulty = 4,
                gatherChance = 0.18f,
                coinMult = 1.35f,
                clearReward = 75,
                clearXp = 45,
                unlockedByRouteId = "chili",
                monsterIds = new List<string> { "boar", "wolf", "shaman" },
                gatherIngredientIds = new List<string> { "garlic", "salt", "herbs" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                rewardKeyItem = "watch_key",
                classLocked = true,
                storyIntro = "Only a Warden's crest opens the tower gate. The Ember Gate's key still hangs in the captain's room at the top.",
                storyOutro = "You found the Watchtower Key! It fits the Ember Gate at the end of Chili Ridge.",
            });
            c.routes.Add(new RouteData
            {
                id = "thunder",
                name = "Thunderpuff Hills",
                emoji = "⛈️",
                kind = RouteKind.Hunt,
                description = "Storm-charged hills. Every area hit crackles with double power, and the monsters come in droves.",
                baseDifficulty = 5,
                gatherChance = 0.24f,
                coinMult = 1.5f,
                clearReward = 95,
                clearXp = 58,
                unlockedByRouteId = "chili",
                monsterIds = new List<string> { "boar", "bat", "wolf", "shaman" },
                gatherIngredientIds = new List<string> { "chili", "garlic", "mint" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                mutators = new List<Mutator> { Mutator.BigBangs, Mutator.Swarm },
                storyIntro = "Thunder rolls over the hills and every sweeping blow lands like a lightning strike. Bring something that hits a crowd: there will be one.",
                storyOutro = "The storm has blown itself out. Somewhere a Warden is very jealous of how many monsters you flattened.",
            });
            c.routes.Add(new RouteData
            {
                id = "canopy",
                name = "Thornwood Canopy",
                emoji = "🌳",
                description = "A forest that lives in the treetops. Only a Ranger can find the paths between the branches.",
                baseDifficulty = 5,
                gatherChance = 0.3f,
                coinMult = 1.25f,
                clearReward = 90,
                clearXp = 55,
                unlockedByRouteId = "ember",
                monsterIds = new List<string> { "bat", "crab", "wolf" },
                gatherIngredientIds = new List<string> { "ginger", "berries", "fire_honey" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                rewardKeyItem = "climbing_rope",
                classLocked = true,
                requiredClass = ClassId.Ranger,
                storyIntro = "The Thornwood Rangers left a spool of thornsilk in their highest lookout. Only a Ranger can climb that far.",
                storyOutro = "You found the Thornsilk Rope! Now Frostfang Pass can be climbed.",
            });
            c.routes.Add(new RouteData
            {
                id = "hive",
                name = "Wild Hive",
                emoji = "🍯",
                kind = RouteKind.Hunt,
                description = "A buzzing, honey-drunk hive. Everything moves at triple speed.",
                baseDifficulty = 6,
                gatherChance = 0.26f,
                coinMult = 1.7f,
                clearReward = 125,
                clearXp = 75,
                unlockedByRouteId = "ember",
                monsterIds = new List<string> { "bat", "boar", "crab" },
                gatherIngredientIds = new List<string> { "fire_honey", "berries", "mint" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                mutators = new List<Mutator> { Mutator.Frantic },
                storyIntro = "The ember-bees have gone honey-mad. Everything in the hive, you included, moves three times as fast. Hold on to your ladle.",
                storyOutro = "Sticky, dizzy and rich in fire honey. Wren says she's never seen anyone come out of the Wild Hive walking straight.",
            });
            c.routes.Add(new RouteData
            {
                id = "larder",
                name = "Forgotten Larder",
                emoji = "🥫",
                kind = RouteKind.Hunt,
                description = "Grandmother Hob's cellar under the old inn. Only a Cook knows the larder's secret door.",
                baseDifficulty = 6,
                gatherChance = 0.26f,
                coinMult = 1.5f,
                clearReward = 110,
                clearXp = 68,
                unlockedByRouteId = "frost",
                monsterIds = new List<string> { "slime", "boar", "shaman" },
                gatherIngredientIds = new List<string> { "truffle", "cave_egg", "ginger" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                rewardKeyItem = "everflame_recipe",
                classLocked = true,
                requiredClass = ClassId.Cook,
                mutators = new List<Mutator> { Mutator.GlassHearts },
                storyIntro = "Somewhere among the jars and barrels is Grandmother Hob's recipe book. Only a Cook would know where she hid it.",
                storyOutro = "You found the Everflame Recipe! The way into the Hollow Heart is open.",
            });
            c.routes.Add(new RouteData
            {
                id = "frosthollow",
                name = "Frost Hollow",
                emoji = "❄️",
                kind = RouteKind.Hunt,
                description = "Past the Hollow, where the cold never lifts. The Hoarfrost Alpha prowls at the bottom.",
                baseDifficulty = 3,
                gatherChance = 0.25f,
                coinMult = 1.3f,
                clearReward = 60,
                clearXp = 90,
                unlockedByRouteId = "",
                monsterIds = new List<string> { "wolf", "crab", "bat" },
                floors = 3,
                bossIds = new List<string> { "hoarfrost_alpha" },
                floorMonsters = new List<FloorMonster> { new FloorMonster { monsterId = "shaman", minFloor = 2 } },
                floorDrops = new List<FloorDrop>
                {
                    new FloorDrop { ingredientId = "starmoss", minFloor = 1 },
                    new FloorDrop { ingredientId = "wild_honeycomb", minFloor = 2 },
                    new FloorDrop { ingredientId = "golden_wheat", minFloor = 2 },
                },
                requiredFlag = "wren_frost",
                lockHint = "Old Wren knows the way",
                gatherIngredientIds = new List<string> { "frost_berry", "ginger" },
                materialIds = new List<string> { "ember_coal", "iron_scrap" },
                hazard = Hazard.Cold,
                hazardPct = 0.016f,
                storyIntro = "Bitter cold, three floors deep. The Hoarfrost Alpha prowls at the bottom, and it can't abide fire.",
                storyOutro = "You made it out of Frost Hollow with frostberries to spare. Try them in a crumble.",
            });
            c.routes.Add(new RouteData
            {
                id = "nowhere",
                name = "???",
                emoji = "🌀",
                kind = RouteKind.Hunt,
                description = "Somewhere strange. You don't remember getting here.",
                gatherChance = 0f,
                clearReward = 0,
                clearXp = 0,
                monsterIds = new List<string> { "slime", "bat", "crab", "wolf", "shaman" },
                floors = 1,
            });
            // ── keyItems (5) ──
            c.keyItems.Add(new KeyItemData
            {
                id = "glow_lantern",
                name = "Firefly Lantern",
                emoji = "🏮",
                description = "A jar of sleepy fireflies. Bright enough to light the Cavern's dark.",
            });
            c.keyItems.Add(new KeyItemData
            {
                id = "bog_boots",
                name = "Bog-Walker Boots",
                emoji = "🥾",
                description = "Waxed boots that never sink. The only way across Chili Ridge's red mud.",
            });
            c.keyItems.Add(new KeyItemData
            {
                id = "watch_key",
                name = "Watchtower Key",
                emoji = "🗝️",
                description = "A heavy iron key from the Wardens' old post. It fits the Ember Gate.",
            });
            c.keyItems.Add(new KeyItemData
            {
                id = "climbing_rope",
                name = "Thornsilk Rope",
                emoji = "🪢",
                description = "Woven from canopy silk by the Thornwood Rangers. Strong enough for Frostfang's cliffs.",
            });
            c.keyItems.Add(new KeyItemData
            {
                id = "everflame_recipe",
                name = "Everflame Recipe",
                emoji = "📜",
                description = "Grandmother Hob's stew, hot enough to relight the Everflame itself.",
            });
        }
    }
}
