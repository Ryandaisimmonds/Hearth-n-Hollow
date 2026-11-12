// Hearth & Hollow — edit-mode tests for ticket 50: buried chests.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class BuriedChestTests
    {
        [Test]
        public void ABuriedChest_GivesTheAreasIngredients_AndSometimesMore()
        {
            var c = DefaultContent.Create();
            Assert.AreEqual(0.25f, c.tuning.buriedBonusChance, 0.001f);
            int bonus = 0, digs = 200;
            for (int seed = 1; seed <= digs; seed++)
            {
                var s = new GameSession(c, null, seed);
                s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
                Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
                int coins = s.Run.coinsEarned;
                s.DungeonHiddenForage();
                int items = 0, kinds = 0;
                foreach (var b in s.Run.bag) if (!Bag.IsEmpty(b)) { items += b.count; kinds++; }
                Assert.GreaterOrEqual(items, 2, "two of the area's ingredients");
                if (s.Run.coinsEarned > coins || kinds > 1) bonus++;
            }
            Assert.That(bonus / (float)digs, Is.InRange(0.12f, 0.4f), "about one dig in four has coins or a material too");
        }
    }
}
