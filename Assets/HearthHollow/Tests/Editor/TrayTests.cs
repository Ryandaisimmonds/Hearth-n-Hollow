// Hearth & Hollow — edit-mode tests for ticket 44: flat + % meal stats, big quality, typed tray slots.
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

        [Test]
        public void FlatFirst_ThenPercent()
        {
            var cls = _c.Class(ClassId.Cook);
            var none = StatCalc.ComputeWith(_c, null, ClassId.Cook, null);
            var meals = Cooking.ToEaten(_c, new List<CookedMeal> { Meal("mushroom_skewer"), Meal("herb_stew"), Meal("firecracker_skewer") });
            var st = StatCalc.ComputeWith(_c, null, ClassId.Cook, meals);
            float m = Meals.StatMult(cls, BuffStat.Attack);
            float expect = (none.damage + 2f * m) * (1f + 0.25f * Meals.StatMult(cls, BuffStat.DamagePct));
            Assert.AreEqual(expect, st.damage, 0.01f, "base + flat, then × (1 + %)");
            Assert.AreEqual(none.maxHp + (int)System.Math.Round(8f * Meals.StatMult(cls, BuffStat.MaxHp)), st.maxHp);
            Assert.Greater(st.damage, none.damage * 1.8f, "a Plain tray roughly doubles your attack");
        }

        [Test]
        public void Quality_IsAOneAndAHalf_AndDoubler()
        {
            Assert.AreEqual(1f, Cooking.GradeMult(_c.tuning, 0));
            Assert.AreEqual(1.5f, Cooking.GradeMult(_c.tuning, 1));
            Assert.AreEqual(2f, Cooking.GradeMult(_c.tuning, 2));
            var plain = StatCalc.ComputeWith(_c, null, ClassId.Cook, Cooking.ToEaten(_c, new List<CookedMeal> { Meal("mushroom_skewer", 0) }));
            var prime = StatCalc.ComputeWith(_c, null, ClassId.Cook, Cooking.ToEaten(_c, new List<CookedMeal> { Meal("mushroom_skewer", 2) }));
            var none = StatCalc.ComputeWith(_c, null, ClassId.Cook, null);
            Assert.AreEqual((plain.damage - none.damage) * 2f, prime.damage - none.damage, 0.01f);
        }

        [Test]
        public void TierOneMeals_HaveOneStat_TierTwo_Two()
        {
            foreach (var r in _c.recipes)
            {
                if (r.buffs == null || r.buffs.Count == 0) continue;
                Assert.LessOrEqual(r.buffs.Count, r.tier >= 2 ? 2 : 1, r.id);
            }
        }

        [Test]
        public void TrayNeeds_AnAttackAndAHealthMeal()
        {
            var s = Cook();
            s.Profile.mealPantry.Add(Meal("herb_stew")); s.Profile.mealPantry.Add(Meal("herb_stew")); s.Profile.mealPantry.Add(Meal("herb_stew"));
            Assert.IsTrue(s.PackMeal(0));
            Assert.IsTrue(s.PackMeal(0), "Health + Free");
            Assert.IsFalse(s.PackMeal(0), "a third Health meal would leave no room for an attack meal");
            Assert.IsTrue(s.PackAlwaysMeal("bread"), "Camp Bread fits the Attack slot");
            Assert.IsNull(s.TrayBlock);
            var eaten = Cooking.Packed(_c, s.Profile);
            Assert.AreEqual(TraySlot.Attack, eaten.Find(e => e.recipeId == "bread").slot);
        }

        [Test]
        public void BreadInTheAttackSlot_GivesAttack_ElsewhereHp()
        {
            var cls = _c.Class(ClassId.Cook);
            var atk = new EatenMeal { recipeId = "bread", potency = 1f, slot = TraySlot.Attack };
            var free = new EatenMeal { recipeId = "bread", potency = 1f, slot = TraySlot.Free };
            Assert.IsTrue(Cooking.MealBuffs(_c, cls, atk).Exists(b => b.stat == BuffStat.Attack));
            Assert.IsTrue(Cooking.MealBuffs(_c, cls, free).Exists(b => b.stat == BuffStat.MaxHp));
        }

        [Test]
        public void Assign_PrefersRealMeals_InTypedSlots()
        {
            var slots = Tray.Assign(_c, new List<string> { "bread", "mushroom_skewer", "herb_butty" });
            Assert.AreEqual(TraySlot.Health, slots[0], "bread covers Health");
            Assert.AreEqual(TraySlot.Attack, slots[1]);
            Assert.AreEqual(TraySlot.Free, slots[2]);
            Assert.AreEqual(0, Tray.Missing(_c, new List<string> { "bread", "mushroom_skewer", "herb_butty" }).Count);
            Assert.IsNotNull(Tray.CanAdd(_c, new List<string> { "herb_butty", "bramble_jam" }, "herb_stew"), "two utility meals leave no room");
        }

        [Test]
        public void OldTray_OfThreeHealthMeals_IsBlocked()
        {
            var s = Cook();
            s.Profile.packed.AddRange(new[] { Meal("herb_stew"), Meal("berry_porridge"), Meal("marsh_chowder") });
            s.Profile.tray.AddRange(new[] { "herb_stew", "berry_porridge", "marsh_chowder" });
            StringAssert.Contains("Attack", s.TrayBlock);
        }
    }
}
