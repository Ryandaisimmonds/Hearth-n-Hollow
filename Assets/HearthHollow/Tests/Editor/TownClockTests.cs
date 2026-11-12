// Ticket 58: the town's time of day turns with the days, and the Hearth grows with the slice's areas.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class TownClockTests
    {
        [Test]
        public void TheDaysTurn_MorningAfternoonEvening()
        {
            Assert.AreEqual(DayTime.Morning, TownClock.For(1));
            Assert.AreEqual(DayTime.Afternoon, TownClock.For(2));
            Assert.AreEqual(DayTime.Evening, TownClock.For(3));
            Assert.AreEqual(DayTime.Morning, TownClock.For(4));
            Assert.AreEqual(DayTime.Morning, TownClock.For(0));
            Assert.AreEqual(0f, TownClock.LanternGlow(DayTime.Morning));
            Assert.Greater(TownClock.LanternGlow(DayTime.Evening), TownClock.LanternGlow(DayTime.Afternoon));
        }

        [Test]
        public void TheHearth_GrowsWithTheSlicesAreas()
        {
            bool was = Slice.On;
            try
            {
                Slice.On = true;
                var c = DefaultContent.Create();
                var p = Economy.NewProfile(c);
                p.clearedRouteIds.Clear();
                Assert.AreEqual(0f, TownClock.Progress(c, p));
                p.clearedRouteIds.Add("meadow");
                Assert.AreEqual(1f / 3f, TownClock.Progress(c, p), 1e-4);
                p.clearedRouteIds.Add("chili"); p.clearedRouteIds.Add("frosthollow");
                Assert.AreEqual(1f, TownClock.Progress(c, p), 1e-4);
            }
            finally { Slice.On = was; }
        }
    }
}
