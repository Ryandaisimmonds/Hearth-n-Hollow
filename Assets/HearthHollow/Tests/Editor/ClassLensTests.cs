// Hearth & Hollow — edit-mode tests for ticket 09: minimal base stats and per-stat class meal multipliers.
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class ClassLensTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        RecipeData HpMeal()
        {
            var r = _c.Recipe("berry_porridge");   // ticket 52: (no +% HP special) every meal's HP comes from its station base
            Assert.IsNotNull(r, "some meal gives max HP");
            return r;
        }

        float Hp(RecipeData r) { float a, h; Tray.Base(_c, r, out a, out h); return h; }

        float MealHp(ClassId id, RecipeData r)
        {
            foreach (var m in Cooking.MealBuffs(_c, _c.Class(id), new EatenMeal { recipeId = r.id, potency = 1f }))
                if (m.stat == BuffStat.MaxHp) return m.value;
            return 0f;
        }

        [Test]
        public void EveryClass_StartsOnTheSameMinimalStats()
        {
            foreach (var k in _c.classes)
            {
                Assert.AreEqual(DefaultContent.MinimalBaseHp, k.baseMaxHp, k.name);
                Assert.AreEqual(1f, k.damageMult, 0.0001f, k.name);
                Assert.AreEqual(1f, k.gatherZoneMult, 0.0001f, k.name);
            }
        }

        [Test]
        public void MealStats_GoThroughTheClassLens()
        {
            var r = HpMeal();
            float hp = Hp(r);
            Assert.AreEqual(hp * 1.25f, MealHp(ClassId.Warden, r), 0.001f, "Warden: health ×1.25");
            Assert.AreEqual(hp * 0.8f, MealHp(ClassId.Ranger, r), 0.001f, "Ranger: health ×0.8");
            Assert.AreEqual(hp * 1.1f, MealHp(ClassId.Cook, r), 0.001f, "Cook: every meal ×1.1");
            Assert.AreEqual(hp, MealHp(ClassId.Emberfist, r), 0.001f, "Emberfist: health untouched");
        }

        [Test]
        public void Stats_AddTheLensedMealOnTopOfTheBase()
        {
            var r = HpMeal();
            var p = Economy.NewProfile(_c);
            var meals = new List<EatenMeal> { new EatenMeal { recipeId = r.id, potency = 1f } };
            var bare = StatCalc.ComputeWith(_c, p, ClassId.Warden, new List<EatenMeal>());
            var fed = StatCalc.ComputeWith(_c, p, ClassId.Warden, meals);
            Assert.AreEqual(System.Math.Round(Hp(r) * 1.25f), fed.maxHp - bare.maxHp, 1.0, "Warden gets ×1.25 of the meal's health");
        }

        [Test]
        public void Lens_DescribesStrongAndWeakStats()
        {
            var w = _c.Class(ClassId.Warden);
            CollectionAssert.Contains(Meals.StrongStats(w), BuffStat.MaxHp);
            CollectionAssert.Contains(Meals.WeakStats(w), BuffStat.MoveSpeedPct);
            Assert.IsEmpty(Meals.StrongStats(_c.Class(ClassId.Cook)), "the Cook gets every meal ×1.1, no favourites");
        }

        [Test]
        public void OldContent_IsResetToTheCurrentDesign()
        {
            var c = DefaultContent.Create();
            var old = c.Class(ClassId.Warden);
            old.baseMaxHp = 16; old.damageMult = 1.15f; old.mealStatMults.Clear();
            c.contentVersion = 27;   // from before refactor 03's baseline: too old to patch
            Assert.IsTrue(DefaultContent.Upgrade(c));
            Assert.AreEqual(DefaultContent.LatestContentVersion, c.contentVersion);
            var w = c.Class(ClassId.Warden);
            Assert.AreEqual(DefaultContent.MinimalBaseHp, w.baseMaxHp);
            Assert.AreEqual(1.25f, Meals.StatMult(w, BuffStat.Armour), 0.0001f);
        }
    }
}
