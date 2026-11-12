// Ticket 66: a bot plays the whole vertical slice through the real GameSession: the fall, Old Wren, cooking,
// packing the tray, Meadow Hollow, Chili Ridge and Frost Hollow, until Wren's thread is done. It only does what a
// player could (talk to Wren when she wants a word, cook what the pantry allows, pack the tray, go where the
// objective says) and fails if it ever gets stuck: no way to set out, nothing to cook, an objective naming nowhere
// you can go, or too long without Wren's story moving on.
using System.Collections.Generic;
using System.Text;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class SliceAutoplayTests
    {
        const int Budget = 500, StallLimit = 80;

        [TestCase(11)]
        [TestCase(2026)]
        [TestCase(777)]
        public void TheBot_PlaysTheWholeSlice_WithoutGettingStuck(int seed)
        {
            bool was = Slice.On;
            try
            {
                Slice.On = true;
                var c = DefaultContent.Create();
                var s = new GameSession(c, null, seed);
                var log = new StringBuilder();
                s.EventRaised += e => { if (e.type == GameEventType.Info) log.AppendLine("  info: " + e.text); };
                var rng = new System.Random(seed);
                int lastStep = (int)s.Wren, sinceProgress = 0, runs = 0;

                Assert.IsTrue(s.FallDue, "a new slice game opens with the fall");
                for (int step = 0; step < Budget && s.Wren != WrenStep.Done; step++)
                {
                    string did = Act(s, rng, ref runs);
                    log.AppendLine(step + " [" + s.Phase + " · Wren " + s.Wren + "] " + did);
                    if ((int)s.Wren != lastStep) { lastStep = (int)s.Wren; sinceProgress = 0; }
                    else if (++sinceProgress > StallLimit) Assert.Fail("Stuck at Wren step " + s.Wren + " (objective: " + s.WrenObjective + ")\n" + Tail(log));
                    if (did.StartsWith("STUCK")) Assert.Fail(did + "\n" + Tail(log));
                }
                Assert.AreEqual(WrenStep.Done, s.Wren, "the slice didn't finish in " + Budget + " actions\n" + Tail(log));
                Assert.IsTrue(s.Profile.clearedRouteIds.Contains("meadow") && s.Profile.clearedRouteIds.Contains("frosthollow"), "Meadow and Frost Hollow cleared");
                Assert.Less(runs, 40, "the slice took " + runs + " runs");
            }
            finally { Slice.On = was; }
        }

        static string Tail(StringBuilder sb) { var t = sb.ToString(); return t.Length > 4000 ? "…" + t.Substring(t.Length - 4000) : t; }

        /// One player action. Returns what it did ("STUCK …" when there's nothing a player could do).
        static string Act(GameSession s, System.Random rng, ref int runs)
        {
            var c = s.Content;
            if (s.FallDue) { runs++; return s.BeginTheFall() ? "fell into the nameless dungeon" : "STUCK: the fall wouldn't start"; }

            if (s.InDungeon)
            {
                if (s.InFall) { s.FallFogBite(); return "the fog bites (hp " + (s.Run != null ? s.Run.hp : 0) + ")"; }
                // fight a few of the floor's monsters for their loot
                var pool = DungeonRules.NormalPool(c, s.Run);
                for (int i = 0; i < 4 && pool.Count > 0 && s.InDungeon; i++) s.DungeonEnemyKilled(s.DungeonEnemy(pool[rng.Next(pool.Count)], false, false));
                if (!s.InDungeon) return "fought";
                // and forage a spot or two (every area has some)
                for (int i = 0; i < 2 && s.InDungeon; i++)
                    if (s.DungeonStartGather())
                    {   // a decent player: three picks in four land in the zone
                        var g = s.Run.gather;
                        float at = rng.NextDouble() < 0.75 ? g.safeCenter + (float)(rng.NextDouble() - 0.5) * 0.8f * g.safeWidth : (g.safeCenter > 0.5f ? 0.02f : 0.98f);
                        s.ResolveGather(at);
                        if (s.Phase == Phase.Gather) return "STUCK: the forage never finished";
                    }
                if (s.DungeonBossFloor)
                {
                    string boss = DungeonRules.BossMonster(c, s.Run);
                    s.DungeonEnemyKilled(s.DungeonEnemy(boss, true, true));
                    s.DungeonExit();
                    return "beat the boss " + boss + " and left";
                }
                if (s.DungeonEndExit) { s.DungeonExit(); return "took the exit at the end"; }
                if (s.DungeonDescend()) return "went down to floor " + s.Run.floor;
                s.DungeonExit();
                return "left (no way down)";
            }

            if (s.Phase != Phase.Hearth)
            {
                s.ReturnToHearth();
                var sb = new StringBuilder("back to the hearth (pantry:");
                foreach (var st in s.Profile.pantry) if (st.count > 0) sb.Append(" " + st.ingredientId + "×" + st.count);
                return sb.Append(")").ToString();
            }

            // Old Wren first, whenever she has something to say
            if (s.WrenFireHintDue) { s.GiveWrenFireHint(); return "heard Wren's fire hint"; }
            if (s.WrenTalkDue || s.Wren == WrenStep.ShowMeal) { s.WrenTalk(); return "talked to Wren"; }

            if (s.Wren == WrenStep.CookMeal)
            {
                var r = Cookable(s);
                if (r == null) return "STUCK: Wren wants a meal and nothing can be cooked";
                s.Cook(r.id, new CookChoice { grade = 1 });
                return "cooked " + r.id;
            }

            if (s.Wren == WrenStep.SeasonMeal)
            {
                if (Economy.PantryCount(s.Profile, "chili") > 0)
                {
                    if (s.Profile.mealPantry.Count == 0) { var r = Cookable(s); if (r != null) s.Cook(r.id, new CookChoice { grade = 1 }); }
                    if (s.Profile.mealPantry.Count > 0)
                    {
                        string why;
                        if (s.SeasonMeal(s.Profile.mealPantry[0], "chili", out why)) return "seasoned a meal with chili";
                        // (no luck this time: go and get more, like a player would; the stall check catches a real dead end)
                    }
                }
            }

            // cook while the pantry allows (a player stocks up), then pack the tray
            int cooked = 0;
            Recipe0:
            var cr = s.Profile.mealPantry.Count < 6 ? Cookable(s) : null;
            if (cr != null && cooked < 3) { s.Cook(cr.id, new CookChoice { grade = 1 }); cooked++; goto Recipe0; }
            int max = c.tuning.traySize, guard = 0;
            while (s.Profile.tray.Count < max && guard++ < 10)
            {
                if (s.Profile.mealPantry.Count > 0 && s.PackMeal(0)) continue;
                var always = c.recipes.Find(x => x.always);
                if (always == null || !s.PackAlwaysMeal(always.id)) break;
            }

            string block = s.WrenSetOutBlock ?? s.TrayBlock;
            if (block != null) return "STUCK: can't set out: " + block;

            // go where the objective says (or, for chili, to the ridge)
            string route = RouteFor(s);
            if (route == null) return "STUCK: the objective names nowhere you can go: " + s.WrenObjective;
            runs++;
            if (!s.StartRun(route)) return "STUCK: couldn't start " + route + " (" + Routes.EnterBlock(c, s.Profile, route, s.Profile.selectedClass) + ")";
            return "set out for " + route + " (" + string.Join(", ", s.Profile.packed.ConvertAll(m => m.recipeId).ToArray()) + ")";
        }

        static RecipeData Cookable(GameSession s)
        {
            var c = s.Content;
            string why;
            // while Wren wants a meal seasoned with chili, don't cook the chili away
            bool keepChili = s.Wren == WrenStep.SeasonMeal;
            int chili = Economy.PantryCount(s.Profile, "chili");
            return c.recipes.Find(x => !x.always && x.ingredients.Count > 0 && Kitchen.Unlocked(c, s.Profile, x) && Cooking.CanCook(c, s.Profile, x, new CookChoice(), out why)
                                      && !(keepChili && x.ingredients.Exists(k => k.ingredientId == "chili" && k.count >= chili)));   // leave one to season with
        }

        /// The slice area the objective talks about; the season step needs chilies from the ridge.
        static string RouteFor(GameSession s)
        {
            var c = s.Content;
            if (s.Wren == WrenStep.SeasonMeal && s.IsRouteUnlocked("chili")) return "chili";
            string obj = s.WrenObjective ?? "";
            foreach (var id in Slice.RouteIds)
            {
                var r = c.Route(id);
                if (r != null && obj.Contains(r.name) && s.IsRouteUnlocked(id)) return id;
            }
            foreach (var id in Slice.RouteIds) if (s.IsRouteUnlocked(id)) return id;   // nothing named: anywhere open
            return null;
        }
    }
}
