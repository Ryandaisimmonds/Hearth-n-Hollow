// Hearth & Hollow — edit-mode tests for the vertical slice gate (Design v2, ticket 05).
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
            Assert.IsFalse(s.StartRun("cavern"));
            Assert.IsTrue(Slice.ShowsRoute("meadow"));
            Assert.IsFalse(Slice.ShowsRoute("cavern"));
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
            Assert.IsTrue(Slice.ShowsRoute("cavern"));
        }
    }
}
