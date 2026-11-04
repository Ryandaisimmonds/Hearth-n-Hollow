// Hearth & Hollow — edit-mode tests for ticket 42: a new game opens with a fall into Meadow Hollow.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class TheFallTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); Slice.On = true; }

        [Test]
        public void NewGame_FallsIn_WakesInTown_ThenMeetsWren()
        {
            var s = new GameSession(_c, null, 91);
            Assert.IsTrue(s.FallDue);
            Assert.IsTrue(s.BeginTheFall());
            Assert.AreEqual(Phase.Dungeon, s.Phase);
            Assert.IsTrue(s.Run.firstDive);
            Assert.AreEqual(WrenStep.FirstDive, s.Wren);
            Assert.IsFalse(s.Profile.metWren, "you haven't met her yet");
            Assert.IsFalse(s.BeginTheFall(), "only once");

            s.DungeonDamagePlayer(99999f);   // the fog wins
            Assert.AreEqual(Phase.RunEnd, s.Phase);
            s.ReturnToHearth();
            Assert.AreEqual(Phase.Hearth, s.Phase);
            Assert.AreEqual(WrenStep.AfterDive, s.Wren);
            Assert.IsTrue(s.WrenTalkDue, "Wren comes over when you wake");
            Assert.IsFalse(s.TrayShown, "no meal tray yet");
            var pages = s.WrenTalk();
            StringAssert.Contains("I'm Wren", pages[0]);
            Assert.IsTrue(s.Profile.metWren);
            Assert.AreEqual(WrenStep.CookMeal, s.Wren);
            Assert.IsTrue(s.AreaOpen("kitchen"));
            Assert.IsFalse(s.FallDue);
        }

        [Test]
        public void TrayShows_OnceWrenExplainsIt()
        {
            var s = new GameSession(_c, null, 92);
            s.Profile.arcStage = 1;
            s.Profile.wrenStep = (int)WrenStep.ShowMeal;
            Assert.IsFalse(s.TrayShown);
            Assert.IsNull(s.TrayBlock, "no tray, nothing to fill");
            s.Profile.wrenStep = (int)WrenStep.FirstBoss;
            Assert.IsTrue(s.TrayShown);
            Assert.IsNotNull(s.TrayBlock);
        }

        [Test]
        public void NoFall_ForAnOngoingGame()
        {
            var s = new GameSession(_c, null, 93);
            s.Profile.runsStarted = 2;
            Assert.IsFalse(s.FallDue);
        }
    }
}
