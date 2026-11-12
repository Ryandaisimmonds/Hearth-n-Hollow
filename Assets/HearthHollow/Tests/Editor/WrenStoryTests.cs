// Hearth & Hollow — edit-mode tests for ticket 35: Old Wren guides you through the slice, step by step.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class WrenStoryTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void TheWholeThread_InOrder()
        {
            var s = new GameSession(_c, null, 71);
            s.Profile.selectedClass = ClassId.Cook;
            Assert.AreEqual(WrenStep.Meet, s.Wren);
            Assert.IsTrue(s.WrenTalkDue, "Wren comes over at the start");
            s.WrenTalk();
            Assert.AreEqual(WrenStep.FirstDive, s.Wren);
            Assert.IsFalse(s.WrenTalkDue);

            // first dive → home → Wren: cook a meal
            s.StartRun(Ids.Area.Meadow); s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.AfterDive, s.Wren);
            Assert.IsTrue(s.WrenTalkDue);
            s.WrenTalk();
            Assert.AreEqual(WrenStep.CookMeal, s.Wren);

            // cook → show Wren
            var r = _c.recipes.Find(x => !x.always && x.ingredients.Count > 0 && Kitchen.Unlocked(_c, s.Profile, x) && Cooking.CanCook(_c, s.Profile, x, new CookChoice(), out string _));
            Assert.IsNotNull(r, "the starter food cooks something");
            Assert.IsNotNull(s.Cook(r.id, new CookChoice()));
            Assert.AreEqual(WrenStep.ShowMeal, s.Wren);
            Assert.IsFalse(s.WrenTalkDue, "you go and show her yourself");
            s.WrenTalk();
            Assert.AreEqual(WrenStep.FirstBoss, s.Wren);

            // the Sow at the bottom of the Meadow (floor 3) → the fire hint → Chili Ridge
            s.StartRun(Ids.Area.Meadow);
            for (int i = 0; i < 2; i++) s.DungeonDescend();
            s.DungeonEnemyKilled(s.DungeonEnemy(Ids.Monster.BrambleSow, true, true));
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.HearHint, s.Wren);
            Assert.IsTrue(s.WrenTalkDue);
            s.GiveWrenFireHint();
            Assert.AreEqual(WrenStep.Ridge, s.Wren);

            // Chili Ridge: two quiet floors with an exit at the top (ticket 47)
            Assert.IsTrue(s.StartRun(Ids.Area.Chili));
            Assert.IsFalse(s.Run.firstRidge, "no special first trip any more");
            Assert.IsTrue(s.DungeonDescend());
            Assert.IsTrue(s.DungeonEndExit);
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.RidgeBack, s.Wren);
            Assert.IsTrue(s.WrenTalkDue);
            s.WrenTalk();
            Assert.AreEqual(WrenStep.SeasonMeal, s.Wren);
            Assert.IsFalse(s.IsRouteUnlocked(Ids.Area.FrostHollow), "Frost Hollow waits for a Fire meal");

            // season a meal with chili → take it to the Alpha
            Economy.AddToPantry(s.Profile, Ids.Ingredient.Chili, 1);
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = r.id });
            string why;
            Assert.IsTrue(s.SeasonMeal(s.Profile.mealPantry[s.Profile.mealPantry.Count - 1], Ids.Ingredient.Chili, out why), why);
            Assert.AreEqual(WrenStep.Alpha, s.Wren);
            StringAssert.Contains("Hoarfrost Alpha", s.WrenObjective);
            Assert.IsTrue(s.IsRouteUnlocked(Ids.Area.FrostHollow), "Wren tells you the way");

            // the Alpha at the bottom of Frost Hollow → done
            Assert.IsTrue(s.StartRun(Ids.Area.FrostHollow));
            for (int i = 0; i < 2; i++) s.DungeonDescend();
            s.DungeonEnemyKilled(s.DungeonEnemy(Ids.Monster.HoarfrostAlpha, true, true));
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.Done, s.Wren);
        }

        [Test]
        public void OldSaves_LandOnTheRightStep()
        {
            var p = Economy.NewProfile(_c);
            p.saveVersion = 13; p.metWren = true; p.arcStage = 2;
            Progression.RecordBoss(p, Ids.Area.Meadow, 5);
            Routes.SetFlag(p, DefaultContent.WrenFireHint);
            var s = new GameSession(_c, p, 72);
            Assert.AreEqual(WrenStep.Ridge, s.Wren);
        }
    }
}
