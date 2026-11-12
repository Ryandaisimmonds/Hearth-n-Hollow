// Hearth & Hollow — edit-mode tests for ticket 14: Chili Ridge.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using NUnit.Framework;
using UnityEditor;

namespace HearthHollow.Tests
{
    public class ChiliRidgeTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void Ridge_Is2QuietFloors_RichInChilies_NoBoss()   // ticket 47
        {
            var r = _c.Route("chili");
            Assert.AreEqual("Chili Ridge", r.name);
            Assert.AreEqual(2, DungeonRules.Floors(_c, "chili"));
            Assert.IsTrue(r.noBoss);
            Assert.Less(r.enemyMult, 1f, "light on monsters");
            Assert.Greater(r.gatherChance, _c.Route("meadow").gatherChance, "rich in resources");
        }

        [Test]
        public void Ridge_IsHidden_UntilWrensDirections()
        {
            var p = Economy.NewProfile(_c);
            Assert.IsFalse(Routes.Discovered(_c, p, _c.Route("chili")));
            Assert.IsFalse(Routes.IsUnlocked(_c, p, "chili"));
            Assert.IsNotNull(Routes.LockReason(_c, p, _c.Route("chili")));
            Assert.IsTrue(Routes.SetFlag(p, DefaultContent.WrenFireHint));
            Assert.IsFalse(Routes.SetFlag(p, DefaultContent.WrenFireHint), "a flag is set once");
            Assert.IsTrue(Routes.IsUnlocked(_c, p, "chili"));
            Assert.IsNull(Routes.EnterBlock(_c, p, "chili", ClassId.Cook));
        }

        [Test]
        public void Chilies_DropOnTheRidge_AndNowhereElseInTheSlice()
        {
            foreach (var id in Slice.RouteIds)
            {
                var set = new HashSet<string>();
                for (int f = 1; f <= DungeonRules.Floors(_c, id); f++)
                    for (int i = 0; i < 200; i++) set.Add(DungeonRules.PickIngredient(_c, _c.Route(id), f, i / 200f));
                Assert.AreEqual(id == "chili", set.Contains("chili"), id);
            }
            CollectionAssert.DoesNotContain(_c.starterPantry.ConvertAll(x => x.ingredientId), "chili");
            Assert.AreEqual(Element.Fire, _c.Ingredient("chili").infuse);
            CollectionAssert.AreEqual(new[] { "ember_coal" }, _c.Route("chili").materialIds);
        }

        [Test]
        public void RidgeDrake_IsAFireBoss()
        {
            Assert.AreEqual(Element.Fire, DefaultContent.Create().Monster("ridge_drake").element);
        }
    }
}
