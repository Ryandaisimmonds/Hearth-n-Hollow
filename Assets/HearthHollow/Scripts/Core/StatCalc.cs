// Hearth & Hollow — derived player stats. Pure C#, no UnityEngine.
// Stats = class base + level + gear (weapon, body, head) + worn trinkets, then raised by the meals eaten before the run.
// At the hearth (run == null) the packed tray is previewed as if eaten, so the player sees
// exactly what they'll set out with.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class StatCalc
    {
        /// Fallback base damage if content has no weapon tiers.
        public const float DefaultBaseDamage = 3f;

        /// Stats for the run in progress, or — at camp — with the packed tray previewed.
        public static PlayerStats Compute(GameContent c, Profile p, RunState run)
        {
            ClassId classId = run != null ? run.classId : (p != null ? p.selectedClass : ClassId.Warden);
            List<EatenMeal> meals = run != null ? run.eaten : Meals.TrayToMeals(c, p != null ? p.tray : null);
            return ComputeWith(c, p, classId, meals);
        }

        /// Stats with no meals at all (class + level + gear) — the "before" in the hearth preview.
        public static PlayerStats ComputeBase(GameContent c, Profile p)
        {
            ClassId classId = p != null ? p.selectedClass : ClassId.Warden;
            return ComputeWith(c, p, classId, null);
        }

        /// Max HP from levels: hpPerLevelEarly for levels 2..earlyLevels, then hpPerLevel.
        /// hpPerLevel may be a fraction (0.2 = +1 HP every 5 levels).
        public static int LevelHp(TuningData t, int level)
        {
            int early = Math.Max(0, Math.Min(level, t.earlyLevels) - 1);
            int late = Math.Max(0, level - Math.Max(1, t.earlyLevels));
            return early * t.hpPerLevelEarly + (int)Math.Floor(late * t.hpPerLevel + 0.0001f);
        }

        public static PlayerStats ComputeWith(GameContent c, Profile p, ClassId classId, List<EatenMeal> meals)
        {
            var t = c.tuning;
            ClassData cls = c.Class(classId) ?? new ClassData { id = classId };
            ClassProgress prog = p != null ? p.Progress(classId) : new ClassProgress { classId = classId };

            float[] sum = Meals.TotalBonus(c, cls, meals);   // meals + combos (empty when no meals)
            float[] tree = Progression.StatBonus(c, p, classId);   // permanent skill-tree bonuses
            for (int i = 0; i < sum.Length && i < tree.Length; i++) sum[i] += tree[i];
            float[] trink = Gear.StatBonus(c, p);              // worn trinkets
            for (int i = 0; i < sum.Length && i < trink.Length; i++) sum[i] += trink[i];

            var st = new PlayerStats();
            int level = Math.Max(1, prog.level);
            st.maxHp = Math.Max(1, (int)Math.Round(cls.baseMaxHp + LevelHp(t, level) + Gear.HeadHp(c, p, classId) + sum[(int)BuffStat.MaxHp]));

            GearTier w = Economy.CurrentWeapon(c, p, classId);
            float baseDmg = w != null ? w.value : DefaultBaseDamage;
            float levelDmg = 1f + Math.Max(0, level - 1) * t.damagePerLevel;   // small bonus every level
            st.damage = baseDmg * cls.damageMult * levelDmg * (1f + sum[(int)BuffStat.DamagePct]);

            GearTier a = Economy.CurrentArmour(c, p, classId);
            float points = (a != null ? a.value : 0f) + sum[(int)BuffStat.Armour];
            st.armourPct = Math.Max(0f, Math.Min(t.armourCap, points * t.armourPerPoint));

            st.critChance = Math.Max(0f, Math.Min(1f, t.baseCritChance + sum[(int)BuffStat.CritPct]));
            st.gatherZoneMult = 1f + sum[(int)BuffStat.GatherZonePct];
            st.coinMult = 1f + sum[(int)BuffStat.CoinPct];
            st.xpMult = 1f + sum[(int)BuffStat.XpPct];
            return st;
        }
    }
}
