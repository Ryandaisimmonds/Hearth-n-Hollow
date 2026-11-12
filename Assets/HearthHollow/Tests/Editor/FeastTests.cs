// Ticket 60: the feast at the gate shows your stats before and after the meals.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.UI;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class FeastTests
    {
        [Test]
        public void TheFeast_ShowsTheMealsLiftingYourStats()
        {
            var c = DefaultContent.Create();
            var p = Economy.NewProfile(c);
            p.selectedClass = ClassId.Cook;
            p.packed = new List<CookedMeal> { new CookedMeal { recipeId = "herb_stew", grade = 1 }, new CookedMeal { recipeId = "mushroom_skewer", grade = 2 } };
            float a0, a1; int h0, h1;
            FeastMoment.Stats(c, p, Cooking.Packed(c, p), out a0, out a1, out h0, out h1);
            Assert.Greater(a1, a0);
            Assert.Greater(h1, h0);
            Assert.Greater(FeastMoment.Length(3), FeastMoment.Length(1));
            Assert.Less(FeastMoment.Length(3), 4f, "short enough not to drag");
        }
    }
}
