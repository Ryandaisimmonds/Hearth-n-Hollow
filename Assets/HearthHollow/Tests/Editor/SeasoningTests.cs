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
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbStew });
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbButty });
            Economy.AddToPantry(s.Profile, Ids.Ingredient.Chili, 2);
            Economy.AddToPantry(s.Profile, Ids.Ingredient.Ginger, 1);
            return s;
        }

        [Test]
        public void Seasoning_SpendsTheIngredient_AndSetsTheSlot()
        {
            var s = WithMeals();
            var m = s.Profile.mealPantry[0];
            string why;
            Assert.IsTrue(s.SeasonMeal(m, Ids.Ingredient.Chili, out why), why);
            Assert.AreEqual(Ids.Ingredient.Chili, m.accelId);
            Assert.AreEqual(1, Economy.PantryCount(s.Profile, Ids.Ingredient.Chili));
            Assert.AreEqual(Element.Fire, Cooking.InfusionOf(_c, m.accelId));
            Assert.IsFalse(s.SeasonMeal(m, Ids.Ingredient.Ginger, out why), "one element per meal");
            Assert.IsFalse(s.SeasonableMeals().Contains(m));
        }

        [Test]
        public void OnlyElementIngredients()
        {
            var s = WithMeals();
            Economy.AddToPantry(s.Profile, Ids.Ingredient.Mushroom, 1);
            string why;
            Assert.IsFalse(s.SeasonMeal(s.Profile.mealPantry[0], Ids.Ingredient.Mushroom, out why));
            Assert.IsFalse(s.SeasonMeal(s.Profile.mealPantry[0], Ids.Ingredient.FireHoney, out why), "none in the larder");
        }

        [Test]
        public void TrayMeals_CanAllBeInfused()   // ticket 46
        {
            var s = WithMeals();
            string why;
            Assert.IsTrue(s.SeasonMeal(s.Profile.mealPantry[0], Ids.Ingredient.Chili, out why), why);
            Assert.IsTrue(s.PackMeal(0)); Assert.IsTrue(s.PackMeal(0));
            var second = s.Profile.packed[1];
            Assert.IsTrue(s.SeasonMeal(second, Ids.Ingredient.Chili, out why), "a second infused meal is fine");
        }
    }
}
