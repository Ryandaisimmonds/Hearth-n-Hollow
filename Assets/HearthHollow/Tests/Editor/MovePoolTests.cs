// Hearth & Hollow — edit-mode tests for ticket 10: the Cook's move pool, arc unlocks, learning and the Ultimate slot.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class MovePoolTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        Profile Fresh() { return Economy.NewProfile(_c); }
        bool Owns(Profile p, string id) { return Progression.Owns(_c, p, ClassId.Cook, _c.Move(id)); }

        [Test]
        public void Pool_IsTheConfirmedKit()
        {
            string[] pool = { "c_ladle", "c_snack", "c_pepper", "c_hotpot", "c_picnic", "c_slide", "c_ult", "c_ult2" };
            foreach (var id in pool) Assert.IsTrue(Progression.InPool(_c, ClassId.Cook, _c.Move(id)), id);
            foreach (var id in new[] { "c_skewer", "c_pan", "c_pin" }) Assert.IsFalse(Progression.InPool(_c, ClassId.Cook, _c.Move(id)), id + " left the pool");
            Assert.AreEqual(MoveSlot.Ultimate, _c.Move("c_ult2").slot);
        }

        [Test]
        public void FreshCook_StartsWithLadleSkewerTossAndRoll_NoUltimate()
        {
            var p = Fresh();
            Assert.IsTrue(Owns(p, "c_ladle")); Assert.IsTrue(Owns(p, "c_snack")); Assert.IsTrue(Owns(p, "roll"));
            Assert.IsFalse(Owns(p, "c_pepper")); Assert.IsFalse(Owns(p, "c_ult"));
            Assert.IsNull(Progression.Ultimate(_c, p, ClassId.Cook));
        }

        [Test]
        public void ArcStep_UnlocksThenLearnInTown()
        {
            var p = Fresh();
            Assert.IsFalse(Progression.ReadyToLearn(_c, p, ClassId.Cook, _c.Move("c_pepper")));
            Assert.IsFalse(Progression.Learn(_c, p, ClassId.Cook, "c_pepper"), "can't learn before the arc step");
            Progression.RecordFloor(p, "meadow", 3);
            Assert.IsTrue(Progression.ReadyToLearn(_c, p, ClassId.Cook, _c.Move("c_pepper")));
            Assert.IsFalse(Owns(p, "c_pepper"), "unlocked isn't owned until learned");
            Assert.IsTrue(Progression.Learn(_c, p, ClassId.Cook, "c_pepper"));
            Assert.IsTrue(Owns(p, "c_pepper"));
            Assert.IsTrue(Progression.Equip(_c, p, ClassId.Cook, LoadSlot.Skill2, "c_pepper"));
        }

        [Test]
        public void BossKill_UnlocksHotPot_AndFloorsOnlyGoUp()
        {
            var p = Fresh();
            Assert.IsTrue(Progression.RecordFloor(p, "meadow", 5));
            Assert.IsFalse(Progression.RecordFloor(p, "meadow", 2));
            Assert.AreEqual(5, Progression.DeepestFloor(p, "meadow"));
            Assert.IsFalse(Progression.ReadyToLearn(_c, p, ClassId.Cook, _c.Move("c_hotpot")), "reaching floor 5 isn't beating its boss");
            Assert.IsTrue(Progression.RecordBoss(p, "meadow", 5));
            Assert.IsFalse(Progression.RecordBoss(p, "meadow", 5));
            Assert.IsTrue(Progression.ReadyToLearn(_c, p, ClassId.Cook, _c.Move("c_hotpot")));
        }

        [Test]
        public void FirstUltimateLearned_GoesInTheSlot_AndTheSecondCanBeChosen()
        {
            var p = Fresh();
            Progression.RecordFloor(p, "meadow", 4);
            Progression.Learn(_c, p, ClassId.Cook, "c_ult");
            Assert.AreEqual("c_ult", Progression.Ultimate(_c, p, ClassId.Cook).id);
            Assert.IsNotNull(Progression.EquipBlock(_c, p, ClassId.Cook, LoadSlot.Ultimate, _c.Move("c_ult2")), "Five-Alarm Chili still locked");
            Progression.RecordBoss(p, "chili", 5);
            Progression.Learn(_c, p, ClassId.Cook, "c_ult2");
            Assert.IsTrue(Progression.Equip(_c, p, ClassId.Cook, LoadSlot.Ultimate, "c_ult2"));
            Assert.AreEqual("c_ult2", Progression.Ultimate(_c, p, ClassId.Cook).id);
        }

        [Test]
        public void WholePool_UnlocksWithinTheSlice()
        {
            foreach (var u in _c.moveUnlocks)
                if (u.kind != UnlockKind.Start) CollectionAssert.Contains(Slice.RouteIds, u.routeId, u.moveId);
        }

        [Test]
        public void SessionDescend_RecordsTheFloorAndAnnouncesUnlocks()
        {
            var s = new GameSession(_c, null, 3);
            s.Profile.arcStage = 1;   // past the first dive (no ladder there)
            s.Profile.selectedClass = ClassId.Cook;
            Assert.IsTrue(s.StartRun("meadow"));
            Assert.AreEqual(1, Progression.DeepestFloor(s.Profile, "meadow"));
            s.DungeonDescend();
            Assert.AreEqual(2, Progression.DeepestFloor(s.Profile, "meadow"));
            Assert.IsTrue(Progression.ReadyToLearn(_c, s.Profile, ClassId.Cook, _c.Move("c_slide")), "Butter Slide unlocks at Meadow floor 2");
        }
    }
}
