// Hearth & Hollow — every ingredient and dish has pixel art (drawn in code: UI/FoodArtData.cs, from
// Tools/FoodArt/food_art.py), and ArtLibrary hands it out when its own slot is empty. The Art Library's ingredient
// and recipe slots are empty on purpose; they only win when someone drops a hand-made sprite into one. So a new
// ingredient or meal needs a drawing in food_art.py, not an Art Library slot.
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.UI;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class FoodArtTests
    {
        [Test]
        public void EveryIngredientAndDish_HasArt()
        {
            var c = DefaultContent.Create();
            foreach (var i in c.ingredients) Assert.IsTrue(FoodArt.Has("ing:" + i.id), "no art for ingredient " + i.id);
            foreach (var r in c.recipes) Assert.IsTrue(FoodArt.Has("dish:" + r.id), "no art for dish " + r.id);
        }

        [Test]
        public void ArtLibrary_FallsBackToTheDrawnArt()
        {
            var c = DefaultContent.Create();
            var keep = ArtLibrary.Active; var fi = ArtLibrary.IngredientFallback; var fr = ArtLibrary.RecipeFallback;
            try
            {
                ArtLibrary.Active = null;
                FoodArt.Install(c);
                var chili = ArtLibrary.GetIngredient(Ids.Ingredient.Chili);
                Assert.IsNotNull(chili);
                Assert.AreEqual(16, (int)chili.rect.width);
                Assert.IsNotNull(ArtLibrary.GetRecipe(Ids.Recipe.HerbStew));
                Assert.IsNull(ArtLibrary.GetIngredient("not_a_thing"));
                // a dish with no drawing of its own borrows its station's
                c.recipes.Add(new RecipeData { id = "test_new_soup", station = Station.Pot });
                Assert.AreSame(FoodArt.Get("station:Pot"), ArtLibrary.GetRecipe("test_new_soup"));
            }
            finally { ArtLibrary.Active = keep; ArtLibrary.IngredientFallback = fi; ArtLibrary.RecipeFallback = fr; }
        }
    }
}
