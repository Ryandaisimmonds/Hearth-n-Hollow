// Hearth & Hollow — the stove (cooking v9). Opened by clicking a recipe on the Cooking tab.
// Pick a grade for each ingredient line and an optional accelerator, see the odds of each meal grade, then cook:
// the ingredients are spent, the grade is rolled and the meal goes into the meal pantry.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class CookOverlay : UIScreen
    {
        readonly VisualElement _head, _lines, _accel, _odds, _preview;
        readonly Label _result, _why;
        readonly Button _cookBtn;
        string _recipeId;
        CookChoice _choice;
        string _lastResult;

        public CookOverlay(GameUI ui) : base(ui, "overlay", "cook-pop")
        {
            var box = UIKit.E("overlay__box", "cook-pop__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.CloseCook(); });
            var close = UIKit.B("", () => Ui.CloseCook(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);

            _head = UIKit.E("cook-pop__head");
            box.Add(_head);
            box.Add(UIKit.L("Ingredients — better grades raise the odds of a better meal", "subhead", "cook-pop__sub"));
            _lines = UIKit.E("cook-pop__lines");
            box.Add(_lines);
            box.Add(UIKit.L("Accelerator (optional) — tailor the meal", "subhead", "cook-pop__sub"));
            _accel = UIKit.E("cook-pop__accel");
            box.Add(_accel);
            box.Add(UIKit.L("Odds", "subhead", "cook-pop__sub"));
            _odds = UIKit.E("cook-pop__odds");
            box.Add(_odds);
            _preview = UIKit.E("cook-pop__preview");
            box.Add(_preview);

            var foot = UIKit.E("cook-pop__foot");
            _why = UIKit.L("", "small", "cook-pop__why");
            foot.Add(_why);
            _cookBtn = UIKit.B("🔥 Cook", DoCook, "btn--primary", "cook-pop__cook");
            foot.Add(_cookBtn);
            box.Add(foot);
            _result = UIKit.L("", "cook-pop__result");
            box.Add(_result);
        }

        /// Open on a recipe, starting from the cheapest grades you have.
        public void SetRecipe(string recipeId)
        {
            _recipeId = recipeId;
            _lastResult = null;
            ResetChoice();
        }

        void ResetChoice()
        {
            var r = C.Recipe(_recipeId);
            _choice = Cooking.DefaultChoice(C, S.Profile, r);
            if (_choice == null)
            {
                _choice = new CookChoice();
                if (r != null) for (int i = 0; i < r.ingredients.Count; i++) _choice.lineGrades.Add(0);
            }
        }

        void DoCook()
        {
            var r = C.Recipe(_recipeId);
            var meal = S.Cook(_recipeId, _choice);
            if (meal == null) return;
            _lastResult = "Cooked: " + Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + (r != null ? r.name : "") + "!";
            // keep the accelerator if there's another one; reset the ingredient grades to what's left
            string acc = _choice.accelId;
            ResetChoice();
            if (!string.IsNullOrEmpty(acc) && Economy.PantryCount(S.Profile, acc) > 0) _choice.accelId = acc;
            Refresh();
        }

        public override void Refresh()
        {
            var r = C.Recipe(_recipeId);
            var p = S.Profile;
            _head.Clear(); _lines.Clear(); _accel.Clear(); _odds.Clear(); _preview.Clear();
            if (r == null) return;
            var cls = C.Class(p.selectedClass);

            // header
            _head.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(r.id), r.emoji, "cook-pop__emoji"), "cook-pop__well"));
            var ht = UIKit.E("cook-pop__head-text");
            ht.Add(UIKit.L(r.name + (Meals.IsFavourite(cls, r) ? " ⭐" : ""), "overlay__title", "cook-pop__title"));
            ht.Add(UIKit.L(r.station + " · " + (r.description ?? ""), "small", "muted"));
            _head.Add(ht);

            // ingredient lines: one grade each
            if (r.ingredients.Count == 0) _lines.Add(UIKit.L("No ingredients needed. Always Plain.", "small", "muted"));
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                var ic = r.ingredients[i];
                int line = i;
                var row = UIKit.E("cook-line");
                row.Add(UIKit.L(UIKit.IngEmoji(C, ic.ingredientId) + " " + ic.count + "× " + UIKit.IngName(C, ic.ingredientId), "cook-line__name"));
                for (int g = 0; g < Cooking.Grades; g++)
                {
                    int grade = g;
                    int have = Economy.PantryCount(p, ic.ingredientId, g);
                    var b = UIKit.B(Cooking.GradeGem(g) + " " + Cooking.IngGradeName(g) + "  (" + have + ")", () => { _choice.lineGrades[line] = grade; Refresh(); }, "filter-chip", "cook-grade", "cook-grade--" + g);
                    b.EnableInClassList("filter-chip--on", Cooking.LineGrade(_choice, i) == g);
                    b.EnableInClassList("cook-grade--short", have < ic.count);
                    b.tooltip = "You have " + have + " " + Cooking.IngGradeName(g) + " " + UIKit.IngName(C, ic.ingredientId) + " (need " + ic.count + ")";
                    row.Add(b);
                }
                _lines.Add(row);
            }

            // accelerator: none, or any accelerator you have at least one of
            var accRow = UIKit.E("cook-line", "cook-line--wrap");
            var none = UIKit.B("None", () => { _choice.accelId = null; Refresh(); }, "filter-chip");
            none.EnableInClassList("filter-chip--on", string.IsNullOrEmpty(_choice.accelId));
            accRow.Add(none);
            int shown = 0;
            foreach (var a in Cooking.Accelerators(C))
            {
                if (Economy.PantryCount(p, a.id) <= 0) continue;
                shown++;
                var acc = a;
                var b = UIKit.B(a.emoji + " " + a.name + " · " + AccelText(a) + "  (" + Economy.PantryCount(p, a.id) + ")", () => { _choice.accelId = acc.id; Refresh(); }, "filter-chip");
                b.EnableInClassList("filter-chip--on", _choice.accelId == a.id);
                b.tooltip = a.name + ": " + AccelText(a) + ". Uses one (your lowest grade first); the grade doesn't matter here.";
                accRow.Add(b);
            }
            if (shown == 0) accRow.Add(UIKit.L("No accelerators in the larder yet (garlic, salt, herbs, fire honey, frostberry, nettle, mint, ginger, glowcap).", "small", "muted"));
            _accel.Add(accRow);
            // odds
            float[] odds = Cooking.Odds(C, p, r, _choice);   // includes the station level's better-meal bonus
            for (int g = 0; g < Cooking.Grades; g++)
            {
                var cell = UIKit.E("cook-odds", "cook-odds--" + g);
                cell.style.flexGrow = Mathf.Max(0.0001f, odds[g]);
                cell.Add(UIKit.L(Cooking.GradeGem(g) + " " + Cooking.GradeName(g) + " " + Mathf.RoundToInt(odds[g] * 100f) + "%", "cook-odds__text"));
                UIKit.Show(cell, odds[g] > 0.0005f);
                _odds.Add(cell);
            }

            // what each grade would give this class
            for (int g = 0; g < Cooking.Grades; g++)
            {
                if (odds[g] <= 0.0005f) continue;
                var em = new EatenMeal { recipeId = r.id, potency = 1f, grade = g, accelId = _choice.accelId };
                _preview.Add(UIKit.L(Cooking.GradeGem(g) + " " + Cooking.GradeName(g) + ": " + UIKit.BuffSummary(Cooking.MealBuffs(C, cls, em)), "small", "cook-pop__line"));
            }
            if (!string.IsNullOrEmpty(_choice.accelId))
            {
                var a = C.Ingredient(_choice.accelId);
                if (a != null && a.accelGuard != Hazard.None)
                    _preview.Add(UIKit.L(Routes.HazardEmoji(a.accelGuard) + " Guards against " + Routes.HazardName(a.accelGuard).ToLower() + " (one halves it, two stop it)", "small", "cook-pop__line"));
            }
            _preview.Add(UIKit.L("🔨 " + r.station + " level " + Kitchen.Level(C, p, r.station) + ": " + UIKit.Pct(Kitchen.Jackpot(C, p, r.station)) + " chance the meal comes out one grade better", "small", "muted"));
            if (Meals.IsFavourite(cls, r)) _preview.Add(UIKit.L("⭐ Favourite station for the " + cls.name + " — included above (applies when packed)", "small", "muted"));

            string why;
            bool can = S.CanCook(_recipeId, _choice, out why);
            _cookBtn.SetEnabled(can);
            _why.text = can ? "Uses the ingredients now · meal pantry " + p.mealPantry.Count + "/" + C.tuning.mealPantrySlots : why;
            _result.text = _lastResult ?? "";
            UIKit.Show(_result, !string.IsNullOrEmpty(_lastResult));
        }

        string AccelText(IngredientData a)
        {
            if (a.accelGuard != Hazard.None) return Routes.HazardEmoji(a.accelGuard) + " " + Routes.HazardName(a.accelGuard).ToLower() + " guard";
            return UIKit.BuffSummary(a.accelBuffs);
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseCook();
            else if ((key == KeyCode.Return || key == KeyCode.KeypadEnter) && _cookBtn.enabledSelf) DoCook();
            return true;   // modal
        }
    }
}
