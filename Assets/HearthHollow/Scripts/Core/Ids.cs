// Hearth & Hollow — refactor 05: the ids the rules (and tests) name directly, in one place, so a typo is a compile
// error instead of a silent miss. The content (Scripts/Core/Content) defines what each id IS; IdsTests checks every
// constant here exists there. Ids the editor builders key art / asset tables by stay as strings in those tables.
namespace HearthHollow.Core
{
    public static class Ids
    {
        /// Areas (RouteData.id). The slice's three, the fall, then the rest of the world.
        public static class Area
        {
            public const string Meadow = "meadow", Chili = "chili", FrostHollow = "frosthollow", Fall = "nowhere";
            public const string Berry = "berry", Salt = "salt", Cavern = "cavern", Ember = "ember", Frost = "frost", Heart = "heart",
                                HareRun = "harerun", Firefly = "firefly", Spore = "spore", Glowcap = "glowcap", Watchtower = "watchtower",
                                Thunder = "thunder", Canopy = "canopy", Hive = "hive", Larder = "larder";
        }

        /// Monsters (MonsterData.id).
        public static class Monster
        {
            public const string Slime = "slime", Boar = "boar", Bat = "bat", Crab = "crab", Wolf = "wolf", Shaman = "shaman", Dragon = "dragon";
            public const string BrambleSow = "bramble_sow", HoarfrostAlpha = "hoarfrost_alpha", RidgeDrake = "ridge_drake";
        }

        /// Ingredients and building materials (IngredientData.id).
        public static class Ingredient
        {
            public const string Herbs = "herbs", Wheat = "wheat", Berries = "berries", Mushroom = "mushroom", Salt = "salt", Fish = "fish",
                                CaveEgg = "cave_egg", Truffle = "truffle", Chili = "chili", Garlic = "garlic", EmberMeat = "ember_meat",
                                FireHoney = "fire_honey", Nettle = "nettle", Mint = "mint", Ginger = "ginger", FrostBerry = "frost_berry",
                                Glowcap = "glowcap", Starmoss = "starmoss", WildHoneycomb = "wild_honeycomb", GoldenWheat = "golden_wheat";
            public const string Timber = "timber", Fieldstone = "fieldstone", Clay = "clay", IronScrap = "iron_scrap", EmberCoal = "ember_coal";
        }

        /// Dishes (RecipeData.id).
        public static class Recipe
        {
            public const string HerbStew = "herb_stew", HerbStew2 = "herb_stew_2", BerryPorridge = "berry_porridge", MarshChowder = "marsh_chowder",
                                NettleSoup = "nettle_soup", TruffleRisotto = "truffle_risotto", LanternSoup = "lantern_soup", DrakeHotpot = "drake_hotpot",
                                GrilledFish = "grilled_fish", MushroomSkewer = "mushroom_skewer", MushroomSkewer2 = "mushroom_skewer_2",
                                TruffleCaps = "truffle_caps", GlowcapKebab = "glowcap_kebab", FirecrackerSkewer = "firecracker_skewer",
                                HoneyRibs = "honey_ribs", EmberSteak = "ember_steak", Bread = "bread", HerbButty = "herb_butty",
                                HerbButty2 = "herb_butty_2", BrambleJam = "bramble_jam", MarshButty = "marsh_butty", TrailWrap = "trail_wrap",
                                GlowEggSandwich = "glow_egg_sandwich", FrostberryToast = "frostberry_toast";
        }

        /// Story flags (Profile.storyFlags).
        public static class Flag
        {
            public const string WrenFireHint = "wren_fire_hint";   // Wren told you the Alpha is weak to Fire: Chili Ridge shows
            public const string WrenFrost = "wren_frost";          // Wren told you the way to Frost Hollow
        }
    }
}
