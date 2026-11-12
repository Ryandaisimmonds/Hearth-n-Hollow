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
            Assert.IsFalse(s.WrenFireHintDue);
            Assert.IsFalse(s.GiveWrenFireHint());
            Progression.RecordFloor(s.Profile, Ids.Area.Meadow, 3);
            Assert.IsFalse(s.WrenFireHintDue, "reaching floor 3 isn't beating its boss");
        }

        [Test]
        public void Hint_TeachesTheFireWeakness_AndRevealsTheRidge_Once()
        {
            var s = Fresh();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 3);
            Assert.IsTrue(s.WrenFireHintDue);
            Assert.AreEqual(Ids.Monster.HoarfrostAlpha, s.MeadowDeepBoss);
            Assert.IsFalse(s.KnowsElement(Ids.Monster.HoarfrostAlpha, Element.Fire));
            Assert.IsFalse(s.IsRouteUnlocked(Ids.Area.Chili));

            var pages = s.WrenFireHintPages();
            Assert.That(string.Join(" ", pages.ToArray()), Does.Contain("Hoarfrost Alpha").And.Contain("Chili Ridge"));
            Assert.IsTrue(s.GiveWrenFireHint());
            Assert.IsTrue(s.KnowsElement(Ids.Monster.HoarfrostAlpha, Element.Fire));
            Assert.IsTrue(s.IsRouteUnlocked(Ids.Area.Chili));
            Assert.IsFalse(s.WrenFireHintDue, "plays once");
            Assert.IsFalse(s.GiveWrenFireHint());
        }

        [Test]
        public void Hint_IsSaved()
        {
            var s = Fresh();
            Progression.RecordBoss(s.Profile, Ids.Area.Meadow, 3);
            s.GiveWrenFireHint();
            var json = UnityEngine.JsonUtility.ToJson(s.Profile);
            var back = UnityEngine.JsonUtility.FromJson<Profile>(json);
            var s2 = new GameSession(_c, back, 12);
            Assert.IsFalse(s2.WrenFireHintDue);
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
            Assert.IsFalse(s.WrenFireHintDue, "not while still in the dungeon");
            s.DungeonExit();
            s.ReturnToHearth();
            Assert.IsTrue(s.WrenFireHintDue);
        }
    }
}
