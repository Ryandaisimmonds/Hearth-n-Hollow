// Hearth & Hollow — edit-mode tests for element discovery (Design v2, ticket 07).
using HearthHollow.Core;
using HearthHollow.Runtime;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class DiscoveryTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void ANewGameKnowsNothing()
        {
            var s = new GameSession(_c, null, 1);
            Assert.IsFalse(s.KnowsElement(Ids.Monster.Wolf, Element.Fire));
            Assert.IsNotNull(s.Profile.knownElements);
            Assert.GreaterOrEqual(s.Profile.saveVersion, 9);
        }

        [Test]
        public void Learning_IsPerMonsterKindAndElement_AndOnlyNewOnce()
        {
            var s = new GameSession(_c, null, 1);
            Assert.IsTrue(s.DiscoverElement(Ids.Monster.Wolf, Element.Fire));
            Assert.IsFalse(s.DiscoverElement(Ids.Monster.Wolf, Element.Fire), "already known");
            Assert.IsTrue(s.KnowsElement(Ids.Monster.Wolf, Element.Fire));
            Assert.IsFalse(s.KnowsElement(Ids.Monster.Wolf, Element.Frost), "other elements stay hidden");
            Assert.IsFalse(s.KnowsElement(Ids.Monster.Slime, Element.Fire), "other kinds stay hidden");
            Assert.IsFalse(s.DiscoverElement(Ids.Monster.Wolf, Element.None), "no element, nothing to learn");
        }

        [Test]
        public void KnownOf_FiltersToWhatYouKnow()
        {
            var p = Economy.NewProfile(_c);
            Discovery.Learn(p, Ids.Monster.Crab, Element.Nature);
            CollectionAssert.AreEqual(new[] { Element.Nature }, Discovery.KnownOf(p, Ids.Monster.Crab, new[] { Element.Fire, Element.Nature }));
        }

        [Test]
        public void Discoveries_SurviveASaveAndLoad()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hh_discovery_" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            SaveService.DirectoryOverride = dir;
            try
            {
                var s = new GameSession(_c, null, 1);
                s.DiscoverElement(Ids.Monster.Shaman, Element.Nature);
                Assert.IsTrue(SaveService.Save(s.Profile), "save failed");
                var back = new GameSession(_c, SaveService.Load(), 2);
                Assert.IsTrue(back.KnowsElement(Ids.Monster.Shaman, Element.Nature));
            }
            finally { SaveService.DirectoryOverride = null; if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true); }
        }
    }
}
