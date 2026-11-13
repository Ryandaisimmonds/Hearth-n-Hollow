// Hearth & Hollow — edit-mode tests for ticket 15: Old Wren's Fire hint after the Meadow floor-5 boss.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class WrenFireHintTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Fresh() { var s = new GameSession(_c, null, 11); s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1; return s; }   // past the first dive

        [Test]
        public void NotDue_UntilTheSowIsBeaten()
        {
            var s = Fresh();
            Assert.IsFalse(s.Story.FireHintDue);
            Assert.IsFalse(s.Story.GiveFireHint());
            Progression.RecordFloor(s.Profile, Ids.Area.Meadow, 3);
            Assert.IsFalse(s.Story.FireHintDue, "reaching floor 3 isn't beating its boss");
        }

        [Test]
        public void Hint_TeachesTheFireWeakness_AndRevealsTheRidge_Once()
        {
            var s = Fresh();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 3);
            Assert.IsTrue(s.Story.FireHintDue);
            Assert.AreEqual(Ids.Monster.HoarfrostAlpha, s.Story.DeepBoss);
            Assert.IsFalse(s.KnowsElement(Ids.Monster.HoarfrostAlpha, Element.Fire));
            Assert.IsFalse(s.IsRouteUnlocked(Ids.Area.Chili));

            var pages = s.Story.FireHintPages();
            Assert.That(string.Join(" ", pages.ToArray()), Does.Contain("Hoarfrost Alpha").And.Contain("Chili Ridge"));
            Assert.IsTrue(s.Story.GiveFireHint());
            Assert.IsTrue(s.KnowsElement(Ids.Monster.HoarfrostAlpha, Element.Fire));
            Assert.IsTrue(s.IsRouteUnlocked(Ids.Area.Chili));
            Assert.IsFalse(s.Story.FireHintDue, "plays once");
            Assert.IsFalse(s.Story.GiveFireHint());
        }

        [Test]
        public void Hint_IsSaved()
        {
            var s = Fresh();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 3);
            s.Story.GiveFireHint();
            var json = UnityEngine.JsonUtility.ToJson(s.Profile);
            var back = UnityEngine.JsonUtility.FromJson<Profile>(json);
            var s2 = new GameSession(_c, back, 12);
            Assert.IsFalse(s2.Story.FireHintDue);
            Assert.IsTrue(s2.IsRouteUnlocked(Ids.Area.Chili));
            Assert.IsTrue(s2.KnowsElement(Ids.Monster.HoarfrostAlpha, Element.Fire));
        }

        [Test]
        public void TheBossKill_InARun_MakesTheHintDueOnceHome()
        {
            var s = Fresh();
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            for (int i = 0; i < 2; i++) s.DungeonDescend();
            Assert.AreEqual(3, s.Run.floor);
            s.DungeonEnemyKilled(s.DungeonEnemy(Ids.Monster.BrambleSow, true, true));
            Assert.IsFalse(s.Story.FireHintDue, "not while still in the dungeon");
            s.DungeonExit();
            s.ReturnToHearth();
            Assert.IsTrue(s.Story.FireHintDue);
        }
    }
}
