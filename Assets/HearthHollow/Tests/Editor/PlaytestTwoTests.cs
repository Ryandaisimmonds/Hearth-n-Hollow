// Hearth & Hollow — edit-mode tests for the second playtest batch (tickets 28–34).
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class PlaytestTwoTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Cook() { var s = new GameSession(_c, null, 61); s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1; return s; }

        [Test]
        public void FirstBossEver_SendsYouHome()
        {
            var s = Cook();
            Assert.IsTrue(s.StartRun("meadow"));
            for (int i = 0; i < 4; i++) s.DungeonDescend();
            Assert.IsTrue(s.DungeonFirstBossFloor, "the boss room has only the way home");
            s.DungeonEnemyKilled(s.DungeonEnemy("bramble_sow", true, true));
            Assert.IsTrue(s.Run.mustGoHome);
            Assert.IsFalse(s.DungeonDescend(), "no going deeper after your first boss");
            s.DungeonExit(); s.ReturnToHearth();
            Assert.IsTrue(s.StartRun("meadow", 6), "the shortcut is open next time");
            for (int i = 0; i < 4; i++) s.DungeonDescend();
            Assert.AreEqual(10, s.Run.floor);
            Assert.IsFalse(s.DungeonFirstBossFloor, "later bosses don't send you home");
        }

        [Test]
        public void LockedSlots_HaveAHint()
        {
            var p = Economy.NewProfile(_c);
            string ult = Progression.NextUnlockHint(_c, p, ClassId.Cook, MoveSlot.Ultimate);
            Assert.That(ult, Does.Contain("Grand Feast").And.Contain("floor 4"));
            Progression.RecordFloor(p, "meadow", 4);
            Assert.That(Progression.NextUnlockHint(_c, p, ClassId.Cook, MoveSlot.Ultimate), Does.StartWith("Learn Grand Feast"));
            Assert.That(Progression.NextUnlockHint(_c, p, ClassId.Cook, MoveSlot.Skill), Does.Contain("Flambé"));
        }

        [Test]
        public void TheFirstWords_AreWrens()
        {
            var s = new GameSession(_c, null, 62);
            Assert.AreEqual(0, s.ArcStage);
            Assert.IsFalse(s.Profile.metWren, "a new game starts with Wren to meet");
        }
    }
}
