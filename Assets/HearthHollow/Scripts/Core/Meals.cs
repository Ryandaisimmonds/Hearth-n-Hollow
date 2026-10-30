// Hearth & Hollow — cooking and meal buffs. Pure C#, no UnityEngine.
// Meals are a PRE-RUN system: you cook up to traySize meals at the hearth, and when you set out
// the whole tray is eaten at camp. Their buffs raise your stats for the entire run (more max HP,
// damage, armour…). Nothing is eaten — and nothing heals — inside the Hollow.
using System;
using System.Collections.Generic;

namespace HearthHollow.Core
{
    public static class Meals
    {
        // The tray is a plan: meals in it only RESERVE ingredients. Nothing leaves the pantry until you
        // set out (PayForTray), so removing a meal from the tray simply frees its ingredients again.

        /// Ingredients currently promised to meals in the tray.
        public static Dictionary<string, int> Reserved(GameContent c, Profile p)
        {
            var res = new Dictionary<string, int>();
            for (int i = 0; i < p.tray.Count; i++)
            {
                RecipeData r = c.Recipe(p.tray[i]);
                if (r == null) continue;
                foreach (var kv in Needs(r)) { int n; res.TryGetValue(kv.Key, out n); res[kv.Key] = n + kv.Value; }
            }
            return res;
        }

        /// Pantry count minus what the tray has reserved.
        public static int Available(GameContent c, Profile p, string ingredientId)
        {
            int n; Reserved(c, p).TryGetValue(ingredientId, out n);
            return Economy.PantryCount(p, ingredientId) - n;
        }

        public static bool CanCook(GameContent c, Profile p, string recipeId, out string reason)
        {
            RecipeData recipe = c.Recipe(recipeId);
            if (recipe == null) { reason = "Unknown recipe"; return false; }
            if (p.tray.Count >= c.tuning.traySize) { reason = "Tray is full"; return false; }
            Dictionary<string, int> reserved = Reserved(c, p);
            foreach (var kv in Needs(recipe))
            {
                int taken; reserved.TryGetValue(kv.Key, out taken);
                if (Economy.PantryCount(p, kv.Key) - taken < kv.Value)
                {
                    IngredientData ing = c.Ingredient(kv.Key);
                    reason = "Missing " + (ing != null ? ing.name : kv.Key);
                    return false;
                }
            }
            reason = null;
            return true;
        }

        /// Adds a meal to the tray. Its ingredients are reserved, not spent.
        public static bool Cook(GameContent c, Profile p, string recipeId)
        {
            string reason;
            if (!CanCook(c, p, recipeId, out reason)) return false;
            p.tray.Add(recipeId);
            return true;
        }

        /// Set-out: spend the tray's ingredients in tray order. A meal that can't be paid for any more is
        /// left behind (returns one message per dropped meal).
        public static List<string> PayForTray(GameContent c, Profile p)
        {
            var dropped = new List<string>();
            for (int i = 0; i < p.tray.Count; )
            {
                RecipeData recipe = c.Recipe(p.tray[i]);
                if (recipe == null) { p.tray.RemoveAt(i); continue; }
                Dictionary<string, int> need = Needs(recipe);
                bool ok = true;
                foreach (var kv in need) if (Economy.PantryCount(p, kv.Key) < kv.Value) { ok = false; break; }
                if (!ok)
                {
                    dropped.Add("Not enough ingredients for " + recipe.emoji + " " + recipe.name + " — left it at camp");
                    p.tray.RemoveAt(i);
                    continue;
                }
                foreach (var kv in need) Economy.RemoveFromPantry(p, kv.Key, kv.Value);
                i++;
            }
            return dropped;
        }

        /// Old saves spent ingredients when cooking: hand them back so the tray can reserve them instead.
        public static void RefundTray(GameContent c, Profile p)
        {
            for (int i = 0; i < p.tray.Count; i++)
            {
                RecipeData recipe = c.Recipe(p.tray[i]);
                if (recipe == null) continue;
                foreach (var kv in Needs(recipe)) Economy.AddToPantry(p, kv.Key, kv.Value);
            }
        }

