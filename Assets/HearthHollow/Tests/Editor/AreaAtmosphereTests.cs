// Ticket 55: each area gets its own air.
using HearthHollow.Core;
using HearthHollow.Dungeon;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class AreaAtmosphereTests
    {
        [Test]
        public void EachSliceArea_HasItsOwnAir()
        {
            var c = DefaultContent.Create();
            Assert.AreEqual(AirKind.Meadow, AreaAtmosphere.KindFor(c, "meadow"));
            Assert.AreEqual(AirKind.Chili, AreaAtmosphere.KindFor(c, "chili"));
            Assert.AreEqual(AirKind.Frost, AreaAtmosphere.KindFor(c, "frosthollow"));
            Assert.AreEqual(AirKind.Fall, AreaAtmosphere.KindFor(c, GameSession.FallRoute));
            Assert.AreEqual(AirKind.None, AreaAtmosphere.KindFor(c, null));
        }
    }
}
