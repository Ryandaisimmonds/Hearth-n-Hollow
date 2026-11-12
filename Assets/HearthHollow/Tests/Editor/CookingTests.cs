// Hearth & Hollow — edit-mode tests for cooking v10 (Design v2, ticket 08).
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class CookingTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Fresh() { var s = new GameSession(_c, null, 1); s.Profile.pantry.Clear(); return s; }

        [Test]
        public void QualityOdds_SumToOne_AndStartAtTheTunedChances()
        {
            var odds = Cooking.Odds(_c.tuning, 0f, 0f);
            Assert.AreEqual(1f, odds[0] + odds[1] + odds[2], 0.0001f);
            Assert.AreEqual(_c.tuning.qualityFineChance, odds[1], 0.0001f);
            Assert.AreEqual(_c.tuning.qualityPrimeChance, odds[2], 0.0001f);
            var nudged = Cooking.Odds(_c.tuning, 0.06f, 1f);
            Assert.Greater(nudged[2], odds[2], "station level and the minigame raise the Prime chance");
            Assert.AreEqual(1f, nudged[0] + nudged[1] + nudged[2], 0.0001f);
        }

        [Test]
        public void Quality_IsABigDeal()   // ticket 44: Fine ×1.5, Prime ×2
        {
            Assert.AreEqual(1.5f, Cooking.GradeMult(_c.tuning, 1), 0.0001f);
            Assert.AreEqual(2f, Cooking.GradeMult(_c.tuning, 2), 0.0001f);
        }

        [Test]
        public void OldSaves_MergeIngredientGradesIntoOneStack()
        {
            var p = Economy.NewProfile(_c);
            p.saveVersion = 9;
            p.pantry = new List<ItemStack> { new ItemStack { ingredientId = Ids.Ingredient.Herbs, count = 2, grade = 0 }, new ItemStack { ingredientId = Ids.Ingredient.Herbs, count = 3, grade = 2 } };
            Cooking.MergeIngredientGrades(p);
            Assert.AreEqual(1, p.pantry.Count);
            Assert.AreEqual(5, p.pantry[0].count);
            Assert.AreEqual(0, p.pantry[0].grade);
            Assert.AreEqual(10, p.saveVersion);
        }

        [Test]
        public void Cooking_SpendsTheIngredientsAndTheElementSlot()
        {
            var s = Fresh();
            Economy.AddToPantry(s.Profile, Ids.Ingredient.Herbs, 2); Economy.AddToPantry(s.Profile, Ids.Ingredient.Mushroom, 1); Economy.AddToPantry(s.Profile, Ids.Ingredient.Ginger, 1);
            var meal = s.Cook(Ids.Recipe.HerbStew, new CookChoice { accelId = Ids.Ingredient.Ginger });
            Assert.IsNotNull(meal);
            Assert.AreEqual(Ids.Ingredient.Ginger, meal.accelId);
            Assert.AreEqual(0, Economy.PantryCount(s.Profile, Ids.Ingredient.Herbs));
            Assert.AreEqual(0, Economy.PantryCount(s.Profile, Ids.Ingredient.Ginger));
        }

        [Test]
        public void ElementSlot_InfusesOrGuards()
        {
            var meals = new List<EatenMeal>
            {
                new EatenMeal { recipeId = Ids.Recipe.HerbStew, potency = 1f, accelId = Ids.Ingredient.Chili },
                new EatenMeal { recipeId = Ids.Recipe.HerbButty, potency = 1f, accelId = Ids.Ingredient.Ginger },
            };
            Assert.AreEqual(Element.Fire, Cooking.Infusion(_c, meals));
            CollectionAssert.AreEqual(new[] { Element.Frost }, Cooking.Guards(_c, meals));
            Assert.AreEqual(0, Cooking.MealBuffs(_c, _c.Class(ClassId.Cook), meals[0]).FindAll(b => b.stat == BuffStat.DamagePct).Count, "the slot adds no stats");
        }

        [Test]
        public void SeveralInfusedMeals_PackTogether()   // ticket 46 (was: only one per tray)
        {
            var s = Fresh();
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbStew, accelId = Ids.Ingredient.Chili });
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbButty, accelId = Ids.Ingredient.FrostBerry });
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.MushroomSkewer, accelId = Ids.Ingredient.Ginger });
            Assert.IsTrue(s.PackMeal(0));
            Assert.IsTrue(s.PackMeal(0), "a second infusion is fine now");
            Assert.IsTrue(s.PackMeal(0), "and a guard meal");
            var inf = Cooking.Infusions(_c, Cooking.Packed(_c, s.Profile));
            Assert.AreEqual(2, inf.Count);
            Assert.Contains(Element.Fire, inf);
        }

        [Test]
        public void TierTwoDishes_NeedADeepIngredient_AndHitHarder()
        {
            var t2 = _c.Recipe(Ids.Recipe.HerbStew2);
            Assert.IsNotNull(t2);
            Assert.AreEqual(2, t2.tier);
            Assert.AreEqual(Ids.Recipe.HerbStew, t2.upgradeOf);
            Assert.IsTrue(t2.ingredients.Exists(ic => _c.Ingredient(ic.ingredientId) != null && _c.Ingredient(ic.ingredientId).deep));
            var cls = _c.Class(ClassId.Cook);   // ticket 52: a tier-2 dish gives more of everything
            float hp2 = 0f, hp1 = 0f;
            foreach (var b in Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = Ids.Recipe.HerbStew2, potency = 1f })) if (b.stat == BuffStat.MaxHp) hp2 += b.value;
            foreach (var b in Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = Ids.Recipe.HerbStew, potency = 1f })) if (b.stat == BuffStat.MaxHp) hp1 += b.value;
            Assert.Greater(hp2, hp1);
            Assert.Greater(t2.buffs.Count, _c.Recipe(Ids.Recipe.HerbStew).buffs.Count, "and a second special");
        }

        [Test]
        public void Combos_AreLight_AndNoXpIsLeft()
        {
            foreach (var sy in _c.synergies)
            {
                Assert.AreEqual(1f, sy.allBuffMultiplier, sy.id);
                Assert.AreEqual(0f, sy.sameRecipeBonus, sy.id);
            }
            foreach (var r in _c.recipes) Assert.IsFalse(r.buffs.Exists(b => b.stat == BuffStat.XpPct), r.id + " still gives XP");
        }

        [Test]
        public void StartingARun_TakesTheTraysInfusionAndGuards()
        {
            var s = Fresh();
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbStew, accelId = Ids.Ingredient.Chili });
            s.Profile.mealPantry.Add(new CookedMeal { recipeId = Ids.Recipe.HerbButty, accelId = Ids.Ingredient.Ginger });
            s.PackMeal(0); s.PackMeal(0);
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            Assert.AreEqual(Element.Fire, s.RunElement);
            CollectionAssert.Contains(s.RunGuards, Element.Frost);
        }
    }
}
