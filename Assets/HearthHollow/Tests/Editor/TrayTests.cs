// Hearth & Hollow — edit-mode tests for tickets 44 / 52: meal stats (base 🗡️ + ❤️ by station, a special on top,
// flat then %), big quality, and a tray of any three meals.
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class TrayTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        GameSession Cook()
        {
            var s = new GameSession(_c, null, 101);
            s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
            s.Profile.wrenStep = (int)WrenStep.FirstBoss;
            s.Profile.tray.Clear(); s.Profile.packed.Clear(); s.Profile.mealPantry.Clear();
            return s;
        }

        static CookedMeal Meal(string id, int grade = 0) { return new CookedMeal { recipeId = id, grade = grade }; }

        static float Sum(List<StatMod> l, BuffStat st) { float v = 0f; foreach (var m in l) if (m.stat == st) v += m.value; return v; }

        [Test]
        public void EveryMeal_GivesAttackAndHp_TiltedByStation()
        {
            var cls = _c.Class(ClassId.Cook);
            var bbq = Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = "mushroom_skewer", potency = 1f });
            var pot = Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = "herb_stew", potency = 1f });
            var sand = Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = "herb_butty", potency = 1f });
            foreach (var l in new[] { bbq, pot, sand }) { Assert.Greater(Sum(l, BuffStat.Attack), 0f); Assert.Greater(Sum(l, BuffStat.MaxHp), 0f); }
            Assert.Greater(Sum(bbq, BuffStat.Attack), Sum(pot, BuffStat.Attack), "BBQ: a bit more attack");
            Assert.Greater(Sum(pot, BuffStat.MaxHp), Sum(bbq, BuffStat.MaxHp), "Pot: a bit more HP");
            Assert.Greater(Sum(bbq, BuffStat.DamagePct), 0f, "and the special on top");
        }

        [Test]
        public void FlatFirst_ThenPercent()
        {
            var cls = _c.Class(ClassId.Cook);
            var none = StatCalc.ComputeWith(_c, null, ClassId.Cook, null);
            var meals = Cooking.ToEaten(_c, new List<CookedMeal> { Meal("mushroom_skewer"), Meal("herb_stew"), Meal("firecracker_skewer") });
            var st = StatCalc.ComputeWith(_c, null, ClassId.Cook, meals);
            float flat = 0f, pct = 0f, hp = 0f, hpPct = 0f;
            foreach (var m in meals) { var b = Cooking.MealBuffs(_c, cls, m); flat += Sum(b, BuffStat.Attack); pct += Sum(b, BuffStat.DamagePct); hp += Sum(b, BuffStat.MaxHp); hpPct += Sum(b, BuffStat.MaxHpPct); }
            Assert.AreEqual((none.damage + flat) * (1f + pct), st.damage, 0.01f, "base + flat, then × (1 + %)");
            Assert.AreEqual((int)System.Math.Round((none.maxHp + hp) * (1f + hpPct)), st.maxHp);
            Assert.Greater(st.damage, none.damage * 1.8f, "a Plain tray roughly doubles your attack");
        }

        [Test]
        public void Quality_IsAOneAndAHalf_AndDoubler()
        {
            Assert.AreEqual(1.5f, Cooking.GradeMult(_c.tuning, 1));
            Assert.AreEqual(2f, Cooking.GradeMult(_c.tuning, 2));
            var plain = StatCalc.ComputeWith(_c, null, ClassId.Cook, Cooking.ToEaten(_c, new List<CookedMeal> { Meal("herb_butty", 0) }));
            var prime = StatCalc.ComputeWith(_c, null, ClassId.Cook, Cooking.ToEaten(_c, new List<CookedMeal> { Meal("herb_butty", 2) }));
            var none = StatCalc.ComputeWith(_c, null, ClassId.Cook, null);
            Assert.AreEqual((plain.damage - none.damage) * 2f, prime.damage - none.damage, 0.01f, "quality scales the base too");
        }

        [Test]
        public void TierOneMeals_HaveOneSpecial_TierTwo_Two()
        {
            foreach (var r in _c.recipes)
            {
                if (r.buffs == null) continue;
                Assert.LessOrEqual(r.buffs.Count, r.tier >= 2 ? 2 : 1, r.id);
                foreach (var b in r.buffs) Assert.IsFalse(Tray.IsBaseStat(b.stat), r.id + ": the base is added by station, not stored");
            }
        }

        [Test]
        public void TheTray_TakesAnyThreeMeals()   // ticket 52: no typed slots
        {
            var s = Cook();
            for (int i = 0; i < 3; i++) s.Profile.mealPantry.Add(Meal("herb_stew"));
            Assert.IsTrue(s.PackMeal(0)); Assert.IsTrue(s.PackMeal(0)); Assert.IsTrue(s.PackMeal(0), "three stews are fine");
            Assert.IsNull(s.TrayBlock);
            Assert.IsFalse(s.PackAlwaysMeal("bread"), "but the tray holds three");
        }

        [Test]
        public void CampBread_IsHalfASandwich_NoSpecial()
        {
            var cls = _c.Class(ClassId.Cook);
            var bread = Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = "bread", potency = 1f });
            var sand = Cooking.MealBuffs(_c, cls, new EatenMeal { recipeId = "herb_butty", potency = 1f });
            Assert.AreEqual(Sum(sand, BuffStat.MaxHp) * 0.5f, Sum(bread, BuffStat.MaxHp), 0.01f);
            Assert.AreEqual(2, bread.Count, "just the base");
        }
    }
}
