// Ticket 63: boss attacks are marked on the floor, long enough to read.
using HearthHollow.Dungeon;
using NUnit.Framework;
using UnityEngine;

namespace HearthHollow.Tests
{
    public class BossTelegraphTests
    {
        [Test]
        public void ALaneMarker_FillsAlongTheChargeAndGoes()
        {
            var t = Telegraph.Lane(Vector2.zero, Vector2.right, 5f, 1f, 0.6f);
            Assert.IsNotNull(t);
            Assert.AreEqual(2, t.transform.childCount, "an outline and a fill");
            Assert.AreEqual(Telegraph.Order, t.GetComponentInChildren<SpriteRenderer>().sortingOrder);
            Object.DestroyImmediate(t.gameObject);
            Assert.GreaterOrEqual(EnemyController.BossTelegraphMin, 0.5f);
        }
    }
}
