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

        RunState Run(int floor, string route = "meadow") { return new RunState { routeId = route, floor = floor, floorsTotal = 3, dungeon = true }; }

        [Test]
        public void Bosses_BrambleSowAtTheBottom_AlphaInFrostHollow()   // ticket 47
        {
            Assert.AreEqual("Meadow Hollow", _c.Route("meadow").name);
            Assert.AreEqual("bramble_sow", DungeonRules.BossMonster(_c, Run(3)));
            Assert.AreEqual("hoarfrost_alpha", DungeonRules.BossMonster(_c, Run(3, "frosthollow")));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(3, "frosthollow")).Contains("hoarfrost_alpha"), "bosses never turn up as ordinary monsters");
        }

        [Test]
        public void Monsters_NatureUpTop_BatsFromFloor2()
        {
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(1)).Contains("bat"));
            Assert.IsTrue(DungeonRules.NormalPool(_c, Run(2)).Contains("bat"));
            Assert.IsFalse(DungeonRules.NormalPool(_c, Run(3)).Contains("wolf"), "wolves live in Frost Hollow now");
        }

        HashSet<string> Drops(int floor, string route = "meadow")
        {
            var set = new HashSet<string>();
            for (int i = 0; i < 400; i++) set.Add(DungeonRules.PickIngredient(_c, _c.Route(route), floor, i / 400f));
            return set;
        }

        [Test]
        public void Drops_GingerFrom2_DeepIngredientsInFrostHollow()
        {
            Assert.IsFalse(Drops(1).Contains("ginger")); Assert.IsTrue(Drops(2).Contains("ginger"));
            foreach (var deep in new[] { "starmoss", "wild_honeycomb", "golden_wheat" })
            {
                Assert.IsFalse(Drops(3).Contains(deep), deep + " not in the Meadow any more");
                Assert.IsTrue(Drops(3, "frosthollow").Contains(deep), deep + " in Frost Hollow");
            }
            Assert.Contains("timber", _c.Route("meadow").materialIds);
        }

        [Test]
        public void EveryTier2Dish_HasItsDeepIngredientInFrostHollow()
        {
            var drops = Drops(3, "frosthollow");
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
