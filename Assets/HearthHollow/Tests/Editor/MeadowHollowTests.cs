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

        RunState Run(int floor) { return new RunState { routeId = "meadow", floor = floor, floorsTotal = 10, dungeon = true }; }

        [Test]
        public void Bosses_BrambleSowOn5_HoarfrostAlphaOn10()
        {
            Assert.AreEqual("Meadow Hollow", _c.Route("meadow").name);
            Assert.AreEqual("bramble_sow", DungeonRules.BossMonster(_c, Run(5)));
            Assert.AreEqual("hoarfrost_alpha", DungeonRules.BossMonster(_c, Run(10)));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(10)).Contains("hoarfrost_alpha"), "bosses never turn up as ordinary monsters");
        }

        [Test]
        public void Monsters_NatureUpTop_WolvesFromFloor6()
        {
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(5)).Contains("wolf"));
            Assert.IsTrue(DungeonRules.NormalPool(_c, Run(6)).Contains("wolf"));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(2)).Contains("bat"));
            Assert.IsTrue(DungeonRules.NormalPool(_c, Run(3)).Contains("bat"));
        }

        HashSet<string> Drops(int floor)
        {
            var set = new HashSet<string>();
            for (int i = 0; i < 400; i++) set.Add(DungeonRules.PickIngredient(_c, _c.Route("meadow"), floor, i / 400f));
            return set;
        }

        [Test]
        public void Drops_GingerFrom4_DeepIngredientsFrom6()
        {
            var f1 = Drops(1); var f4 = Drops(4); var f6 = Drops(6);
            Assert.IsFalse(f1.Contains("ginger")); Assert.IsTrue(f4.Contains("ginger"));
            foreach (var deep in new[] { "starmoss", "wild_honeycomb", "golden_wheat" })
            {
                Assert.IsFalse(f4.Contains(deep), deep + " not before floor 6");
                Assert.IsTrue(f6.Contains(deep), deep + " from floor 6");
            }
            Assert.Contains("timber", _c.Route("meadow").materialIds);
        }

        [Test]
        public void EveryTier2Dish_HasItsDeepIngredientInTheMeadow()
        {
            var drops = Drops(10);
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
            var ds = AssetDatabase.LoadAssetAtPath<DungeonSettings>("Assets/HearthHollow/Dungeon/DungeonSettings.asset");
            Assert.IsNotNull(ds);
            var sow = ds.ProfileFor("bramble_sow"); var alpha = ds.ProfileFor("hoarfrost_alpha");
            Assert.AreEqual(Element.Nature, sow.element);
            Assert.AreEqual(Element.Frost, alpha.element);
            Assert.Contains(Element.Fire, alpha.weakTo);
            Assert.Contains(Element.Frost, alpha.resists);
        }
    }
}
