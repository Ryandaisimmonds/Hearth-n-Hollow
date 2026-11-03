// Hearth & Hollow — edit-mode tests for ticket 16: shared utility gear and the slice trinkets.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class UtilityGearTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void Smithy_SellsBootsLanternAndBag_Only()
        {
            CollectionAssert.AreEqual(new[] { GearSlot.Boots, GearSlot.Lantern, GearSlot.Bag }, Gear.Shop);
            var p = Economy.NewProfile(_c); p.coins = 10000;
            string why;
            Assert.IsFalse(Gear.Buy(_c, p, null, GearSlot.Weapon, out why));
            Assert.IsFalse(Economy.BuyWeapon(_c, p, out why));
            Assert.IsTrue(Gear.Buy(_c, p, null, GearSlot.Lantern, out why), why);
            Assert.AreEqual(1, p.lanternTier);
            Assert.Greater(Gear.LanternReach(_c, p), 0f);
        }

        [Test]
        public void Gear_IsSharedByEveryClass()
        {
            var p = Economy.NewProfile(_c); p.coins = 10000;
            string why;
            p.selectedClass = ClassId.Cook;
            Gear.Buy(_c, p, null, GearSlot.Boots, out why);
            Assert.AreEqual(1, Gear.TierIndex(p, GearSlot.Boots, ClassId.Warden));
            Assert.AreEqual(Gear.BootsSpeed(_c, p, ClassId.Cook), Gear.BootsSpeed(_c, p, ClassId.Ranger));
        }

        [Test]
        public void FirstBoots_UnlockTheDash()
        {
            var arc = new OpeningArc();
            var p = Economy.NewProfile(_c);
            p.arcStage = 0;
            Assert.IsFalse(OpeningArcRules(arc, p), "a new player has no dash");
            p.bootsTier = 1;
            Assert.IsTrue(OpeningArcRules(arc, p));
        }
        static bool OpeningArcRules(OpeningArc a, Profile p) { return HearthHollow.Core.Arc.DashUnlocked(a, p, ClassId.Cook); }

        [Test]
        public void OldSaves_GetOneSharedSet_AndARefund()
        {
            var p = Economy.NewProfile(_c);
            p.saveVersion = 11; p.coins = 0;
            var cook = p.Progress(ClassId.Cook); var warden = p.Progress(ClassId.Warden);
            cook.weaponTier = 2; cook.armourTier = 1; cook.bootsTier = 2; warden.bootsTier = 1; warden.headTier = 1;
            int expected = _c.weaponTiers[1].price + _c.weaponTiers[2].price + _c.armourTiers[1].price + _c.headTiers[1].price + _c.bootsTiers[1].price;
            int refund = Gear.ShareGear(_c, p);
            Assert.AreEqual(expected, refund);
            Assert.AreEqual(expected, p.coins);
            Assert.AreEqual(2, p.bootsTier, "keeps the best boots any class had");
            Assert.AreEqual(0, cook.weaponTier);
            Assert.AreEqual(0, Gear.ShareGear(_c, p), "once only");
        }

        [Test]
        public void SliceTrinkets_AreTheOnlyOnesSold()
        {
            var p = Economy.NewProfile(_c);
            var stock = Gear.WorkshopStock(_c, p);
            var ids = stock.ConvertAll(t => t.id);
            if (Slice.On)
            {
                CollectionAssert.AreEquivalent(new[] { "ember_locket", "magpie_spoon", "pocket_hearth", "honeylocket" }, ids);
            }
        }

        [Test]
        public void PocketHearth_SavesYouOnceARun()
        {
            var s = new GameSession(_c, null, 21);
            s.Profile.arcStage = 1;   // past the first dive
            s.Profile.selectedClass = ClassId.Cook;
            Gear.Give(_c, s.Profile, "pocket_hearth");
            string why; Assert.IsTrue(Gear.Equip(_c, s.Profile, "pocket_hearth", out why), why);
            Assert.IsTrue(s.StartRun("meadow"));
            s.DungeonDamagePlayer(100000f);
            Assert.AreEqual(Phase.Dungeon, s.Phase, "saved by the Pocket Hearth");
            Assert.AreEqual(1, s.Run.hp);
            s.DungeonDamagePlayer(100000f);
            Assert.AreEqual(Phase.RunEnd, s.Phase, "only once");
        }
    }
}
