// Hearth & Hollow — edit-mode tests: the opening arc (stages, gifts, the dash lock, the pit, start speed).
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class ArcTests
    {
        GameContent _c;
        OpeningArc _arc;

        [SetUp]
        public void SetUp() { _c = DefaultContent.Create(); _arc = new OpeningArc(); }

        GameSession NewSession() { var s = new GameSession(_c, null, 1); s.Arc = _arc; return s; }

        [Test]
        public void NewPlayer_StartsAtArrival_SlowAndWithoutDash()
        {
            var s = NewSession();
            Assert.AreEqual(0, s.ArcStage);
            Assert.IsFalse(s.DashUnlocked, "a new player shouldn't have the dash");
            Assert.Less(s.ArcSpeed, 1f);
            Assert.IsTrue(s.AreaOpen("hearth"));
            Assert.IsTrue(s.AreaOpen("board"));
            Assert.IsFalse(s.AreaOpen("kitchen"));
            Assert.IsFalse(s.AreaOpen("smithy"));
            Assert.IsTrue(s.AreaOpen("fishing pond"), "areas no stage mentions are always open");
        }

        [Test]
        public void Stages_OnlyAdvanceInOrder()
        {
            var p = Economy.NewProfile(_c);
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.FirstLadder), "a ladder can't skip the first death");
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.PitFall));
            Assert.AreEqual(0, p.arcStage);
            Assert.IsNotNull(Arc.Advance(_c, _arc, p, ArcTrigger.FirstDeath));
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.FirstDeath), "dying again doesn't advance twice");
            Assert.AreEqual(1, p.arcStage);
        }

        [Test]
        public void FullArc_HandsOutItsGifts()
        {
            var p = Economy.NewProfile(_c);
            string herb = _c.starterPantry[0].ingredientId;
            int herbs = Economy.PantryCount(p, herb), coins = p.coins;

            Arc.Advance(_c, _arc, p, ArcTrigger.FirstDeath);
            Assert.AreEqual(herbs + _c.starterPantry[0].count, Economy.PantryCount(p, herb), "stage 1 should feed you");
            Assert.IsTrue(Arc.IsOpen(_arc, p, "kitchen"));

            Assert.IsFalse(Arc.DashUnlocked(_arc, p, ClassId.Warden));
            Arc.Advance(_c, _arc, p, ArcTrigger.FirstLadder);
            foreach (var cd in _c.classes) Assert.AreEqual(1, Gear.TierIndex(p, GearSlot.Boots, cd.id), cd.id + " didn't get the first boots");
            Assert.IsTrue(Arc.DashUnlocked(_arc, p, ClassId.Warden));
            Assert.IsTrue(Arc.IsOpen(_arc, p, "workshop"));

            Arc.Advance(_c, _arc, p, ArcTrigger.PitFall);
            Arc.Advance(_c, _arc, p, ArcTrigger.HomeAfterPit);
            Assert.AreEqual(4, p.arcStage);
            Assert.IsTrue(Arc.Finished(_arc, p));
            Assert.IsTrue(Arc.IsOpen(_arc, p, "town"));
            Assert.AreEqual(coins + 50, p.coins);
        }

        [Test]
        public void ArcOff_EverythingOpen_DashOn()
        {
            _arc.enabled = false;
            var s = NewSession();
            Assert.IsTrue(s.DashUnlocked);
            Assert.IsTrue(s.AreaOpen("kitchen"));
            Assert.IsTrue(s.ArcFinished);
        }

        [Test]
        public void DyingOnTheFirstRun_ReachesStage1()
        {
            var s = NewSession();
            var route = _c.routes.Find(r => s.IsRouteUnlocked(r.id));
            Assert.IsTrue(s.StartRun(route.id));
            s.DungeonDamagePlayer(100000f);
            Assert.AreEqual(Phase.RunEnd, s.Phase);
            Assert.AreEqual(1, s.ArcStage);
        }

        [Test]
        public void Ladder_GivesBootsMidRun()
        {
            var s = NewSession();
            s.Profile.arcStage = 1;
            var route = _c.routes.Find(r => s.IsRouteUnlocked(r.id));
            s.StartRun(route.id);
            Assert.IsFalse(s.DashUnlocked);
            if (s.Run.floorsTotal < 2) Assert.Ignore(route.id + " has only one floor");
            Assert.IsTrue(s.DungeonDescend());
            Assert.AreEqual(2, s.ArcStage);
            Assert.IsTrue(s.DashUnlocked);
        }

        [Test]
        public void Pit_DropsAFloor_AndTheCacheGivesEquipment()
        {
            var s = NewSession();
            s.Profile.arcStage = 2;
            var route = _c.routes.Find(r => s.IsRouteUnlocked(r.id));
            s.StartRun(route.id);
            if (s.Run.floorsTotal < 2) Assert.Ignore(route.id + " has only one floor");
            Assert.IsTrue(s.DungeonPitWanted);
            int floor = s.Run.floor;
            int lantern = Gear.TierIndex(s.Profile, GearSlot.Lantern, s.Run.classId);
            Assert.IsTrue(s.DungeonFallIntoPit());
            Assert.AreEqual(floor + 1, s.Run.floor);
            Assert.AreEqual(3, s.ArcStage);
            Assert.IsTrue(s.Run.pitRoom);
            Assert.IsFalse(s.DungeonPitWanted, "only one pit per arc");
            s.DungeonPitCache();
            Assert.AreEqual(lantern + 1, Gear.TierIndex(s.Profile, GearSlot.Lantern, s.Run.classId));
        }

        [Test]
        public void Pit_NeverAppearsOutsideItsStage()
        {
            var s = NewSession();
            var route = _c.routes.Find(r => s.IsRouteUnlocked(r.id));
            s.StartRun(route.id);
            Assert.IsFalse(s.DungeonPitWanted);
            Assert.IsFalse(s.DungeonFallIntoPit());
        }

        [Test]
        public void SpeedGear_IsDoubled_ForTheSlowerStart()
        {
            Assert.AreEqual(0.08f, _c.bootsTiers[1].value, 0.0001f);
            Assert.AreEqual(0.28f, _c.bootsTiers[4].value, 0.0001f);
            Assert.AreEqual(0.16f, _c.trinkets.Find(t => t.id == "feather").perkValue, 0.0001f);
            foreach (var n in _c.skillNodes)
                if (n.effect == NodeEffect.MoveSpeed) Assert.AreEqual(0.06f, n.valuePerRank, 0.0001f, n.id);
            // fully kitted ends near today's top speed (today: 1 + 0.14 + 0.08 + 0.10 + 0.15 = 1.47)
            float now = _arc.baseSpeed * (1f + 0.28f + 0.16f + 0.20f + 0.30f);
            Assert.That(now, Is.InRange(1.35f, 1.55f));
        }
    }
}
