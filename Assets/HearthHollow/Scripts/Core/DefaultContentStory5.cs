// Hearth & Hollow — story content, part 2 (contentVersion 5): the Darkness hazard, dungeon mutators and four more
// side dungeons, bringing the map to 8 story chapters + 10 side dungeons. Pure C#.
//
//   Glowcap Grotto  (after ch4, DARKNESS)           the only place glowcaps grow → glowcap dishes light your way
//   Hare's Run      (after ch2, Swift ×2 speed)      a breezy, coin-rich warm-up
//   Thunderpuff Hills (after ch5, Big Bangs + Swarm) area attacks hit twice as hard, twice the monsters
//   Wild Hive       (after ch6, Frantic ×3 speed)    everything is very, very fast
// Also: The Hollow Heart (ch8) is now dark with fog (pack glowcap dishes), Spore Mire swarms, Frost Hollow has giants,
// and the Forgotten Larder has Glass Hearts (everyone hits twice as hard).
//
// Like ApplyStory, this only ADDS what's missing and only fills fields that are still empty, so edits made in the
// content asset are kept.
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        const string HeartIntroV4 = "With the Everflame Recipe in your pack, you step into the Hollow Heart. The fog thickens, and the great drake that feeds on it wakes.";

        static void ApplyStory5(GameContent c)
        {
            // ───── darkness: a new ingredient + one counter meal per station ─────
            IngOnce(c, "glowcap", "Glowcap", "🍄", 2);
            RecOnce(c, "glowcap_broth", "Glowcap Broth", "🫖", Station.Pot, new[] { Cost("glowcap", 2), Cost("herbs", 1) },
                new[] { Mod(BuffStat.MaxHp, 20), Mod(BuffStat.XpPct, 0.1f) }, new[] { "stone", "veg" }, Hazard.Darkness,
                "A softly glowing broth. Your eyes shine for hours afterwards. Lights up the dark.");
            RecOnce(c, "glowcap_kebab", "Glowcap Kebab", "🍢", Station.BBQ, new[] { Cost("glowcap", 1), Cost("mushroom", 1), Cost("garlic", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.45f), Mod(BuffStat.CritPct, 0.08f) }, new[] { "smoky", "veg" }, Hazard.Darkness,
                "Glowcaps charred until they sparkle. Lights up the dark.");
            RecOnce(c, "glowcap_sandwich", "Glow-in-the-Dark Sandwich", "🥪", Station.Sandwich, new[] { Cost("glowcap", 1), Cost("cave_egg", 1), Cost("wheat", 1) },
                new[] { Mod(BuffStat.DamagePct, 0.3f), Mod(BuffStat.MaxHp, 12), Mod(BuffStat.GatherZonePct, 0.15f) }, new[] { "stone" }, Hazard.Darkness,
                "Glow-egg and glowcap on a crusty roll. Lights up the dark.");

            // glowcaps also grow in the lantern-lit Cavern, so the story never dead-ends
            var cavern = c.Route("cavern");
            if (cavern != null && cavern.gatherIngredientIds != null && !cavern.gatherIngredientIds.Contains("glowcap")) cavern.gatherIngredientIds.Add("glowcap");

            // ───── the finale is shrouded in fog ─────
            var heart = c.Route("heart");
            if (heart != null && heart.hazard == Hazard.None)
            {
                heart.hazard = Hazard.Darkness;
                if (heart.storyIntro == HeartIntroV4)
                    heart.storyIntro = "With the Everflame Recipe in your pack, you step into the Hollow Heart. The fog is so thick you can barely see your own hands; " +
                                       "glowcap dishes from the Glowcap Grotto will light your way. Somewhere in the dark, the great drake that feeds on the fog wakes.";
            }

            // ───── mutators on existing side dungeons ─────
            AddMutator(c, "spore", Mutator.Swarm);
            AddMutator(c, "frosthollow", Mutator.Giants);
            AddMutator(c, "larder", Mutator.GlassHearts);

            // ───── four more side dungeons ─────
            RouteOnce(c, new RouteData
            {
                id = "harerun", name = "Hare's Run", emoji = "🐇", kind = RouteKind.Trail,
                description = "Windy hillsides where everything runs at twice the speed — you included.",
                baseDifficulty = 1, gatherChance = 0.3f, coinMult = 1.4f, clearReward = 35, clearXp = 20,
                unlockedByRouteId = "berry",
                mutators = new List<Mutator> { Mutator.Swift },
                monsterIds = L("boar", "wolf", "slime"),
                gatherIngredientIds = L("wheat", "berries", "herbs"),
                storyIntro = "Old Wren swears the hares here are enchanted. Everything on these hills moves at double speed, so keep your feet light.",
                storyOutro = "You outran the hares! Wren laughs that you're the first cook to come back with your hat still on.",
            });
            RouteOnce(c, new RouteData
            {
                id = "glowcap", name = "Glowcap Grotto", emoji = "🕯️", kind = RouteKind.Hunt,
                description = "A pitch-black grotto where glowcap mushrooms sprout. Without light you'll see only a few steps ahead.",
                baseDifficulty = 4, gatherChance = 0.3f, coinMult = 1.35f, clearReward = 70, clearXp = 42,
                unlockedByRouteId = "cavern",
                hazard = Hazard.Darkness,
                monsterIds = L("bat", "crab", "slime"),
                gatherIngredientIds = L("glowcap", "cave_egg", "truffle"),
                storyIntro = "The Grotto is utterly dark. Glowcaps grow here and nowhere else: cook them and your eyes will glow for a day. You'll need that light in the Hollow Heart.",
                storyOutro = "Your bag glows softly with glowcaps. Cook them into broth, kebabs or sandwiches to light the darkest places.",
            });
            RouteOnce(c, new RouteData
            {
                id = "thunder", name = "Thunderpuff Hills", emoji = "⛈️", kind = RouteKind.Hunt,
                description = "Storm-charged hills. Every area hit crackles with double power, and the monsters come in droves.",
                baseDifficulty = 5, gatherChance = 0.24f, coinMult = 1.5f, clearReward = 95, clearXp = 58,
                unlockedByRouteId = "chili",
                mutators = new List<Mutator> { Mutator.BigBangs, Mutator.Swarm },
                monsterIds = L("boar", "bat", "wolf", "shaman"),
                gatherIngredientIds = L("chili", "garlic", "mint"),
                storyIntro = "Thunder rolls over the hills and every sweeping blow lands like a lightning strike. Bring something that hits a crowd: there will be one.",
                storyOutro = "The storm has blown itself out. Somewhere a Warden is very jealous of how many monsters you flattened.",
            });
            RouteOnce(c, new RouteData
            {
                id = "hive", name = "Wild Hive", emoji = "🍯", kind = RouteKind.Hunt,
                description = "A buzzing, honey-drunk hive. Everything moves at triple speed.",
                baseDifficulty = 6, gatherChance = 0.26f, coinMult = 1.7f, clearReward = 125, clearXp = 75,
                unlockedByRouteId = "ember",
                mutators = new List<Mutator> { Mutator.Frantic },
                monsterIds = L("bat", "boar", "crab"),
                gatherIngredientIds = L("fire_honey", "berries", "mint"),
                storyIntro = "The ember-bees have gone honey-mad. Everything in the hive, you included, moves three times as fast. Hold on to your ladle.",
                storyOutro = "Sticky, dizzy and rich in fire honey. Wren says she's never seen anyone come out of the Wild Hive walking straight.",
            });

            // story chapters first, then side dungeons in unlock order
            var order = new List<string> { "meadow", "berry", "salt", "cavern", "chili", "ember", "frost", "heart",
                                           "harerun", "firefly", "spore", "glowcap", "watchtower", "thunder", "canopy", "hive", "larder", "frosthollow" };
            c.routes.Sort((a, b) =>
            {
                int ia = order.IndexOf(a.id), ib = order.IndexOf(b.id);
                if (ia < 0) ia = 1000; if (ib < 0) ib = 1000;
                return ia.CompareTo(ib);
            });
        }

        static void AddMutator(GameContent c, string routeId, Mutator m)
        {
            var r = c.Route(routeId);
            if (r == null) return;
            if (r.mutators == null) r.mutators = new List<Mutator>();
            if (r.mutators.Count == 0) r.mutators.Add(m);   // only routes that have none yet (keeps your edits)
        }
    }
}
