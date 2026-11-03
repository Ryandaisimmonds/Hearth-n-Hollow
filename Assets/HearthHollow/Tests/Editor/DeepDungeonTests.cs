// Hearth & Hollow — edit-mode tests for ticket 12: deep dungeons (floors, a boss every 5, the home choice, shortcuts).
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class DeepDungeonTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Cook() { var s = new GameSession(_c, null, 7); s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1; return s; }   // past the first dive

        [Test]
        public void Meadow_Is10Floors_BossesOn5And10_Chili5()
        {
            Assert.AreEqual(10, DungeonRules.Floors(_c, "meadow"));
            Assert.AreEqual(5, DungeonRules.Floors(_c, "chili"));
            for (int f = 1; f <= 10; f++)
                Assert.AreEqual(f == 5 || f == 10, DungeonRules.IsBossFloor(_c, "meadow", f), "meadow floor " + f);
            Assert.IsTrue(DungeonRules.IsBossFloor(_c, "chili", 5));
            Assert.IsFalse(DungeonRules.IsBossFloor(_c, "chili", 3));
        }

        [Test]
        public void WayHome_OnlyAfterBosses_AndOnRarePortals()
        {
            int portals = 0, floors = 0;
            for (int seed = 1; seed <= 400; seed++)
            {
                Assert.IsFalse(DungeonRules.HomeChoiceOn(_c, "meadow", 1, seed), "never on floor 1");
                Assert.IsTrue(DungeonRules.HomeChoiceOn(_c, "meadow", 5, seed), "the floor-5 boss opens a way home");
                Assert.IsTrue(DungeonRules.HomeChoiceOn(_c, "meadow", 10, seed));
                foreach (int f in new[] { 2, 3, 4, 6, 7, 8, 9 }) { floors++; if (DungeonRules.HomeChoiceOn(_c, "meadow", f, seed)) portals++; }
            }
            float share = portals / (float)floors;
            Assert.That(share, Is.InRange(0.14f, 0.26f), "about 1 floor in 5 has a portal");
            Assert.AreEqual(DungeonRules.PortalFloor(_c, 4, 77), DungeonRules.PortalFloor(_c, 4, 77), "the same run and floor always agree");
        }

        [Test]
        public void Run_GoesAllTenFloorsDeep()
        {
            var s = Cook();
            Assert.IsTrue(s.StartRun("meadow"));
            Assert.AreEqual(10, s.Run.floorsTotal);
            for (int i = 0; i < 9; i++) Assert.IsTrue(s.DungeonDescend());
            Assert.AreEqual(10, s.Run.floor);
            Assert.IsTrue(s.DungeonBossFloor && s.DungeonOnLastFloor);
            Assert.IsFalse(s.DungeonDescend(), "nothing below the bottom");
        }

        [Test]
        public void BeatingABoss_UnlocksAShortcut_ThatRunsCanStartFrom()
        {
            var s = Cook();
            CollectionAssert.AreEqual(new[] { 1 }, s.StartFloors("meadow"));
            Assert.IsTrue(s.StartRun("meadow", 6));
            Assert.AreEqual(1, s.Run.floor, "no shortcut yet: starts on floor 1");
            s.DungeonDamagePlayer(100000f);   // fall
            s = new GameSession(_c, s.Profile, 8);
            Progression.RecordBoss(s.Profile, "meadow", 5);
            CollectionAssert.AreEqual(new[] { 1, 6 }, s.StartFloors("meadow"));
            Progression.RecordBoss(s.Profile, "meadow", 10);
            CollectionAssert.AreEqual(new[] { 1, 6 }, s.StartFloors("meadow"), "no shortcut below the bottom");
            Assert.IsTrue(s.StartRun("meadow", 6));
            Assert.AreEqual(6, s.Run.floor);
            Assert.AreEqual(6, Progression.DeepestFloor(s.Profile, "meadow"));
        }

        [Test]
        public void Growth_IsCapped_SoDeepFloorsStayFair()
        {
            Assert.AreEqual(0, DungeonRules.ExtraEnemies(1));
            Assert.AreEqual(4, DungeonRules.ExtraEnemies(10));
            Assert.AreEqual(3, DungeonRules.ExtraRooms(10));
        }

        [Test]
        public void FallingDeep_StillKeepsOnlyTheDeathShare()
        {
            Assert.That(_c.tuning.deathKeepFraction, Is.EqualTo(0.25f).Within(0.001f));
        }
    }
}