        private static Dictionary<string, int> Needs(RecipeData r)
        {
            var need = new Dictionary<string, int>();
            if (r.ingredients == null) return need;
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                IngredientCost ic = r.ingredients[i];
                if (string.IsNullOrEmpty(ic.ingredientId) || ic.count <= 0) continue;
                int n; need.TryGetValue(ic.ingredientId, out n);
                need[ic.ingredientId] = n + ic.count;
            }
            return need;
        }

        /// Potency of the n-th helping of the same recipe (0-based): 1, secondHelping, thirdHelping, thirdHelping…
        public static float PotencyFor(TuningData t, int helpingIndex)
        {
            if (helpingIndex <= 0) return 1f;
            if (helpingIndex == 1) return t.secondHelping;
            return t.thirdHelping;
        }

        /// The tray as eaten meals, in tray order, with diminishing returns applied to repeats.
        public static List<EatenMeal> TrayToMeals(GameContent c, List<string> tray)
        {
            var list = new List<EatenMeal>();
            if (tray == null) return list;
            var seen = new Dictionary<string, int>();
            for (int i = 0; i < tray.Count; i++)
            {
                string id = tray[i];
                if (c.Recipe(id) == null) continue;
                int n; seen.TryGetValue(id, out n);
                list.Add(new EatenMeal { recipeId = id, potency = PotencyFor(c.tuning, n) });
                seen[id] = n + 1;
            }
            return list;
        }

        /// Potency the tray meal at `slot` will have (for UI previews).
        public static float TrayPotency(GameContent c, List<string> tray, int slot)
        {
            if (tray == null || slot < 0 || slot >= tray.Count) return 1f;
            int n = 0;
            for (int i = 0; i < slot; i++) if (tray[i] == tray[slot]) n++;
            return PotencyFor(c.tuning, n);
        }

        /// Set-out: eat the packed tray at camp. Buffs apply for the whole run. Returns events for toasts.
        public static List<GameEvent> PackForRun(GameContent c, RunState run)
        {
            var ev = new List<GameEvent>();
            run.eaten = TrayToMeals(c, run.tray);
            run.tray.Clear();
            run.activeSynergyIds.Clear();
            for (int i = 0; i < run.eaten.Count; i++)
            {
                RecipeData r = c.Recipe(run.eaten[i].recipeId);
                ev.Add(new GameEvent(GameEventType.MealEaten, run.eaten[i].potency, true, r != null ? r.name : run.eaten[i].recipeId));
            }
            List<SynergyData> active = ActiveSynergies(c, run.eaten);
            for (int i = 0; i < active.Count; i++)
            {
                run.activeSynergyIds.Add(active[i].id);
                ev.Add(new GameEvent(GameEventType.Synergy, active[i].allBuffMultiplier, true, active[i].name));
            }
            return ev;
        }

        /// The meal eaten most often in a set, and how many times.
        static RecipeData MostRepeated(List<RecipeData> recipes, out int count)
        {
            count = 0; RecipeData best = null;
            for (int i = 0; i < recipes.Count; i++)
            {
                int n = 0;
                for (int j = 0; j < recipes.Count; j++) if (recipes[j].id == recipes[i].id) n++;
                if (n > count) { count = n; best = recipes[i]; }
            }
            return best;
        }

        /// How much a class gets out of one meal: the class's general meal bonus (Cook ×1.25), times its
        /// favourite-station bonus when the meal comes from that station (Warden BBQ, Ranger Sandwich, Cook Pot).
        public static float ClassMult(ClassData cls, RecipeData r)
        {
            if (cls == null) return 1f;
            float m = cls.mealBuffMult;
            if (IsFavourite(cls, r)) m *= 1f + cls.favouriteBonus;
            return m;
        }

