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
            Assert.AreEqual(1, _c.Recipe("firecracker_skewer").stationLevel);
        }

        [Test]
        public void FindingAnIngredient_RevealsItsRecipes()
        {
            var p = Economy.NewProfile(_c);
            p.pantry.Clear(); p.seenIngredients.Clear();
            var skewer = _c.Recipe("firecracker_skewer");   // chili + mushroom
            Assert.IsFalse(Routes.RecipeKnown(_c, p, skewer));
            Economy.AddToPantry(p, "chili", 1);
            Assert.IsTrue(Routes.RecipeKnown(_c, p, skewer), "one known ingredient is enough to see it");
            Assert.IsFalse(Routes.IngredientKnown(_c, p, "mushroom"), "the other shows as ?");
            Economy.RemoveFromPantry(p, "chili", 1);
            Assert.IsTrue(Routes.RecipeKnown(_c, p, skewer), "once found, always known");
        }

        [Test]
        public void SliceHidesRecipesItCantFinish()
        {
            if (!Slice.On) Assert.Ignore("slice off");
            var p = Economy.NewProfile(_c);
            Economy.AddToPantry(p, "wheat", 1);
            Assert.IsFalse(Routes.RecipeKnown(_c, p, _c.Recipe("marsh_butty")), "salt isn't in the slice");
            Assert.IsTrue(Routes.RecipeKnown(_c, p, _c.Recipe("herb_butty")));
        }
    }
}
