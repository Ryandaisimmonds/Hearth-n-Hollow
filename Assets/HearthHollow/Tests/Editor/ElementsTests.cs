// Hearth & Hollow — edit-mode tests for elements (Design v2, ticket 06).
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class ElementsTests
    {
        static readonly Element[] Loop = { Element.Fire, Element.Frost, Element.Nature, Element.Water };

        [Test]
        public void TheLoop_FireFrostNatureWaterFire()
        {
            Assert.AreEqual(Element.Frost, Elements.StrongAgainst(Element.Fire));
            Assert.AreEqual(Element.Nature, Elements.StrongAgainst(Element.Frost));
            Assert.AreEqual(Element.Water, Elements.StrongAgainst(Element.Nature));
            Assert.AreEqual(Element.Fire, Elements.StrongAgainst(Element.Water));
            foreach (var e in Loop) Assert.AreEqual(e, Elements.WeakTo(Elements.StrongAgainst(e)), e + " should beat what it's strong against");
            Assert.AreEqual(Element.None, Elements.StrongAgainst(Element.Light));
            Assert.AreEqual(Element.None, Elements.WeakTo(Element.Light));
        }

        [Test]
        public void Multiplier_WeakOneAndAHalf_ResistHalf_ElseOne()
        {
            var weak = new List<Element> { Element.Fire };
            var resist = new List<Element> { Element.Frost };
            Assert.AreEqual(1.5f, Elements.Multiplier(Element.Fire, weak, resist));
            Assert.AreEqual(0.5f, Elements.Multiplier(Element.Frost, weak, resist));
            Assert.AreEqual(1f, Elements.Multiplier(Element.Water, weak, resist));
            Assert.AreEqual(1f, Elements.Multiplier(Element.None, weak, resist), "a hit with no element is never weak or resisted");
        }

        [Test]
        public void Defaults_WeakToWhatBeatsIt_ResistsItself()
        {
            var weak = new List<Element>(); var resist = new List<Element>();
            Elements.Defaults(Element.Frost, weak, resist);
            CollectionAssert.AreEqual(new[] { Element.Fire }, weak);
            CollectionAssert.AreEqual(new[] { Element.Frost }, resist);
            weak.Clear(); resist.Clear();
            Elements.Defaults(Element.None, weak, resist);
            Assert.IsEmpty(weak); Assert.IsEmpty(resist);
        }

        [Test]
        public void Content_MovesKeepTheirFireAndLight_PhysicalHitsHaveNone()
        {
            var c = DefaultContent.Create();
            Assert.AreEqual(Element.Fire, c.Move("c_pepper").element, "Flambé");
            Assert.AreEqual(Element.Light, c.Move("l_wisp").element, "Wisp Bolts");
            Assert.AreEqual(Element.None, c.Move("r_shot").element, "Triple Shot is physical");
            Assert.AreEqual(Element.Fire, c.Move("c_ladle").element2, "Pan & Ladle's flaming pan finisher");
        }

        [Test]
        public void RunElement_IsNoneOutsideARun()
        {
            var s = new GameSession(DefaultContent.Create(), null, 1);
            Assert.AreEqual(Element.None, s.RunElement);
        }
    }
}
