// Hearth & Hollow — edit-mode tests for tickets 23–24: BBQ earlier, and ingredients revealing their recipes.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class DiscoveryRecipeTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void FirecrackerSkewer_NeedsOnlyBbqLevel1()
        {
            Assert.AreEqual(1, _c.Recipe(Ids.Recipe.FirecrackerSkewer).stationLevel);
        }

        [Test]
        public void FindingAnIngredient_RevealsItsRecipes()
        {
            var p = Economy.NewProfile(_c);
            p.pantry.Clear(); p.seenIngredients.Clear();
            var skewer = _c.Recipe(Ids.Recipe.FirecrackerSkewer);   // chili + mushroom
            Assert.IsFalse(Routes.RecipeKnown(_c, p, skewer));
            Economy.AddToPantry(p, Ids.Ingredient.Chili, 1);
            Assert.IsTrue(Routes.RecipeKnown(_c, p, skewer), "one known ingredient is enough to see it");
            Assert.IsFalse(Routes.IngredientKnown(_c, p, Ids.Ingredient.Mushroom), "the other shows as ?");
            Economy.RemoveFromPantry(p, Ids.Ingredient.Chili, 1);
            Assert.IsTrue(Routes.RecipeKnown(_c, p, skewer), "once found, always known");
        }

        [Test]
        public void SliceHidesRecipesItCantFinish()
        {
            if (Features.AllIngredients) Assert.Ignore("slice off");
            var p = Economy.NewProfile(_c);
            Economy.AddToPantry(p, Ids.Ingredient.Wheat, 1);
            Assert.IsFalse(Routes.RecipeKnown(_c, p, _c.Recipe(Ids.Recipe.MarshButty)), "salt isn't in the slice");
            Assert.IsTrue(Routes.RecipeKnown(_c, p, _c.Recipe(Ids.Recipe.HerbButty)));
        }
    }
}
