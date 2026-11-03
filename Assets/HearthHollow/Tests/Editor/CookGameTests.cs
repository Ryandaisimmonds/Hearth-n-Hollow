// Hearth & Hollow — edit-mode tests for ticket 11: the stove's optional timing tap.
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class CookGameTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void Marker_SweepsOnceInAboutOneToTwoSeconds()
        {
            var t = _c.tuning;
            Assert.That(t.cookGameSeconds, Is.InRange(1f, 2f));
            Assert.AreEqual(0f, Cooking.CookMarkerAt(t, 0f), 0.0001f);
            Assert.AreEqual(0.5f, Cooking.CookMarkerAt(t, t.cookGameSeconds / 2f), 0.0001f);
            Assert.AreEqual(1f, Cooking.CookMarkerAt(t, t.cookGameSeconds * 3f), 0.0001f, "stops at the end, no ping-pong");
        }

        [Test]
        public void Timing_GoldIsFullNudge_WarmIsPart_MissIsNothing()
        {
            var t = _c.tuning;
            Assert.AreEqual(1f, Cooking.CookNudge(t, t.cookGameTarget), 0.0001f);
            Assert.AreEqual(t.cookGameGoodNudge, Cooking.CookNudge(t, t.cookGameTarget + t.cookGameGood * 0.45f), 0.0001f);
            Assert.AreEqual(0f, Cooking.CookNudge(t, 0f), 0.0001f);
            Assert.AreEqual(0f, Cooking.CookNudge(t, 1f), 0.0001f);
        }

        [Test]
        public void SkippingOrMissing_IsNeverAPenalty_AndPerfectHelps()
        {
            var t = _c.tuning;
            var quick = Cooking.Odds(t, 0f, 0f);
            var miss = Cooking.Odds(t, 0f, Cooking.CookNudge(t, 0f));
            var perfect = Cooking.Odds(t, 0f, 1f);
            for (int g = 0; g < 3; g++) Assert.AreEqual(quick[g], miss[g], 0.0001f);
            Assert.Greater(perfect[2], quick[2]);
            Assert.Greater(perfect[1], quick[1]);
            Assert.Less(perfect[1] + perfect[2], 0.6f, "a small bonus, not a guarantee");
        }

        [Test]
        public void Nudge_NeverChangesTheDish()
        {
            var s = new GameSession(_c, null, 5);
            var r = _c.recipes.Find(x => !x.always && x.tier < 2 && x.ingredients.Count > 0 && Kitchen.Unlocked(_c, s.Profile, x));
            Assert.IsNotNull(r);
            foreach (var ic in r.ingredients) Economy.AddToPantry(s.Profile, ic.ingredientId, ic.count * 2);
            var meal = s.Cook(r.id, new CookChoice { qualityNudge = 1f });
            Assert.IsNotNull(meal);
            Assert.AreEqual(r.id, meal.recipeId, "same dish, same tier");
        }
    }
}
