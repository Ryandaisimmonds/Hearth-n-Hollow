// Hearth & Hollow — cooking v9: ingredient grades, the rarity roll, accelerators and the meal pantry. Pure C#, no UnityEngine.
//
// Ingredients and meals both come in three grades: 0 Plain · 1 Fine · 2 Prime.
// Cooking spends the ingredients straight away and rolls the meal's grade:
//   • each ingredient unit counts towards its grade (2 Plain + 1 Prime = 67% Plain, 33% Prime; all Prime = a Prime meal for certain);
//   • then a small chance of one grade higher: the jackpot (capped at Rare).
// A meal's grade multiplies its stats (tuning.gradeMult*). The optional accelerator adds its base boost (or a hazard guard);
// accelerators have no grade of their own (it uses up your lowest-grade one). Cooked meals wait in the meal pantry (tuning.mealPantrySlots); you pack up to
// traySize of them on the tray, and they're eaten as you set out. Favourite station and combos are worked out then, at
// packing time, never at the stove.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    /// What the player picked at the stove: one grade per ingredient line of the recipe, and an optional accelerator.
    [Serializable] public class CookChoice
    {
        public List<int> lineGrades = new List<int>();
        public string accelId;
        public int accelGrade;   // unused since accelerators lost their grade (kept so old data loads)
    }

    public static class Cooking
    {
        public const int Grades = 3;
        public static readonly string[] GradeNames = { "Plain", "Fine", "Prime" };   // meals use the same names as ingredients (Ryan, 30 Oct)
        public static readonly string[] GradeGems = { "⚪", "🟢", "🔵" };

        public static int ClampGrade(int g) { return g < 0 ? 0 : g >= Grades ? Grades - 1 : g; }
        /// Meal grade name: Plain / Fine / Prime (same as ingredients).
        public static string GradeName(int g) { return GradeNames[ClampGrade(g)]; }

        /// Ingredient grade names: Plain / Fine / Prime.
        public static readonly string[] IngredientGradeNames = { "Plain", "Fine", "Prime" };
        public static string IngGradeName(int g) { return IngredientGradeNames[ClampGrade(g)]; }
        public static string GradeGem(int g) { return GradeGems[ClampGrade(g)]; }

        public static float GradeMult(TuningData t, int g)
        {
            g = ClampGrade(g);
            return g == 2 ? t.gradeMultRare : g == 1 ? t.gradeMultUncommon : 1f;
        }

        // ───────── accelerators ─────────
        public static bool IsAccelerator(IngredientData i)
        {
            return i != null && ((i.accelBuffs != null && i.accelBuffs.Count > 0) || i.accelGuard != Hazard.None);
        }

        /// Always-available meals (Camp Bread): shown as a permanent card by the meal pantry, packed without cooking.
        public static List<RecipeData> AlwaysMeals(GameContent c) { return c.recipes.FindAll(r => r.always); }

        public static List<IngredientData> Accelerators(GameContent c) { return c.ingredients.FindAll(IsAccelerator); }

        // ───────── what you need / what you have ─────────
        static string Key(string id, int g) { return id + "#" + g; }

        /// Units needed per (ingredient, grade) for a choice — ingredient lines plus the accelerator (which takes your
        /// lowest-grade one left over after the lines).
        public static Dictionary<string, int> Needs(GameContent c, Profile p, RecipeData r, CookChoice ch)
        {
            var need = new Dictionary<string, int>();
            if (r != null && r.ingredients != null)
                for (int i = 0; i < r.ingredients.Count; i++)
                {
                    var ic = r.ingredients[i];
                    if (string.IsNullOrEmpty(ic.ingredientId) || ic.count <= 0) continue;
                    int g = LineGrade(ch, i);
                    int n; need.TryGetValue(Key(ic.ingredientId, g), out n);
                    need[Key(ic.ingredientId, g)] = n + ic.count;
                }
            if (ch != null && !string.IsNullOrEmpty(ch.accelId))
            {
                int pick = 0;
                for (int g = 0; g < Grades; g++)
                {
                    int u; need.TryGetValue(Key(ch.accelId, g), out u);
                    if (p != null && Economy.PantryCount(p, ch.accelId, g) - u >= 1) { pick = g; break; }
                }
                int n; need.TryGetValue(Key(ch.accelId, pick), out n);
                need[Key(ch.accelId, pick)] = n + 1;
            }
            return need;
        }

        public static int LineGrade(CookChoice ch, int line)
        {
            return ch != null && ch.lineGrades != null && line < ch.lineGrades.Count ? ClampGrade(ch.lineGrades[line]) : 0;
        }

        static void Split(string key, out string id, out int g)
        {
            int k = key.LastIndexOf('#');
            id = key.Substring(0, k); g = int.Parse(key.Substring(k + 1));
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
            { reason = "That can't go in the accelerator slot"; return false; }
            foreach (var kv in Needs(c, p, r, ch))
            {
                string id; int g; Split(kv.Key, out id, out g);
                if (Economy.PantryCount(p, id, g) < kv.Value)
                {
                    var ing = c.Ingredient(id);
                    reason = "Not enough " + IngGradeName(g) + " " + (ing != null ? ing.name : id);
                    return false;
                }
            }
            reason = null;
            return true;
        }

        /// The cheapest choice that works: each line uses the lowest grade you have enough of (no accelerator).
        /// Null if the recipe can't be made at all.
        public static CookChoice DefaultChoice(GameContent c, Profile p, RecipeData r)
        {
            var ch = new CookChoice();
            if (r == null) return null;
            var used = new Dictionary<string, int>();
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                var ic = r.ingredients[i];
                int pick = -1;
                for (int g = 0; g < Grades && pick < 0; g++)
                {
                    int u; used.TryGetValue(Key(ic.ingredientId, g), out u);
                    if (Economy.PantryCount(p, ic.ingredientId, g) - u >= ic.count) pick = g;
                }
                if (pick < 0) return null;
                int n; used.TryGetValue(Key(ic.ingredientId, pick), out n);
                used[Key(ic.ingredientId, pick)] = n + ic.count;
                ch.lineGrades.Add(pick);
            }
            return ch;
        }

        /// The recipe can be made from what's in the pantry (ignoring whether the meal pantry has room).
        public static bool HaveIngredients(GameContent c, Profile p, RecipeData r) { return DefaultChoice(c, p, r) != null; }

        // ───────── the rarity roll ─────────
        /// Chance of each meal grade (Plain, Fine, Prime) for a choice.
        /// Odds with the station's level bonus (the better-meal chance grows with each level).
        public static float[] Odds(GameContent c, Profile p, RecipeData r, CookChoice ch)
        {
            return Odds(c, r, ch, r != null && p != null ? Kitchen.Jackpot(c, p, r.station) : c.tuning.jackpotChance);
        }

        public static float[] Odds(GameContent c, RecipeData r, CookChoice ch) { return Odds(c, r, ch, c.tuning.jackpotChance); }

        public static float[] Odds(GameContent c, RecipeData r, CookChoice ch, float jackpot)
        {
            var t = c.tuning;
            var share = new float[Grades];
            int units = 0;
            var per = new int[Grades];
            if (r != null && r.ingredients != null)
                for (int i = 0; i < r.ingredients.Count; i++)
                {
                    var ic = r.ingredients[i];
                    if (ic.count <= 0) continue;
                    per[LineGrade(ch, i)] += ic.count; units += ic.count;
                }
            if (units == 0) { share[0] = 1f; return share; }   // no ingredients: always Plain
            for (int g = 0; g < Grades; g++) share[g] = per[g] / (float)units;
            float up = Math.Max(0f, Math.Min(1f, jackpot));
            var odds = new float[Grades];
            for (int g = 0; g < Grades; g++)
            {
                if (g == Grades - 1) { odds[g] += share[g]; continue; }
                odds[g] += share[g] * (1f - up);
                odds[g + 1] += share[g] * up;
            }
            return odds;
        }

        public static int Roll(float[] odds, float v)
        {
            float acc = 0f;
            for (int g = 0; g < odds.Length; g++) { acc += odds[g]; if (v < acc) return g; }
            for (int g = odds.Length - 1; g >= 0; g--) if (odds[g] > 0f) return g;
            return 0;
        }

        /// Cook: spend the ingredients, roll the grade and put the meal in the meal pantry.
        public static CookedMeal Cook(GameContent c, Profile p, RecipeData r, CookChoice ch, Rng rng, out string reason)
        {
            if (!CanCook(c, p, r, ch, out reason)) return null;
            foreach (var kv in Needs(c, p, r, ch))
            {
                string id; int g; Split(kv.Key, out id, out g);
                Economy.RemoveFromPantry(p, id, kv.Value, g);
            }
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

        public static bool Pack(GameContent c, Profile p, int pantryIndex, out string reason)
        {
            if (pantryIndex < 0 || pantryIndex >= p.mealPantry.Count) { reason = "No such meal"; return false; }
            if (p.packed.Count >= c.tuning.traySize) { reason = "The tray is full"; return false; }
            p.packed.Add(p.mealPantry[pantryIndex]);
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

        /// One meal's buffs for a class: its stats × grade × class bonus (favourite station etc.) × potency, plus the accelerator.
        public static List<StatMod> MealBuffs(GameContent c, ClassData cls, EatenMeal m)
        {
            var list = new List<StatMod>();
            var r = c.Recipe(m.recipeId);
            if (r == null) return list;
            float mult = GradeMult(c.tuning, m.grade) * Meals.ClassMult(cls, r) * m.potency;
            if (r.buffs != null) foreach (var b in r.buffs) Add(list, b.stat, b.value * mult);
            var a = string.IsNullOrEmpty(m.accelId) ? null : c.Ingredient(m.accelId);
            if (a != null && a.accelBuffs != null)
                foreach (var b in a.accelBuffs) Add(list, b.stat, b.value * m.potency);   // accelerators have no grade: always the base boost
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

        // ───────── ingredient grades from the Hollow (contentVersion 14) ─────────
        /// Chance an ingredient pickup is Fine / Prime: deeper floors and harder routes drop better; elites, bosses,
        /// a perfect gather and a Lucky route add to the Prime chance.
        public static void DropOdds(TuningData t, int floor, int difficulty, bool elite, bool boss, bool perfect, bool lucky, out float fine, out float prime)
        {
            int deeper = Math.Max(0, floor - 1), diff = Math.Max(0, difficulty);
            prime = t.dropRare + t.dropRarePerFloor * deeper + t.dropRarePerDifficulty * diff
                  + (elite ? t.dropEliteRareBonus : 0f) + (boss ? t.dropBossRareBonus : 0f) + (perfect ? t.dropPerfectGatherRare : 0f) + (lucky ? t.dropLuckyRare : 0f);
            prime = Math.Max(0f, Math.Min(0.9f, prime));
            fine = Math.Max(0f, Math.Min(1f - prime, t.dropUncommon + t.dropUncommonPerFloor * deeper + t.dropUncommonPerDifficulty * diff));
        }

        /// Grade of an ingredient picked up on <paramref name="floor"/> (1-based) of a route of <paramref name="difficulty"/>.
        public static int RollDropGrade(TuningData t, int floor, int difficulty, bool elite, bool boss, bool perfect, bool lucky, Rng rng)
        {
            float unc, rare;
            DropOdds(t, floor, difficulty, elite, boss, perfect, lucky, out unc, out rare);
            float v = rng.Value();
            if (v < rare) return 2;
            if (v < rare + unc) return 1;
            return 0;
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
            if (p.saveVersion >= 6) { SyncTray(p); return; }
            p.packed.Clear();
            p.tray.Clear();
            p.saveVersion = 6;
        }
    }
}
