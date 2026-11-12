// Hearth & Hollow — the tray. Pure C#.
// Ticket 52 (replaces ticket 44's typed slots): any three meals. Every meal gives a base of both 🗡️ attack and ❤️ HP,
// tilted by its station (BBQ a little more attack, Pot a little more HP, Sandwich even), scaled by its tier; quality
// multiplies everything. On top sits the recipe's special (its buffs): Pot = healing / defence, BBQ = fighting,
// Sandwich = utility. Camp Bread (an "always" meal) gives half a Sandwich's base and no special.
// (The TraySlot enum, Assign / Missing / CanAdd stay so old callers and saves still work: every slot is Free now.)
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public enum TraySlot { Attack, Health, Free }

    public static class Tray
    {
        public static readonly TraySlot[] Slots = { TraySlot.Attack, TraySlot.Health, TraySlot.Free };

        public static bool IsAttackStat(BuffStat s) { return s == BuffStat.Attack || s == BuffStat.DamagePct || s == BuffStat.CritPct; }
        public static bool IsHealthStat(BuffStat s)
        {
            return s == BuffStat.MaxHp || s == BuffStat.MaxHpPct || s == BuffStat.Armour || s == BuffStat.Regen || s == BuffStat.HealOnKill;
        }

        public static bool IsAttackMeal(RecipeData r) { return r != null && r.buffs != null && r.buffs.Exists(b => IsAttackStat(b.stat)); }
        public static bool IsHealthMeal(RecipeData r) { return r != null && r.buffs != null && r.buffs.Exists(b => IsHealthStat(b.stat)); }

        /// Can this recipe sit in this slot? (Ticket 52: any meal goes anywhere.)
        public static bool Fits(RecipeData r, TraySlot s) { return r != null; }

        /// Ticket 52: the station's icon: what family of special the dish has.
        public static string StationEmoji(Station st) { return st == Station.Pot ? "🍲" : st == Station.BBQ ? "🔥" : "🥪"; }
        public static string StationEmoji(RecipeData r) { return r == null ? "🍽️" : r.always ? "🍞" : StationEmoji(r.station); }

        public static string SlotEmoji(TraySlot s) { return s == TraySlot.Attack ? "🗡️" : s == TraySlot.Health ? "❤️" : "✨"; }
        public static string SlotName(TraySlot s) { return s == TraySlot.Attack ? "Attack" : s == TraySlot.Health ? "Health" : "Free"; }

        /// Which slot each meal fills (same order as <paramref name="recipeIds"/>). Typed slots are filled first, by real
        /// meals before Camp Bread. A meal that fits nothing left is still placed (Free) so nothing vanishes.
        public static TraySlot[] Assign(GameContent c, IList<string> recipeIds)
        {
            int n = recipeIds != null ? recipeIds.Count : 0;
            var best = new TraySlot[n];
            for (int i = 0; i < n; i++) best[i] = TraySlot.Free;
            return best;   // ticket 52: no typed slots
#pragma warning disable 0162
            if (n == 0) return best;
            var recs = new RecipeData[n];
            for (int i = 0; i < n; i++) recs[i] = c.Recipe(recipeIds[i]);
            int bestScore = int.MinValue;
            var cur = new int[n];
            Search(recs, cur, 0, new bool[Slots.Length], ref bestScore, best);
            return best;
#pragma warning restore 0162
        }

        static void Search(RecipeData[] recs, int[] cur, int i, bool[] used, ref int bestScore, TraySlot[] best)
        {
            if (i == recs.Length)
            {
                int score = 0;
                for (int k = 0; k < recs.Length; k++)
                {
                    if (cur[k] < 0) { score -= 100; continue; }   // overflow (more meals than slots, or nothing fits)
                    var s = Slots[cur[k]];
                    if (s != TraySlot.Free) score += 10 + (recs[k] != null && !recs[k].always ? 1 : 0);   // typed slots filled, by real meals first
                }
                if (score > bestScore) { bestScore = score; for (int k = 0; k < recs.Length; k++) best[k] = cur[k] < 0 ? TraySlot.Free : Slots[cur[k]]; }
                return;
            }
            bool any = false;
            for (int s = 0; s < Slots.Length; s++)
            {
                if (used[s] || !Fits(recs[i], Slots[s])) continue;
                any = true;
                used[s] = true; cur[i] = s;
                Search(recs, cur, i + 1, used, ref bestScore, best);
                used[s] = false;
            }
            if (!any) { cur[i] = -1; Search(recs, cur, i + 1, used, ref bestScore, best); }
        }

        /// The typed slots (Attack / Health) nothing fills yet.
        public static List<TraySlot> Missing(GameContent c, IList<string> recipeIds) { return new List<TraySlot>(); }   // ticket 52: nothing is required

        /// Would the tray still be completable after adding this recipe? Null if yes, else why not.
        public static string CanAdd(GameContent c, IList<string> recipeIds, string recipeId)
        {
            int size = c.tuning.traySize;
            int n = recipeIds != null ? recipeIds.Count : 0;
            if (n >= size) return "The tray is full";
            var next = new List<string>();
            if (recipeIds != null) next.AddRange(recipeIds);
            next.Add(recipeId);
            var missing = Missing(c, next);
            if (missing.Count > size - next.Count)
                return "Your tray needs " + Need(missing);
            return null;
        }

        /// "an 🗡️ Attack meal" / "a ❤️ Health meal" / "an 🗡️ Attack and a ❤️ Health meal".
        public static string Need(List<TraySlot> missing)
        {
            if (missing == null || missing.Count == 0) return "";
            if (missing.Count == 1) return (missing[0] == TraySlot.Attack ? "an " : "a ") + SlotEmoji(missing[0]) + " " + SlotName(missing[0]) + " meal";
            return "an 🗡️ Attack and a ❤️ Health meal";
        }

        /// Ticket 52: a meal's base (before quality, helpings and class): both attack and HP, tilted by station, × tier.
        public static void Base(GameContent c, RecipeData r, out float attack, out float hp)
        {
            var t = c.tuning;
            attack = 0f; hp = 0f;
            if (r == null) return;
            if (r.station == Station.BBQ) { attack = t.baseBbqAttack; hp = t.baseBbqHp; }
            else if (r.station == Station.Pot) { attack = t.basePotAttack; hp = t.basePotHp; }
            else { attack = t.baseSandwichAttack; hp = t.baseSandwichHp; }
            float k = r.always ? t.baseAlwaysMult : r.tier >= 2 ? t.baseTier2Mult : 1f;
            attack *= k; hp *= k;
        }

        /// What a recipe gives before quality, helpings and class bonuses: the base 🗡️ + ❤️, then its special.
        public static List<StatMod> RawBuffs(GameContent c, RecipeData r, TraySlot slot)
        {
            var list = new List<StatMod>();
            if (r == null) return list;
            float a, h; Base(c, r, out a, out h);
            if (a > 0f) list.Add(new StatMod { stat = BuffStat.Attack, value = a });
            if (h > 0f) list.Add(new StatMod { stat = BuffStat.MaxHp, value = h });
            if (r.buffs != null) list.AddRange(r.buffs);
            return list;
        }

        /// The special part only (everything but the base).
        public static bool IsBaseStat(BuffStat s) { return s == BuffStat.Attack || s == BuffStat.MaxHp; }
    }
}
