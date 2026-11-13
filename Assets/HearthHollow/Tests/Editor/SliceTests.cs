// Hearth & Hollow — edit-mode tests for the vertical slice gate and the Features it answers, with the slice on and off.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class SliceTests
    {
        GameContent _c;
        bool _was;

        [SetUp] public void SetUp() { _c = DefaultContent.Create(); _was = Slice.On; Slice.On = true; }
        [TearDown] public void TearDown() { Slice.On = _was; }

        [Test]
        public void SliceOn_OnlyTheCookCanBeSelected()
        {
            var s = new GameSession(_c, null, 1);
            s.DevMaxEverything();   // every class joins
            s.SelectClass(ClassId.Warden);
            Assert.AreEqual(ClassId.Cook, s.Profile.selectedClass);
            Assert.IsTrue(s.ClassAvailable(ClassId.Cook));
            Assert.IsFalse(s.ClassAvailable(ClassId.Warden));
        }

        [Test]
        public void SliceOn_ASavedNonSliceClassFallsBackToTheCook()
        {
            var p = Economy.NewProfile(_c);
            foreach (var cd in _c.classes) if (!string.IsNullOrEmpty(cd.joinRoute)) p.clearedRouteIds.Add(cd.joinRoute);
            p.selectedClass = ClassId.Ranger;
            var s = new GameSession(_c, p, 1);
            Assert.AreEqual(ClassId.Cook, s.Profile.selectedClass);
        }

        [Test]
        public void SliceOn_OldRoutesCantBeEntered()
        {
            var s = new GameSession(_c, null, 1);
            s.DevGiveKeyItems();
            Assert.IsFalse(s.StartRun(Ids.Area.Cavern));
            Assert.IsTrue(Slice.ShowsRoute(Ids.Area.Meadow));
            Assert.IsFalse(Slice.ShowsRoute(Ids.Area.Cavern));
        }

        [Test]
        public void SliceOn_NoCampEvents()
        {
            var p = Economy.NewProfile(_c);
            for (int seed = 1; seed < 60; seed++)
                Assert.AreEqual(CampEvent.None, Economy.RollCamp(_c, p, new Rng(seed)).campEvent, "seed " + seed);
        }

        [Test]
        public void SliceOff_EverythingIsBack()
        {
            Slice.On = false;
            var s = new GameSession(_c, null, 1);
            s.DevMaxEverything();
            s.SelectClass(ClassId.Warden);
            Assert.AreEqual(ClassId.Warden, s.Profile.selectedClass);
            Assert.IsTrue(Slice.ShowsRoute(Ids.Area.Cavern));
        }

        [Test]
        public void SliceOff_CampEventsCanRoll()
        {
            Slice.On = false;
            var p = Economy.NewProfile(_c);
            bool any = false;
            for (int seed = 1; seed < 200 && !any; seed++) any = Economy.RollCamp(_c, p, new Rng(seed)).campEvent != CampEvent.None;
            Assert.IsTrue(any);
        }

        [Test]
        public void SliceOn_FeaturesAreTheSlices()
        {
            Assert.IsTrue(Features.TownIsHome && Features.OpensWithFall && Features.WrenGuides && Features.FullTrayToSetOut
                && Features.HearthGrowsWithAreas && Features.SlimRouteBoard);
            Assert.IsFalse(Features.AllRoutes || Features.AllClasses || Features.AllTrinkets || Features.AllIngredients
                || Features.ThirdTrinketSlot || Features.Mutators || Features.CampEvents || Features.ClassSelect);
        }

        [Test]
        public void SliceOff_FeaturesAreTheFullGames()
        {
            Slice.On = false;
            Assert.IsFalse(Features.TownIsHome || Features.OpensWithFall || Features.WrenGuides || Features.FullTrayToSetOut
                || Features.HearthGrowsWithAreas || Features.SlimRouteBoard);
            Assert.IsTrue(Features.AllRoutes && Features.AllClasses && Features.AllTrinkets && Features.AllIngredients
                && Features.ThirdTrinketSlot && Features.Mutators && Features.CampEvents && Features.ClassSelect);
        }

        [Test]
        public void SliceOff_NoFallAndNoWrenGate()
        {
            Slice.On = false;
            var s = new GameSession(_c, null, 1);
            Assert.IsFalse(s.Story.FallDue, "no opening fall");
            Assert.IsNull(s.Story.SetOutBlock, "Wren doesn't hold you back");
            Assert.IsTrue(s.Story.TrayShown);
            Assert.IsNull(s.TrayBlock);
        }

        [Test]
        public void SliceOn_ANewGameOpensWithTheFall()
        {
            var s = new GameSession(_c, null, 1);
            Assert.IsTrue(s.Story.FallDue);
            Assert.IsNotNull(s.Story.SetOutBlock, "Wren wants a word first");
        }
    }
}
