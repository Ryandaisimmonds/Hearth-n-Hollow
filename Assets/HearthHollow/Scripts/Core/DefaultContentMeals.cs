// Hearth & Hollow — content upgrade for "meals are the power" (Oct 2026, contentVersion 3). Pure C#.
// Player base stats, levels, gear and the skill tree's stat nodes were cut hard, meals were made much
// stronger, and each class got a favourite station whose meals count extra:
//   Warden → BBQ, Ranger → Sandwich, Cook → Pot.
// Upgrade3 swaps in the new numbers ONLY for entries still exactly at the previous defaults (matched by a
// signature of their numbers + text), so anything Ryan tuned in the content asset is left alone.
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HearthHollow.Core
{
    public static partial class DefaultContent
    {
        public const int LatestContentVersion = 36;

        static string F(float v) { return v.ToString("R", CultureInfo.InvariantCulture); }

        static string Mods(List<StatMod> mods)
        {
            var sb = new StringBuilder();
            if (mods != null) foreach (var m in mods) sb.Append(m.stat).Append('=').Append(F(m.value)).Append(';');
            return sb.ToString();
        }

        public static string Sig(RecipeData r) { return "recipe|" + r.id + "|" + Mods(r.buffs); }
        public static string Sig(ClassData c) { return "class|" + c.id + "|" + c.baseMaxHp + "|" + c.description; }
        public static string Sig(SynergyData s) { return "syn|" + s.id + "|" + Mods(s.bonus) + "|" + s.description; }
        public static string Sig(SkillNodeData n) { return "node|" + n.id + "|" + F(n.valuePerRank) + "|" + n.description; }
        public static string Sig(string kind, List<GearTier> tiers)
        {
            var sb = new StringBuilder("gear|" + kind + "|");
            if (tiers != null) foreach (var g in tiers) sb.Append(F(g.value)).Append(';');
            return sb.ToString();
        }

        /// Every entry's signature (for the upgrade table).
        public static List<string> AllSigs(GameContent c)
        {
            var list = new List<string>();
            foreach (var x in c.recipes) list.Add(Sig(x));
            foreach (var x in c.classes) list.Add(Sig(x));
            foreach (var x in c.synergies) list.Add(Sig(x));
            foreach (var x in c.skillNodes) list.Add(Sig(x));
            list.Add(Sig("weapon", c.weaponTiers));
            list.Add(Sig("armour", c.armourTiers));
            return list;
        }

        // Signatures of the version-2 defaults (generated from the old DefaultContent).
        static readonly string[] V2Sigs = {
            "recipe|herb_stew|MaxHp=10;",
            "recipe|berry_porridge|XpPct=0.2;MaxHp=4;",
            "recipe|marsh_chowder|MaxHp=10;Armour=2;",
            "recipe|truffle_risotto|Armour=3;MaxHp=6;",
            "recipe|ember_chili|DamagePct=0.2;MaxHp=4;",
            "recipe|lantern_soup|MaxHp=16;",
            "recipe|mushroom_skewer|DamagePct=0.1;CritPct=0.05;",
            "recipe|grilled_fish|CritPct=0.1;CoinPct=0.15;",
            "recipe|firecracker_skewer|DamagePct=0.25;CritPct=0.05;",
            "recipe|ember_steak|DamagePct=0.3;MaxHp=4;",
            "recipe|honey_ribs|CritPct=0.15;DamagePct=0.1;",
            "recipe|bread|MaxHp=4;",
            "recipe|herb_butty|GatherZonePct=0.3;MaxHp=4;",
            "recipe|bramble_jam|CoinPct=0.2;MaxHp=4;",
            "recipe|marsh_butty|Armour=2;CoinPct=0.15;",
            "recipe|glow_egg_sandwich|MaxHp=6;XpPct=0.25;GatherZonePct=0.2;",
            "recipe|ember_toastie|DamagePct=0.2;CritPct=0.1;",
            "class|Warden|30|A steady guardian of the campfire. Hits 15% harder.",
            "class|Ranger|30|Knows the wilds by heart. Much wider golden gather zone.",
            "class|Cook|30|Every meal tastes like home. Meal buffs are 25% stronger.",
            "syn|balanced_feast||One meal from each station: all meal bonuses ×1.5.",
            "syn|twin_flame|CritPct=0.1;DamagePct=0.1;|Eat 2 fiery meals: +10% crit chance, +10% damage.",
            "syn|stone_stomach|Armour=3;MaxHp=6;|Eat 2 hearty meals: +3 armour, +6 max HP.",
            "syn|comfort_food||3 of the same meal: that meal's bonus counts an extra half again.",
            "syn|slow_simmer|MaxHp=8;Armour=2;|All Pot meals: +8 max HP, +2 armour.",
            "syn|grill_master|DamagePct=0.15;CritPct=0.05;|All BBQ meals: +15% damage, +5% crit.",
            "syn|packed_lunch|GatherZonePct=0.25;CoinPct=0.15;|All Sandwich meals: +25% gather zone, +15% coins.",
            "node|w_whetstone|0.03|+3% damage per rank.",
            "node|w_n_lunge|0|New attack: Step forward with a long thrust. Great reach.",
            "node|w_keen|0.02|+2% crit chance per rank.",
            "node|w_honed|0.1|Attacks hit 10% harder per rank.",
            "node|w_n_maul|0|New attack: Slow, crushing blow. Stuns what it hits.",
            "node|w_brutal|0.04|+4% damage per rank.",
            "node|w_warlord|0.25|Attacks hit 25% harder.",
            "node|w_hide|3|+3 max HP per rank.",
            "node|w_iron|1|+1 armour per rank (5% less damage each).",
            "node|w_n_charge|0|New dash: Barge forward, hurting and shoving anything you run through.",
            "node|w_n_shieldwall|0|New skill: Brace: take 70% less damage for 3 seconds.",
            "node|w_secondwind|1|Heal 1 HP per rank whenever you defeat an enemy.",
            "node|w_bastion|6|+6 max HP per rank.",
            "node|w_unbreakable|2|+2 armour.",
            "node|w_rhythm|0.04|Skills recharge 4% faster per rank.",
            "node|w_n_slam|0|New skill: A shockwave rolls forward, damaging and stunning everything in its path.",
            "node|w_might|0.08|Skills hit 8% harder per rank.",
            "node|w_n_warcry|0|New skill: Roar: +40% damage for 6 seconds.",
            "node|w_n_whirl|0|New skill: Spin for 1.5 seconds, hitting everything nearby again and again. You can still move.",
            "node|w_relentless|0.05|Skills recharge 5% faster per rank.",
            "node|w_berserker|0.25|Skills hit 25% harder.",
            "node|r_steady|0.02|+2% crit chance per rank.",
            "node|r_n_spread|0|New attack: Three arrows in a fan. Weaker each, hard to dodge.",
            "node|r_fletching|0.03|+3% damage per rank.",
            "node|r_quickdraw|0.1|Attacks hit 10% harder per rank.",
            "node|r_n_longbow|0|New attack: A slow, heavy arrow that flies through every enemy in a line.",
            "node|r_deadeye|0.04|+4% damage per rank.",
            "node|r_hawkeye|0.25|Attacks hit 25% harder.",
            "node|r_lightpack|0.03|Move 3% faster per rank.",
            "node|r_eye|0.08|Gather zones 8% wider per rank.",
            "node|r_n_vault|0|New dash: Flip backwards and loose an arrow at whatever you were facing.",
            "node|r_n_mark|0|New skill: Mark the nearest enemy: it takes +50% damage for 6 seconds.",
            "node|r_scavenger|0.05|+5% coins per rank.",
            "node|r_hardened|4|+4 max HP per rank.",
            "node|r_fleet|0.25|Dash recharges 25% faster.",
            "node|r_nimble|0.04|Skills recharge 4% faster per rank.",
            "node|r_n_trap|0|New skill: Set a snare at your feet. The first enemy to step in is hurt and pinned for 2.5 seconds.",
            "node|r_barbed|0.08|Skills hit 8% harder per rank.",
            "node|r_n_rain|0|New skill: Arrows fall on a spot ahead of you for 1.5 seconds.",
            "node|r_n_smoke|0|New skill: Enemies nearby are slowed by half for 3 seconds and you speed up.",
            "node|r_pockets|0.05|Skills recharge 5% faster per rank.",
            "node|r_mastertrap|0.25|Skills hit 25% harder.",
            "node|c_grease|0.03|+3% damage per rank.",
            "node|c_n_skewer|0|New attack: Jab forward with a kebab skewer.",
            "node|c_tenderiser|0.02|+2% crit chance per rank.",
            "node|c_heavyhand|0.1|Attacks hit 10% harder per rank.",
            "node|c_n_pan|0|New attack: A ringing pan to the head. Stuns.",
            "node|c_fury|0.04|+4% damage per rank.",
            "node|c_ironchef|0.25|Attacks hit 25% harder.",
            "node|c_wellfed|3|+3 max HP per rank.",
            "node|c_apron|1|+1 armour per rank (5% less damage each).",
            "node|c_n_slide|0|New dash: Slide along a buttery trail. Enemies on it are slowed for 3 seconds.",
            "node|c_n_picnic|0|New skill: Spread a blanket for 5 seconds. Stand on it to heal 2 HP every half second.",
            "node|c_leftovers|1|Heal 1 HP per rank whenever you defeat an enemy.",
            "node|c_seconds|6|+6 max HP per rank.",
            "node|c_homecooking|2|+2 armour.",
            "node|c_mise|0.04|Skills recharge 4% faster per rank.",
            "node|c_n_pepper|0|New skill: Fling pepper in a cone: hurts and slows enemies by half for 3 seconds.",
            "node|c_seasoning|0.08|Skills hit 8% harder per rank.",
            "node|c_n_hotpot|0|New skill: Lob a pot ahead. It leaves a scalding puddle for 4 seconds.",
            "node|c_n_pin|0|New skill: Throw a rolling pin that flies out and comes back, hitting everything twice.",
            "node|c_preptime|0.05|Skills recharge 5% faster per rank.",
            "node|c_signature|0.25|Skills hit 25% harder.",
            "gear|weapon|8;10;13;16;20;",
            "gear|armour|0;2;4;6;9;",
        };

        static void Upgrade3(GameContent c)
        {
            var old = new HashSet<string>(V2Sigs);
            var fresh = Create();

            for (int i = 0; i < c.recipes.Count; i++)
            {
                var f = fresh.recipes.Find(x => x.id == c.recipes[i].id);
                if (f != null && old.Contains(Sig(c.recipes[i]))) c.recipes[i].buffs = f.buffs;
            }
            foreach (var cls in c.classes)
            {
                var f = fresh.classes.Find(x => x.id == cls.id);
                if (f == null) continue;
                if (old.Contains(Sig(cls))) { cls.baseMaxHp = f.baseMaxHp; cls.description = f.description; }
            }
            for (int i = 0; i < c.synergies.Count; i++)
            {
                var f = fresh.synergies.Find(x => x.id == c.synergies[i].id);
                if (f != null && old.Contains(Sig(c.synergies[i]))) { c.synergies[i].bonus = f.bonus; c.synergies[i].description = f.description; }
            }
            foreach (var n in c.skillNodes)
            {
                var f = fresh.skillNodes.Find(x => x.id == n.id);
                if (f != null && old.Contains(Sig(n))) { n.valuePerRank = f.valuePerRank; n.description = f.description; }
            }
            if (old.Contains(Sig("weapon", c.weaponTiers)) && c.weaponTiers.Count == fresh.weaponTiers.Count)
                for (int i = 0; i < c.weaponTiers.Count; i++) c.weaponTiers[i].value = fresh.weaponTiers[i].value;
            if (old.Contains(Sig("armour", c.armourTiers)) && c.armourTiers.Count == fresh.armourTiers.Count)
                for (int i = 0; i < c.armourTiers.Count; i++) c.armourTiers[i].value = fresh.armourTiers[i].value;

            c.contentVersion = 3;
        }
    }
}
