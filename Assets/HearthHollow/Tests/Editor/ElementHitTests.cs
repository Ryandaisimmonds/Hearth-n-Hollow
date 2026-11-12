// Ticket 61: element hits are mostly looks; frost's slow stays slight.
using HearthHollow.Dungeon;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class ElementHitTests
    {
        [Test]
        public void FrostsChill_IsOnlyASlightSlow()
        {
            Assert.That(EnemyController.ChillSlow, Is.InRange(0.8f, 0.95f));
            Assert.That(EnemyController.ChillTime, Is.LessThanOrEqualTo(2f));
            Assert.That(EnemyController.BurnTime, Is.LessThanOrEqualTo(2f));
        }
    }
}
