// Hearth & Hollow — edit-mode tests for ticket 38: ways to heal slowly.
using System.Collections.Generic;
using HearthHollow.Core;
using NUnit.Framework;

namespace HearthHollow.Tests
{
    public class HealingTests
    {
        GameContent _c;
        [SetUp] public void SetUp() { _c = DefaultContent.Create(); }

        [Test]
        public void PotMeals_AndCampBread_HealOverTime()
        {
            foreach (var id in new[] { Ids.Recipe.BerryPorridge, Ids.Recipe.HerbStew2 })   // ticket 44: one healing meal at tier 1, and the tier-2 stew
                Assert.IsTrue(_c.Recipe(id).buffs.Exists(b => b.stat == BuffStat.Regen && b.value > 0f), id);
            var m = new CombatMods();
            Cooking.AddMealMods(_c, _c.Class(ClassId.Cook), new List<EatenMeal> { new EatenMeal { recipeId = Ids.Recipe.BerryPorridge, potency = 1f } }, m);
            Assert.AreEqual(2f * 1.1f, m.regen, 0.01f, "the Cook gets every meal ×1.1");
        }

        [Test]
        public void Foraging_IsASnack()
        {
            var s = new GameSession(_c, null, 81);
            s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            s.Run.hp = 3;
            Assert.IsTrue(s.DungeonStartGather());
            Assert.AreEqual(3 + _c.tuning.forageHeal, s.Run.hp);
        }

        [Test]
        public void ClearingARoom_HealsAShare()
        {
            var s = new GameSession(_c, null, 82);
            s.Profile.selectedClass = ClassId.Cook; s.Profile.arcStage = 1;
            Assert.IsTrue(s.StartRun(Ids.Area.Meadow));
            int max = s.Stats.maxHp;
            s.Run.hp = 1;
            s.DungeonRoomCleared();
            Assert.AreEqual(1 + System.Math.Max(1, (int)System.Math.Round(max * _c.tuning.roomClearHealPct)), s.Run.hp);
            s.Run.hp = max;
            s.DungeonRoomCleared();
            Assert.AreEqual(max, s.Run.hp, "never above max");
        }

        [Test]
        public void SettingOut_NeedsAFullTray_ExceptTheFirstDive()
        {
            var s = new GameSession(_c, null, 83);
            s.Profile.selectedClass = ClassId.Cook;
            s.Profile.tray.Clear();
            Assert.IsNull(s.TrayBlock, "first encounter: no meals needed");
            s.Profile.arcStage = 1;
            s.Profile.wrenStep = (int)WrenStep.FirstBoss;   // ticket 42: the tray shows once Wren has explained it
            Assert.IsNotNull(s.TrayBlock);
            StringAssert.Contains("0/3", s.TrayBlock);
            for (int i = 0; i < _c.tuning.traySize; i++) Assert.IsTrue(s.PackAlwaysMeal(Ids.Recipe.Bread), "Camp Bread always fills the tray");
            Assert.IsNull(s.TrayBlock);
        }
    }
}
