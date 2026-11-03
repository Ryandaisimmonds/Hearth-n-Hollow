// Hearth & Hollow — edit-mode tests for ticket 27: element ingredients go in at the spice rack, after cooking.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class SeasoningTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession WithMeals()
        {
            var s = new GameSession(_c, null, 51);
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = "herb_stew" });
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = "herb_butty" });
            Economy.AddToPantry(s.Profile, "chili", 2);
            Economy.AddToPantry(s.Profile, "ginger", 1);
            return s;
        }

        [Test]
        public void Seasoning_SpendsTheIngredient_AndSetsTheSlot()
        {
            var s = WithMeals();
            var m = s.Profile.mealPantry[0];
            string why;
            Assert.IsTrue(s.SeasonMeal(m, "chili", out why), why);
            Assert.AreEqual("chili", m.accelId);
            Assert.AreEqual(1, Economy.PantryCount(s.Profile, "chili"));
            Assert.AreEqual(Element.Fire, Cooking.InfusionOf(_c, m.accelId));
            Assert.IsFalse(s.SeasonMeal(m, "ginger", out why), "one element per meal");
            Assert.IsFalse(s.SeasonableMeals().Contains(m));
        }

        [Test]
        public void OnlyElementIngredients()
        {
            var s = WithMeals();
            Economy.AddToPantry(s.Profile, "mushroom", 1);
            string why;
            Assert.IsFalse(s.SeasonMeal(s.Profile.mealPantry[0], "mushroom", out why));
            Assert.IsFalse(s.SeasonMeal(s.Profile.mealPantry[0], "fire_honey", out why), "none in the larder");
        }

        [Test]
        public void TrayMeals_KeepTheOneInfusionRule()
        {
            var s = WithMeals();
            string why;
            Assert.IsTrue(s.SeasonMeal(s.Profile.mealPantry[0], "chili", out why), why);
            Assert.IsTrue(s.PackMeal(0)); Assert.IsTrue(s.PackMeal(0));
            var second = s.Profile.packed[1];
            Assert.IsFalse(s.SeasonMeal(second, "chili", out why), "only one infused meal per tray");
            Assert.IsTrue(s.SeasonMeal(second, "ginger", out why), "a guard is fine");
        }
    }
}
