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
            Assert.AreEqual(AirKind.Meadow, AreaAtmosphere.KindFor(c, Ids.Area.Meadow));
            Assert.AreEqual(AirKind.Chili, AreaAtmosphere.KindFor(c, Ids.Area.Chili));
            Assert.AreEqual(AirKind.Frost, AreaAtmosphere.KindFor(c, Ids.Area.FrostHollow));
            Assert.AreEqual(AirKind.Fall, AreaAtmosphere.KindFor(c, GameSession.FallRoute));
            Assert.AreEqual(AirKind.None, AreaAtmosphere.KindFor(c, null));
        }
    }
}
