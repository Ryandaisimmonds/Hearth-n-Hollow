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
            s.StartRun("meadow"); s.DungeonExit(); s.ReturnToHearth();
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

            // floor-5 boss → the fire hint → Chili Ridge
            s.StartRun("meadow");
            for (int i = 0; i < 4; i++) s.DungeonDescend();
            s.DungeonEnemyKilled(s.DungeonEnemy("bramble_sow", true, true));
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.HearHint, s.Wren);
            Assert.IsTrue(s.WrenTalkDue);
            s.GiveWrenFireHint();
            Assert.AreEqual(WrenStep.Ridge, s.Wren);

            // the first Ridge trip: one short, easy floor full of chilies
            Assert.IsTrue(s.StartRun("chili"));
            Assert.IsTrue(s.Run.firstRidge);
            Assert.IsTrue(s.DungeonNoLadder);
            Assert.IsFalse(s.DungeonDescend());
            var easy = s.DungeonEnemy("bat", false, false);
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(WrenStep.RidgeBack, s.Wren);
            Assert.IsTrue(s.WrenTalkDue);
            s.WrenTalk();
            Assert.AreEqual(WrenStep.SeasonMeal, s.Wren);
            Assert.IsTrue(s.StartRun("chili"));
            Assert.IsFalse(s.Run.firstRidge, "only the first trip is the short one");
            Assert.Greater(s.DungeonEnemy("bat", false, false).maxHp, easy.maxHp);
            s.DungeonExit(); s.ReturnToHearth();

            // season a meal with chili → take it to the Alpha
            Economy.AddToPantry(s.Profile, "chili", 1);
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = r.id });
            string why;
            Assert.IsTrue(s.SeasonMeal(s.Profile.mealPantry[s.Profile.mealPantry.Count - 1], "chili", out why), why);
            Assert.AreEqual(WrenStep.Alpha, s.Wren);
            StringAssert.Contains("Hoarfrost Alpha", s.WrenObjective);
        }

        [Test]
        public void OldSaves_LandOnTheRightStep()
        {
            var p = Economy.NewProfile(_c);
            p.saveVersion = 13; p.metWren = true; p.arcStage = 2;
            Progression.RecordBoss(p, "meadow", 5);
            Routes.SetFlag(p, DefaultContent.WrenFireHint);
            var s = new GameSession(_c, p, 72);
            Assert.AreEqual(WrenStep.Ridge, s.Wren);
        }
    }
}
