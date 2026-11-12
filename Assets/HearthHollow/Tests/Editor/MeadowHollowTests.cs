// Hearth & Hollow — edit-mode tests for ticket 13: Meadow Hollow's floors, bosses, monsters and drops.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Dungeon;
using NUnit.Framework;
using UnityEditor;

namespace HearthHollow.Tests
{
    public class MeadowHollowTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        RunState Run(int floor, string route = Ids.Area.Meadow) { return new RunState { routeId = route, floor = floor, floorsTotal = 3, dungeon = true }; }

        [Test]
        public void Bosses_BrambleSowAtTheBottom_AlphaInFrostHollow()   // ticket 47
        {
            Assert.AreEqual("Meadow Hollow", _c.Route(Ids.Area.Meadow).name);
            Assert.AreEqual(Ids.Monster.BrambleSow, DungeonRules.BossMonster(_c, Run(3)));
            Assert.AreEqual(Ids.Monster.HoarfrostAlpha, DungeonRules.BossMonster(_c, Run(3, Ids.Area.FrostHollow)));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(3, Ids.Area.FrostHollow)).Contains(Ids.Monster.HoarfrostAlpha), "bosses never turn up as ordinary monsters");
        }

        [Test]
        public void Monsters_NatureUpTop_BatsFromFloor2()
        {
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(1)).Contains(Ids.Monster.Bat));
            Assert.IsTrue(DungeonRules.NormalPool(_c, Run(2)).Contains(Ids.Monster.Bat));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(3)).Contains(Ids.Monster.Wolf), "wolves live in Frost Hollow now");
        }

        HashSet<string> Drops(int floor, string route = Ids.Area.Meadow)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < 400; i++) set.Add(DungeonRules.PickIngredient(_c, _c.Route(route), floor, i / 400f));
            return set;
        }

        [Test]
        public void Drops_GingerFrom2_DeepIngredientsInFrostHollow()
        {
            Assert.IsFalse(Drops(1).Contains(Ids.Ingredient.Ginger)); Assert.IsTrue(Drops(2).Contains(Ids.Ingredient.Ginger));
            foreach (var deep in new[] { Ids.Ingredient.Starmoss, Ids.Ingredient.WildHoneycomb, Ids.Ingredient.GoldenWheat })
            {
                Assert.IsFalse(Drops(3).Contains(deep), deep + " not in the Meadow any more");
                Assert.IsTrue(Drops(3, Ids.Area.FrostHollow).Contains(deep), deep + " in Frost Hollow");
            }
            Assert.Contains(Ids.Ingredient.Timber, _c.Route(Ids.Area.Meadow).materialIds);
        }

        [Test]
        public void EveryTier2Dish_HasItsDeepIngredientInFrostHollow()
        {
            var drops = Drops(3, Ids.Area.FrostHollow);
            foreach (var r in _c.recipes)
                if (r.tier >= 2)
                    foreach (var ic in r.ingredients)
                    {
                        var ing = _c.Ingredient(ic.ingredientId);
                        if (ing != null && ing.deep) Assert.IsTrue(drops.Contains(ing.id), r.id + " needs " + ing.id);
                    }
        }

        [Test]
        public void BossElements_SowIsNature_AlphaIsFrostWeakToFire()
        {
            var c = DefaultContent.Create();
            var sow = c.Monster(Ids.Monster.BrambleSow); var alpha = c.Monster(Ids.Monster.HoarfrostAlpha);
            Assert.AreEqual(Element.Nature, sow.element);
            Assert.AreEqual(Element.Frost, alpha.element);
            Assert.Contains(Element.Fire, alpha.weakTo);
            Assert.Contains(Element.Frost, alpha.resists);
        }
    }
}
