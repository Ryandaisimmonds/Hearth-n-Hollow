// Hearth & Hollow — edit-mode tests: the opening arc, trimmed for Design v2 (ticket 18).
// Beats: dead fire + Wren → first dive → home, Wren feeds you, kitchen opens → second dive → first boss →
// home, the camp grows into a town → boots (the dash) from the smithy. No pit, no slow start.
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

        GameSession NewSession() { var s = new GameSession(_c, null, 1); s.Arc = _arc; s.Profile.selectedClass = ClassId.Cook; return s; }

        [Test]
        public void NewPlayer_StartsAtArrival_FullSpeed_NoDash()
        {
            var s = NewSession();
            Assert.AreEqual(0, s.ArcStage);
            Assert.IsFalse(s.DashUnlocked, "no boots, no dash");
            Assert.AreEqual(1f, s.ArcSpeed, "the 75% start was cut");
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
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.HomeAfterBoss), "the first boss can't skip the first dive");
            Assert.IsNotNull(Arc.Advance(_c, _arc, p, ArcTrigger.FirstRunEnd));
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.FirstRunEnd), "a second run doesn't advance twice");
            Assert.AreEqual(1, p.arcStage);
            Assert.IsNull(Arc.Advance(_c, _arc, p, ArcTrigger.PitFall), "the pit is gone");
        }

        [Test]
        public void FirstDive_EscapedOrFallen_WrenFeedsYou_KitchenOpens()
        {
            var s = NewSession();
            string herb = _c.starterPantry[0].ingredientId;
            int had = Economy.PantryCount(s.Profile, herb);
            Assert.IsTrue(s.StartRun("meadow"));
            s.DungeonExit();   // escape (falling works the same)
            Assert.AreEqual(1, s.ArcStage);
            Assert.AreEqual(had + _c.starterPantry[0].count, Economy.PantryCount(s.Profile, herb), "Wren feeds you");
            Assert.IsTrue(s.AreaOpen("kitchen"));
            Assert.IsTrue(s.AreaOpen("larder"));
            Assert.IsFalse(s.AreaOpen("smithy"));
        }

        [Test]
        public void FallingOnTheFirstDive_AlsoReachesStage1()
        {
            var s = NewSession();
            Assert.IsTrue(s.StartRun("meadow"));
            s.DungeonDamagePlayer(100000f);
            Assert.AreEqual(Phase.RunEnd, s.Phase);
            Assert.AreEqual(1, s.ArcStage);
        }

        [Test]
        public void HomeAfterTheFirstBoss_TheCampGrows_AndBootsGiveTheDash()
        {
            var s = NewSession();
            s.StartRun("meadow"); s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(1, s.ArcStage);
            int coins = s.Profile.coins;
            s.StartRun("meadow");
            for (int i = 0; i < 4; i++) s.DungeonDescend();
            s.DungeonEnemyKilled(s.DungeonEnemy("bramble_sow", true, true));
            Assert.AreEqual(1, s.ArcStage, "the town grows when you're home, not mid-dive");
            s.DungeonExit(); s.ReturnToHearth();
            Assert.AreEqual(2, s.ArcStage);
            Assert.IsTrue(s.ArcFinished);
            Assert.IsTrue(s.AreaOpen("smithy") && s.AreaOpen("workshop") && s.AreaOpen("lampmaker"));
            Assert.Greater(s.Profile.coins, coins, "a little coin to get started");
            Assert.IsFalse(s.DashUnlocked, "the dash comes from the smithy's first boots");
            s.Profile.coins += 1000;
            string why; Assert.IsTrue(s.BuyGear(GearSlot.Boots, out why), why);
            Assert.IsTrue(s.DashUnlocked);
        }

        [Test]
        public void Ladders_NoLongerGiveBoots()
        {
            var s = NewSession();
            s.StartRun("meadow");
            Assert.IsTrue(s.DungeonDescend());
            Assert.IsFalse(s.DashUnlocked);
            Assert.AreEqual(0, s.Profile.bootsTier);
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
        public void BootSpeeds_AreBackToNormal_WithoutTheSlowStart()
        {
            Assert.AreEqual(0.04f, _c.bootsTiers[1].value, 0.0001f);
            Assert.AreEqual(0.14f, _c.bootsTiers[4].value, 0.0001f);
        }
    }
}
