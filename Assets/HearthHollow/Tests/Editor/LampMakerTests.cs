// Hearth & Hollow — edit-mode tests for ticket 17: the Lamp-maker, the slice's town building.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class LampMakerTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Fresh() { var s = new GameSession(_c, null, 31); s.Profile.coins = 0; return s; }

        void Give(GameSession s, LampStage st)
        {
            s.Profile.coins += st.price;
            foreach (var m in st.materials) Economy.AddToPantry(s.Profile, m.ingredientId, m.count);
        }

        [Test]
        public void ThreeStages_PaidWithMeadowAndRidgeMaterials()
        {
            Assert.AreEqual(3, _c.lampMaker.Count);
            Assert.IsTrue(_c.lampMaker[0].materials.Exists(m => m.ingredientId == "timber"));
            Assert.IsTrue(_c.lampMaker[1].materials.Exists(m => m.ingredientId == "ember_coal"), "stage 2 needs Chili Ridge's coal");
            Assert.Contains("timber", _c.Route("meadow").materialIds);
            Assert.Contains("ember_coal", _c.Route("chili").materialIds);
        }

        [Test]
        public void Building_NeedsCoinsAndMaterials_AndPaysThem()
        {
            var s = Fresh();
            string why;
            Assert.IsFalse(s.BuildLampMaker(out why));
            Assert.IsNotNull(why);
            Give(s, _c.lampMaker[0]);
            Assert.IsTrue(s.BuildLampMaker(out why), why);
            Assert.AreEqual(1, s.Profile.lampMakerStage);
            Assert.AreEqual(0, s.Profile.coins);
            Assert.AreEqual(0, Economy.PantryCount(s.Profile, "timber"));
        }

        [Test]
        public void EachStage_WidensLightAndFlare()
        {
            var s = Fresh();
            float light0 = Gear.LightReach(_c, s.Profile);
            Assert.AreEqual(0f, LampMaker.FlareRadius(_c, s.Profile));
            string why;
            for (int i = 0; i < _c.lampMaker.Count; i++) { Give(s, _c.lampMaker[i]); Assert.IsTrue(s.BuildLampMaker(out why), why); }
            Assert.AreEqual(3, LampMaker.Stage(s.Profile));
            Assert.Greater(Gear.LightReach(_c, s.Profile), light0);
            Assert.Greater(LampMaker.FlareRadius(_c, s.Profile), 0f);
            Assert.Greater(LampMaker.FlareCooldownCut(_c, s.Profile), 0f);
            Assert.IsNull(LampMaker.Next(_c, s.Profile));
            Assert.IsFalse(s.BuildLampMaker(out why), "nothing left to build");
        }

        [Test]
        public void OnlyInTown()
        {
            var s = Fresh();
            Give(s, _c.lampMaker[0]);
            s.Profile.selectedClass = ClassId.Cook;
            Assert.IsTrue(s.StartRun("meadow"));
            string why;
            Assert.IsFalse(s.BuildLampMaker(out why));
        }
    }
}
