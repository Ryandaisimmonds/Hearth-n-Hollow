// Hearth & Hollow — a quick check on the balance simulator (the full report: Hearth & Hollow > Write Balance Report).
using HearthHollow.Core;
using HearthHollow.EditorTools;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class BalanceSimTests
    {
        [Test]
        public void FoodMatters_PrimeNeverDiesMoreThanHungry()
        {
            var c = DefaultContent.Create();
            var hungry = BalanceSim.Run(c, Ids.Area.Meadow, BalanceSim.Trays()[0], 7);
            var prime = BalanceSim.Run(c, Ids.Area.Meadow, BalanceSim.Trays()[3], 7);
            Assert.GreaterOrEqual(hungry.deathRate, prime.deathRate, "a Prime tray never does worse than going hungry");
        }
    }
}
