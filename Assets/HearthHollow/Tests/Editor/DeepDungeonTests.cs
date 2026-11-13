// Hearth & Hollow — edit-mode tests for tickets 12 / 47: area lengths, bosses, the way home and shortcuts.
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
        public void Areas_Meadow3_Chili2NoBoss_Frost3()   // ticket 47
        {
            Assert.AreEqual(3, DungeonRules.Floors(_c, Ids.Area.Meadow));
            Assert.AreEqual(2, DungeonRules.Floors(_c, Ids.Area.Chili));
            Assert.AreEqual(3, DungeonRules.Floors(_c, Ids.Area.FrostHollow));
            for (int f = 1; f <= 3; f++) Assert.AreEqual(f == 3, DungeonRules.IsBossFloor(_c, Ids.Area.Meadow, f), "meadow floor " + f);
            Assert.IsFalse(DungeonRules.IsBossFloor(_c, Ids.Area.Chili, 2), "Chili Ridge has no boss");
            Assert.IsTrue(DungeonRules.EndsInExit(_c, Ids.Area.Chili, 2));
            Assert.IsFalse(DungeonRules.EndsInExit(_c, Ids.Area.Chili, 1));
            Assert.IsTrue(DungeonRules.IsBossFloor(_c, Ids.Area.FrostHollow, 3));
        }

        [Test]
        public void WayHome_InShortAreas_IsTheBottom()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                Assert.IsFalse(DungeonRules.HomeChoiceOn(_c, Ids.Area.Meadow, 1, seed));
                Assert.IsFalse(DungeonRules.HomeChoiceOn(_c, Ids.Area.Meadow, 2, seed), "no portals in a short area");
                Assert.IsTrue(DungeonRules.HomeChoiceOn(_c, Ids.Area.Meadow, 3, seed), "the boss at the bottom");
                Assert.IsFalse(DungeonRules.HomeChoiceOn(_c, Ids.Area.Chili, 1, seed));
                Assert.IsTrue(DungeonRules.HomeChoiceOn(_c, Ids.Area.Chili, 2, seed), "the exit at the top of the Ridge");
            }
            Assert.AreEqual(DungeonRules.PortalFloor(_c, 4, 77), DungeonRules.PortalFloor(_c, 4, 77), "the same run and floor always agree");
        }

        [Test]
        public void LongAreas_StillHavePortalsAndShortcuts()
        {
            var r = _c.Route(Ids.Area.Meadow); r.floors = 10; r.bossEvery = 5;   // a long area, made up for the test
            int portals = 0, floors = 0;
            for (int seed = 1; seed <= 400; seed++)
                foreach (int f in new[] { 2, 3, 4, 6, 7, 8, 9 }) { floors++; if (DungeonRules.HomeChoiceOn(_c, Ids.Area.Meadow, f, seed)) portals++; }
            Assert.That(portals / (float)floors, Is.InRange(0.14f, 0.26f), "about 1 floor in 5 has a portal");
            var s = Cook();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 5);
            CollectionAssert.AreEqual(new[] { 1, 6 }, s.StartFloors(Ids.Area.Meadow));
        }

        [Test]
        public void Run_GoesThreeFloorsDeep()
        {
            var s = Cook();
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            Assert.AreEqual(3, s.Run.floorsTotal);
            for (int i = 0; i < 2; i++) Assert.IsTrue(s.DungeonDescend());
            Assert.IsTrue(s.DungeonBossFloor && s.DungeonOnLastFloor);
            Assert.IsFalse(s.DungeonDescend(), "nothing below the bottom");
        }

        [Test]
        public void ShortAreas_HaveNoShortcuts()
        {
            var s = Cook();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 3);
            CollectionAssert.AreEqual(new[] { 1 }, s.StartFloors(Ids.Area.Meadow));
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow, 2));
            Assert.AreEqual(1, s.Run.floor);
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

        [Test]
        public void OldSaves_MapOntoTheShorterAreas()
        {
            var p = Economy.NewProfile(_c);
            p.saveVersion = 14; p.wrenStep = (int)WrenStep.Alpha;
            p.bossesBeaten = new System.Collections.Generic.List<string> { "meadow:5", "meadow:10" };
            var s = new GameSession(_c, p, 9);
            Assert.IsTrue(Progression.BeatBoss(s.Profile, Ids.Area.Meadow, 3));
            Assert.IsTrue(Progression.BeatBoss(s.Profile, Ids.Area.FrostHollow, 3));
            Assert.IsTrue(Routes.HasFlag(s.Profile, Story.WrenFrostFlag), "Frost Hollow is open for a save that was hunting the Alpha");
        }
    }
}
