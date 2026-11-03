// Hearth & Hollow — cooking v10 (Design v2, ticket 08). Pure C#, no UnityEngine.
//
// A meal is a recipe, a quality and an optional element ingredient.
//   • Ingredients have no grades any more: a recipe just needs so many of each. Better ingredients come from going
//     deeper (new, rarer ones), not from rolling a grade.
//   • Quality (small): each cook rolls Plain / Fine / Prime (tuning.qualityFineChance / qualityPrimeChance, plus the
//     station level's bonus and the minigame's nudge, ticket 11). Quality multiplies the meal's stats a little
//     (tuning.gradeMultUncommon / gradeMultRare, ×1.1 / ×1.2).
//   • Tier (big): a recipe can have a stronger version (RecipeData.tier 2, upgradeOf) that needs an ingredient
//     only found deep in a dungeon.
//   • The element slot (the old accelerator slot): one ingredient that either infuses (the run's attacks without
//     their own element take it) or guards (attacks of that element hit you for half). One infusion per tray.
// Cooked meals wait in the meal pantry; you pack up to traySize on the tray and they're eaten as you set out.
// Class multipliers and combos are worked out then, at packing time, never at the stove.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// What the player picked at the stove: an optional element ingredient (and, later, the minigame's nudge).
    [Serializable] public class CookChoice
    {
        public List<int> lineGrades = new List<int>();   // unused since ingredients lost their grades (kept so old data loads)
        public string accelId;                            // the element ingredient (empty = none)
        public int accelGrade;                            // unused (kept so old data loads)
        public float qualityNudge;                        // 0..1 from the cooking minigame (ticket 11): a small push towards better quality
    }

    public static class Cooking
    {
        public const int Grades = 3;
        public static readonly string[] GradeNames = { "Plain", "Fine", "Prime" };   // meal quality (Ryan's names)
        public static readonly string[] GradeGems = { "⚪", "🟢", "🔵" };

        public static int ClampGrade(int g) { return g < 0 ? 0 : g >= Grades ? Grades - 1 : g; }
        /// Meal quality name: Plain / Fine / Prime.
        public static string GradeName(int g) { return GradeNames[ClampGrade(g)]; }
        public static string GradeGem(int g) { return GradeGems[ClampGrade(g)]; }

        /// Stats multiplier for a meal's quality (small on purpose: tiers are the big jump).
        public static float GradeMult(TuningData t, int g)
        {
            g = ClampGrade(g);
            return g == 2 ? t.gradeMultRare : g == 1 ? t.gradeMultUncommon : 1f;
        }

        // ───────── the element slot ─────────
        /// Can go in a meal's element slot: it infuses or guards (or, on old hazard routes, guards a hazard).
        public static bool IsAccelerator(IngredientData i)
        {
            return i != null && (i.infuse != Element.None || i.guard != Element.None || i.accelGuard != Hazard.None);
        }

        /// "🔥 Infuses Fire" / "❄️ Guards against Frost" for an element ingredient.
        public static string SlotText(IngredientData i)
        {
            if (i == null) return "";
            if (i.infuse != Element.None) return Elements.Emoji(i.infuse) + " Infuses " + Elements.Name(i.infuse);
            if (i.guard != Element.None) return Elements.Emoji(i.guard) + " Guards against " + Elements.Name(i.guard);
            if (i.accelGuard != Hazard.None) return Routes.HazardEmoji(i.accelGuard) + " Guards against " + Routes.HazardName(i.accelGuard).ToLower();
            return "";
        }

        /// Always-available meals (Camp Bread): shown as a permanent card by the meal pantry, packed without cooking.
        public static List<RecipeData> AlwaysMeals(GameContent c) { return c.recipes.FindAll(r => r.always); }

        public static List<IngredientData> Accelerators(GameContent c) { return c.ingredients.FindAll(IsAccelerator); }

        static IngredientData SlotIngredient(GameContent c, string accelId) { return string.IsNullOrEmpty(accelId) ? null : c.Ingredient(accelId); }

        /// The element a meal infuses (None if its slot is empty or guards).
        public static Element InfusionOf(GameContent c, string accelId) { var i = SlotIngredient(c, accelId); return i != null ? i.infuse : Element.None; }
        public static Element GuardOf(GameContent c, string accelId) { var i = SlotIngredient(c, accelId); return i != null ? i.guard : Element.None; }
        /// The element a meal's element slot carries, infused or guarded (None = empty slot) — tints its card.
        public static Element SlotElement(GameContent c, string accelId) { var e = InfusionOf(c, accelId); return e != Element.None ? e : GuardOf(c, accelId); }

        /// The run's infused element from a set of meals: the first infusing meal (a tray only ever holds one).
        public static Element Infusion(GameContent c, List<EatenMeal> meals)
        {
            if (meals != null) foreach (var m in meals) { var e = InfusionOf(c, m.accelId); if (e != Element.None) return e; }
            return Element.None;
        }

        /// Every element the meals guard against.
        public static List<Element> Guards(GameContent c, List<EatenMeal> meals)
        {
            var list = new List<Element>();
            if (meals != null) foreach (var m in meals) { var e = GuardOf(c, m.accelId); if (e != Element.None && !list.Contains(e)) list.Add(e); }
            return list;
        }

        // ───────── what you need / what you have ─────────
        /// Units needed per ingredient for a choice: the recipe's ingredients plus one for the element slot.
        public static Dictionary<string, int> Needs(GameContent c, Profile p, RecipeData r, CookChoice ch)
        {
            var need = new Dictionary<string, int>();
            if (r != null && r.ingredients != null)
                foreach (var ic in r.ingredients)
                {
                    if (string.IsNullOrEmpty(ic.ingredientId) || ic.count <= 0) continue;
                    int n; need.TryGetValue(ic.ingredientId, out n);
                    need[ic.ingredientId] = n + ic.count;
                }
            if (ch != null && !string.IsNullOrEmpty(ch.accelId))
            {
                int n; need.TryGetValue(ch.accelId, out n);
                need[ch.accelId] = n + 1;
            }
            return need;
        }

        public static bool MealPantryFull(GameContent c, Profile p)
        {
            return p.mealPantry != null && p.mealPantry.Count >= Kitchen.PantrySlots(c, p);
        }

        public static bool CanCook(GameContent c, Profile p, RecipeData r, CookChoice ch, out string reason)
        {
            if (r == null) { reason = "Unknown recipe"; return false; }
            if (r.always) { reason = r.name + " is always available — pack it straight onto the tray"; return false; }
            if (!Kitchen.Unlocked(c, p, r)) { reason = "Needs the " + r.station + " at level " + r.stationLevel + " (upgrade it in the Kitchen)"; return false; }
            if (MealPantryFull(c, p)) { reason = "The meal pantry is full — pack or throw out a meal first"; return false; }
            if (ch != null && !string.IsNullOrEmpty(ch.accelId) && !IsAccelerator(c.Ingredient(ch.accelId)))
            { reason = "That can't go in the element slot"; return false; }
            foreach (var kv in Needs(c, p, r, ch))
                if (Economy.PantryCount(p, kv.Key) < kv.Value)
                {
                    var ing = c.Ingredient(kv.Key);
                    reason = "Not enough " + (ing != null ? ing.name : kv.Key) + (ing != null && ing.deep ? " (found deep in the dungeons)" : "");
                    return false;
                }
            reason = null;
            return true;
        }

        /// A plain choice (no element ingredient), or null if the recipe can't be made from the pantry.
        public static CookChoice DefaultChoice(GameContent c, Profile p, RecipeData r)
        {
            if (r == null) return null;
            foreach (var kv in Needs(c, p, r, null)) if (Economy.PantryCount(p, kv.Key) < kv.Value) return null;
            return new CookChoice();
        }

        /// The recipe can be made from what's in the pantry (ignoring whether the meal pantry has room).
        public static bool HaveIngredients(GameContent c, Profile p, RecipeData r) { return DefaultChoice(c, p, r) != null; }

        // ───────── the quality roll ─────────
        /// Chance of each quality (Plain, Fine, Prime), with the station level's bonus and the minigame's nudge.
        public static float[] Odds(GameContent c, Profile p, RecipeData r, CookChoice ch)
        {
            float stationBonus = r != null && p != null ? Math.Max(0f, Kitchen.Jackpot(c, p, r.station) - c.tuning.jackpotChance) : 0f;
            return Odds(c.tuning, stationBonus, ch != null ? ch.qualityNudge : 0f);
        }

        public static float[] Odds(TuningData t, float stationBonus, float nudge)
        {
            nudge = Math.Max(0f, Math.Min(1f, nudge));
            float prime = Math.Max(0f, t.qualityPrimeChance + stationBonus * 0.5f + nudge * t.qualityNudgePrime);
            float fine = Math.Max(0f, t.qualityFineChance + stationBonus + nudge * t.qualityNudgeFine);
            if (prime > 1f) prime = 1f;
            if (fine > 1f - prime) fine = 1f - prime;
            return new[] { 1f - fine - prime, fine, prime };
        }

        // ───────── the cooking minigame (ticket 11) ─────────
        public enum Timing { Missed, Good, Perfect }

        /// Where the heat marker is (0..1) after this many seconds: one sweep across the bar, then it stops at the end.
        public static float CookMarkerAt(TuningData t, float seconds)
        {
            float dur = t.cookGameSeconds > 0.05f ? t.cookGameSeconds : 1.6f;
            return Math.Max(0f, Math.Min(1f, seconds / dur));
        }

        public static Timing CookTiming(TuningData t, float marker)
        {
            float d = Math.Abs(marker - t.cookGameTarget);
            if (d <= t.cookGamePerfect * 0.5f) return Timing.Perfect;
            if (d <= t.cookGameGood * 0.5f) return Timing.Good;
            return Timing.Missed;
        }

        /// The quality nudge (0..1) for stopping the marker here. Missing — or skipping with Quick cook — is just 0: never a penalty.
        public static float CookNudge(TuningData t, float marker)
        {
            switch (CookTiming(t, marker))
            {
                case Timing.Perfect: return 1f;
                case Timing.Good: return Math.Max(0f, Math.Min(1f, t.cookGameGoodNudge));
            }
            return 0f;
        }

        public static int Roll(float[] odds, float v)
        {
            float acc = 0f;
            for (int g = 0; g < odds.Length; g++) { acc += odds[g]; if (v < acc) return g; }
            for (int g = odds.Length - 1; g >= 0; g--) if (odds[g] > 0f) return g;
            return 0;
        }

        /// Cook: spend the ingredients, roll the quality and put the meal in the meal pantry.
        public static CookedMeal Cook(GameContent c, Profile p, RecipeData r, CookChoice ch, Rng rng, out string reason)
        {
            if (!CanCook(c, p, r, ch, out reason)) return null;
            foreach (var kv in Needs(c, p, r, ch)) Economy.RemoveFromPantry(p, kv.Key, kv.Value);
            int grade = Roll(Odds(c, p, r, ch), rng.Value());
            var meal = new CookedMeal { recipeId = r.id, grade = grade };
            if (ch != null && !string.IsNullOrEmpty(ch.accelId)) meal.accelId = ch.accelId;
            p.mealPantry.Add(meal);
            return meal;
        }

        // ───────── meal pantry ↔ tray ─────────
        /// The tray (Profile.tray) mirrors the packed meals' recipe ids, for code that only needs names.
        public static void SyncTray(Profile p)
        {
            if (p.packed == null) p.packed = new List<CookedMeal>();
            p.tray.Clear();
            for (int i = 0; i < p.packed.Count; i++) p.tray.Add(p.packed[i].recipeId);
        }

        // ───────── the spice rack (ticket 27): add the element ingredient after cooking ─────────
        /// Season a cooked meal (in the meal pantry or on the tray) with one element ingredient: spends it.
        public static bool Season(GameContent c, Profile p, CookedMeal m, string ingId, out string reason)
        {
            var ing = string.IsNullOrEmpty(ingId) ? null : c.Ingredient(ingId);
            if (m == null || (!p.mealPantry.Contains(m) && !p.packed.Contains(m))) { reason = "Pick a cooked meal"; return false; }
            if (!string.IsNullOrEmpty(m.accelId)) { reason = "That meal is already seasoned"; return false; }
            if (ing == null || (ing.infuse == Element.None && ing.guard == Element.None)) { reason = "Pick an element ingredient"; return false; }
            if (Economy.PantryCount(p, ingId) <= 0) { reason = "You have no " + ing.name + " left"; return false; }
            if (ing.infuse != Element.None && p.packed.Contains(m))
                foreach (var q in p.packed)
                    if (q != m && InfusionOf(c, q.accelId) != Element.None) { reason = "Only one infused meal per tray"; return false; }
            Economy.RemoveFromPantry(p, ingId, 1);
            m.accelId = ingId;
            reason = null; return true;
        }

        public static bool Pack(GameContent c, Profile p, int pantryIndex, out string reason)
        {
            if (pantryIndex < 0 || pantryIndex >= p.mealPantry.Count) { reason = "No such meal"; return false; }
            if (p.packed.Count >= c.tuning.traySize) { reason = "The tray is full"; return false; }
            var m = p.mealPantry[pantryIndex];
            if (InfusionOf(c, m.accelId) != Element.None)
                foreach (var q in p.packed)
                    if (InfusionOf(c, q.accelId) != Element.None) { reason = "Only one infused meal per tray"; return false; }
            p.packed.Add(m);
            p.mealPantry.RemoveAt(pantryIndex);
            SyncTray(p);
            reason = null; return true;
        }

        /// Pack an always-available meal (Camp Bread): a fresh Plain helping, no cooking, no meal-pantry slot.
        public static bool PackAlways(GameContent c, Profile p, string recipeId, out string reason)
        {
            var r = c.Recipe(recipeId);
            if (r == null || !r.always) { reason = "That meal isn't always available"; return false; }
            if (p.packed.Count >= c.tuning.traySize) { reason = "The tray is full"; return false; }
            p.packed.Add(new CookedMeal { recipeId = r.id, grade = 0 });
            SyncTray(p);
            reason = null; return true;
        }

        /// Take a meal off the tray, back into the meal pantry (allowed even when the pantry is full — nothing is lost).
        /// An always-available meal just goes back to its permanent card.
        public static bool Unpack(GameContent c, Profile p, int slot)
        {
            if (slot < 0 || slot >= p.packed.Count) return false;
            var r = c.Recipe(p.packed[slot].recipeId);
            if (r == null || !r.always) p.mealPantry.Add(p.packed[slot]);
            p.packed.RemoveAt(slot);
            SyncTray(p);
            return true;
        }

        public static void MovePacked(Profile p, int from, int to)
        {
            if (from < 0 || from >= p.packed.Count || from == to) return;
            if (to < 0) to = 0; if (to >= p.packed.Count) to = p.packed.Count - 1;
            var m = p.packed[from]; p.packed.RemoveAt(from); p.packed.Insert(to, m);
            SyncTray(p);
        }

        public static bool Discard(Profile p, int pantryIndex)
        {
            if (pantryIndex < 0 || pantryIndex >= p.mealPantry.Count) return false;
            p.mealPantry.RemoveAt(pantryIndex);
            return true;
        }

        /// Meals as eaten, in order, with diminishing returns for repeats of the same recipe.
        public static List<EatenMeal> ToEaten(GameContent c, List<CookedMeal> meals)
        {
            var list = new List<EatenMeal>();
            if (meals == null) return list;
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < meals.Count; i++)
            {
                var m = meals[i];
                if (m == null || c.Recipe(m.recipeId) == null) continue;
                int n; seen.TryGetValue(m.recipeId, out n);
                list.Add(new EatenMeal { recipeId = m.recipeId, potency = Meals.PotencyFor(c.tuning, n), grade = m.grade, accelId = m.accelId, accelGrade = m.accelGrade });
                seen[m.recipeId] = n + 1;
            }
            return list;
        }

        /// The packed tray as it will be eaten.
        public static List<EatenMeal> Packed(GameContent c, Profile p) { return ToEaten(c, p != null ? p.packed : null); }

        /// A cooked meal as a one-meal list (for previews of a single meal's buffs).
        public static EatenMeal AsEaten(CookedMeal m)
        {
            return new EatenMeal { recipeId = m.recipeId, potency = 1f, grade = m.grade, accelId = m.accelId, accelGrade = m.accelGrade };
        }

        /// One meal's buffs for a class: its stats × quality × class bonus × potency. (The element slot adds no stats.)
        public static List<StatMod> MealBuffs(GameContent c, ClassData cls, EatenMeal m)
        {
            var list = new List<StatMod>();
            var r = c.Recipe(m.recipeId);
            if (r == null) return list;
            float mult = GradeMult(c.tuning, m.grade) * m.potency;
            if (r.buffs != null) foreach (var b in r.buffs) Add(list, b.stat, b.value * mult * Meals.StatMult(cls, b.stat));
            return list;
        }

        static void Add(List<StatMod> list, BuffStat st, float v)
        {
            var x = list.Find(s => s.stat == st);
            if (x == null) list.Add(new StatMod { stat = st, value = v }); else x.value += v;
        }

        /// Speed and heal-on-kill from meals (these feed the dungeon's CombatMods rather than PlayerStats).
        public static void AddMealMods(GameContent c, ClassData cls, List<EatenMeal> meals, CombatMods m)
        {
            if (m == null || meals == null || meals.Count == 0) return;
            float[] sum = Meals.TotalBonus(c, cls, meals);
            m.moveSpeed += sum[(int)BuffStat.MoveSpeedPct];
            m.healOnKill += (int)Math.Round(sum[(int)BuffStat.HealOnKill]);
        }

        /// Cooked meals whose recipe no longer exists are dropped.
        public static void DropUnknownMeals(GameContent c, Profile p)
        {
            if (p.mealPantry != null) p.mealPantry.RemoveAll(m => m == null || c.Recipe(m.recipeId) == null || c.Recipe(m.recipeId).always);   // always-available meals never sit in the pantry
            if (p.packed != null) p.packed.RemoveAll(m => m == null || c.Recipe(m.recipeId) == null);
            SyncTray(p);
        }

        // ───────── save upgrade ─────────
        /// saveVersion 6: the old tray only reserved ingredients (nothing was spent), so it is simply emptied.
        public static void UpgradeSave(Profile p)
        {
            if (p.mealPantry == null) p.mealPantry = new List<CookedMeal>();
            if (p.packed == null) p.packed = new List<CookedMeal>();
            if (p.saveVersion < 6) { p.packed.Clear(); p.tray.Clear(); p.saveVersion = 6; }
            SyncTray(p);
        }

        /// saveVersion 10 (ticket 08): ingredients lost their grades — merge each ingredient's stacks into one.
        public static void MergeIngredientGrades(Profile p)
        {
            if (p.saveVersion >= 10 || p.pantry == null) return;
            var merged = new List<ItemStack>();
            foreach (var st in p.pantry)
            {
                if (st == null || string.IsNullOrEmpty(st.ingredientId) || st.count <= 0) continue;
                var into = merged.Find(x => x.ingredientId == st.ingredientId);
                if (into != null) into.count += st.count;
                else merged.Add(new ItemStack { ingredientId = st.ingredientId, count = st.count, grade = 0 });
            }
            p.pantry = merged;
            p.saveVersion = 10;
        }
    }
}