        public static bool IsFavourite(ClassData cls, RecipeData r)
        {
            return cls != null && r != null && cls.favouriteBonus > 0f && r.station == cls.favouriteStation;
        }

        /// Everything a set of meals adds, indexed by (int)BuffStat: each meal's buffs × helping potency ×
        /// class meal bonus (incl. favourite station) × combo multipliers, plus combo bonuses (incl. Comfort Food's extra helping).
        public static float[] TotalBonus(GameContent c, ClassData cls, List<EatenMeal> meals)
        {
            float[] sum = new float[Enum.GetValues(typeof(BuffStat)).Length];
            if (meals == null || meals.Count == 0) return sum;
            float mealMult = cls != null ? cls.mealBuffMult : 1f;
            var recipes = new List<RecipeData>();
            for (int i = 0; i < meals.Count; i++) { RecipeData r = c.Recipe(meals[i].recipeId); if (r != null) recipes.Add(r); }

            float synMult = 1f;
            List<SynergyData> active = ActiveSynergies(c, meals);
            for (int i = 0; i < active.Count; i++)
            {
                synMult *= active[i].allBuffMultiplier;
                if (active[i].bonus != null)
                    for (int b = 0; b < active[i].bonus.Count; b++)
                        sum[(int)active[i].bonus[b].stat] += active[i].bonus[b].value * mealMult;
            }
            for (int i = 0; i < meals.Count; i++)
            {
                RecipeData r = c.Recipe(meals[i].recipeId);
                if (r == null || r.buffs == null) continue;
                float own = ClassMult(cls, r);
                for (int b = 0; b < r.buffs.Count; b++)
                    sum[(int)r.buffs[b].stat] += r.buffs[b].value * meals[i].potency * own * synMult;
            }
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].sameRecipeBonus <= 0f) continue;
                int n; RecipeData rep = MostRepeated(recipes, out n);
                if (rep == null || rep.buffs == null) continue;
                float repMult = ClassMult(cls, rep);
                for (int b = 0; b < rep.buffs.Count; b++)
                    sum[(int)rep.buffs[b].stat] += rep.buffs[b].value * active[i].sameRecipeBonus * repMult * synMult;
            }
            return sum;
        }

        public static List<SynergyData> ActiveSynergies(GameContent c, RunState run)
        {
            return ActiveSynergies(c, run != null ? run.eaten : null);
        }

        public static List<SynergyData> ActiveSynergies(GameContent c, List<EatenMeal> eaten)
        {
            var result = new List<SynergyData>();
            if (eaten == null || eaten.Count == 0) return result;
            var recipes = new List<RecipeData>();
            for (int i = 0; i < eaten.Count; i++)
            {
                RecipeData r = c.Recipe(eaten[i].recipeId);
                if (r != null) recipes.Add(r);
            }
            for (int s = 0; s < c.synergies.Count; s++)
            {
                SynergyData syn = c.synergies[s];
                bool ok;
                if (syn.requireAllStations)
                {
                    ok = true;
                    foreach (Station st in Enum.GetValues(typeof(Station)))
                        if (!recipes.Exists(x => x.station == st)) { ok = false; break; }
                }
                else if (syn.requireSameRecipe)
                {
                    int same;
                    MostRepeated(recipes, out same);
                    ok = same >= Math.Max(2, syn.requiredCount);
                }
                else if (syn.useStation)
                {
                    int n = 0;
                    for (int i = 0; i < recipes.Count; i++) if (recipes[i].station == syn.requiredStation) n++;
                    ok = n >= Math.Max(1, syn.requiredCount);
                }
                else if (!string.IsNullOrEmpty(syn.requiredTag))
                {
                    int n = 0;
                    for (int i = 0; i < recipes.Count; i++)
                        if (recipes[i].tags != null && recipes[i].tags.Contains(syn.requiredTag)) n++;
                    ok = n >= Math.Max(1, syn.requiredCount);
                }
                else ok = false;
                if (ok) result.Add(syn);
            }
            return result;
        }
    }
}
