// Hearth & Hollow — edit-mode tests for ticket 46: several infused elements on one hit.
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class ElementMixTests
    {
        static readonly List<Element> FireFrost = new List<Element> { Element.Fire, Element.Frost };

        [Test]
        public void WeaknessOnly_IsOneAndAHalf()
        {
            bool cancel;
            Assert.AreEqual(1.5f, Elements.Multiplier(FireFrost, new List<Element> { Element.Fire }, new List<Element>(), out cancel));
            Assert.IsFalse(cancel);
        }

        [Test]
        public void ResistanceOnly_IsAHalf()
        {
            bool cancel;
            Assert.AreEqual(0.5f, Elements.Multiplier(FireFrost, new List<Element>(), new List<Element> { Element.Frost }, out cancel));
        }

        [Test]
        public void WeaknessAndResistance_Cancel()
        {
            bool cancel;
            Assert.AreEqual(1f, Elements.Multiplier(FireFrost, new List<Element> { Element.Fire }, new List<Element> { Element.Frost }, out cancel));
            Assert.IsTrue(cancel);
        }

        [Test]
        public void TwoWeaknesses_AreStillOneAndAHalf()
        {
            bool cancel;
            Assert.AreEqual(1.5f, Elements.Multiplier(FireFrost, new List<Element> { Element.Fire, Element.Frost }, new List<Element>(), out cancel));
        }

        [Test]
        public void Names_ReadNicely()
        {
            Assert.AreEqual("Fire and Frost", Elements.Names(FireFrost));
            Assert.AreEqual("Fire, Frost and Water", Elements.Names(new List<Element> { Element.Fire, Element.Frost, Element.Water }));
        }

        [Test]
        public void TheRun_CarriesEveryInfusion()
        {
            var c = DefaultContent.Create();
            var s = new GameSession(c, null, 61);
            s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
            s.Profile.packed.Add(new CookedMeal { recipeId = "mushroom_skewer", accelId = "chili" });
            s.Profile.packed.Add(new CookedMeal { recipeId = "herb_stew", accelId = "frost_berry" });
            Cooking.SyncTray(s.Profile);
            Assert.IsTrue(s.StartRun("meadow"));
            Assert.AreEqual(2, s.RunInfusions.Count);
        }
    }
}
