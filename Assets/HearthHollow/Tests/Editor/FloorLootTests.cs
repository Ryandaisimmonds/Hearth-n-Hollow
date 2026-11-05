// Hearth & Hollow — edit-mode tests for ticket 48: fewer drops, loot on the floor, the swap box, dropping.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class FloorLootTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession InRun()
        {
            var s = new GameSession(_c, null, 48);
            s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
            Assert.IsTrue(s.StartRun("meadow"));
            return s;
        }

        static void Fill(GameSession s, string id)
        {
            for (int i = 0; i < s.Run.bag.Count; i++) s.Run.bag[i] = new BagSlot { ingredientId = id, count = s.Content.tuning.stackSize };
        }

        [Test]
        public void Drops_AreAboutFortyPercentFewer()
        {
            Assert.AreEqual(0.21f, _c.tuning.monsterDropChance, 0.001f);
            Assert.Less(_c.tuning.materialDropChance, 0.1f);
        }

        [Test]
        public void FullBag_LootLandsOnTheFloor()
        {
            var s = InRun();
            Fill(s, "wheat");
            s.DungeonSecretChest(1);
            Assert.GreaterOrEqual(s.FloorLoot.Count, 1, "it didn't vanish (a chest can give an ingredient and a material)");
            Assert.Greater(s.FloorLoot[0].count, 0);
        }

        [Test]
        public void PickUp_WithRoom_GoesInTheBag()
        {
            var s = InRun();
            s.Run.bag[0] = new BagSlot { ingredientId = "herbs", count = 2 };
            Assert.IsTrue(s.BagDrop(0));
            Assert.AreEqual(1, s.FloorLoot.Count);
            Assert.IsTrue(Bag.IsEmpty(s.Run.bag[0]));
            Assert.IsFalse(s.DungeonPickUp(s.FloorLoot[0]), "it fits: no swap needed");
            Assert.AreEqual(0, s.FloorLoot.Count);
            Assert.AreEqual("herbs", s.Run.bag[0].ingredientId);
        }

        [Test]
        public void PickUp_WithAFullBag_HoldsIt_ThenSwaps()
        {
            var s = InRun();
            s.Run.bag[0] = new BagSlot { ingredientId = "berries", count = 3 };
            s.BagDrop(0);
            var item = s.FloorLoot[0]; item.placed = true; item.x = 4f; item.y = 5f;
            Fill(s, "wheat");
            Assert.IsTrue(s.DungeonPickUp(item), "bag full: hold it");
            Assert.AreSame(item, s.HeldItem);
            Assert.IsTrue(s.SwapHeld(2));
            Assert.AreEqual("berries", s.Run.bag[2].ingredientId);
            Assert.IsNull(s.HeldItem);
            Assert.AreEqual(1, s.FloorLoot.Count);
            Assert.AreEqual("wheat", s.FloorLoot[0].ingredientId, "the swapped-out stack lies where the berries were");
            Assert.AreEqual(4f, s.FloorLoot[0].x);
        }

        [Test]
        public void ClosingWithoutSwapping_LeavesItOnTheFloor()
        {
            var s = InRun();
            s.Run.bag[0] = new BagSlot { ingredientId = "berries", count = 3 };
            s.BagDrop(0);
            Fill(s, "wheat");
            var item = s.FloorLoot[0];
            Assert.IsTrue(s.DungeonPickUp(item));
            s.CancelHeld();
            Assert.IsNull(s.HeldItem);
            Assert.AreEqual(1, s.FloorLoot.Count);
            Assert.AreEqual(3, s.FloorLoot[0].count);
        }

        [Test]
        public void LeavingTheFloor_LeavesItsLoot()
        {
            var s = InRun();
            s.Run.bag[0] = new BagSlot { ingredientId = "herbs", count = 1 };
            s.BagDrop(0);
            Assert.IsTrue(s.DungeonDescend());
            Assert.AreEqual(0, s.FloorLoot.Count);
        }
    }
}
