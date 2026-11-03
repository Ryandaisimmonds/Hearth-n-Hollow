// Hearth & Hollow — the stove (cooking v10, Design v2). Opened by clicking a recipe on the Cooking tab.
// Shows what the dish needs, lets you put one ingredient in the element slot (infuse or guard), shows the odds of
// each quality, then cooks: the ingredients are spent, the quality is rolled and the meal goes into the meal pantry.
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
            box.Add(UIKit.L("Ingredients", "subhead", "cook-pop__sub"));
            _lines = UIKit.E("cook-pop__lines");
            box.Add(_lines);
            box.Add(UIKit.L("Element slot (optional) — infuse your attacks, or guard against an element", "subhead", "cook-pop__sub"));
            _accel = UIKit.E("cook-pop__accel");
            box.Add(_accel);
            box.Add(UIKit.L("Quality", "subhead", "cook-pop__sub"));
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

        /// Open on a recipe with an empty element slot.
        public void SetRecipe(string recipeId)
        {
            _recipeId = recipeId;
            _lastResult = null;
            _choice = new CookChoice();
        }

        void DoCook()
        {
            var r = C.Recipe(_recipeId);
            var meal = S.Cook(_recipeId, _choice);
            if (meal == null) return;
            _lastResult = "Cooked: " + Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + (r != null ? r.name : "") + "!";
            // keep the element ingredient if there's another one
            string acc = _choice.accelId;
            _choice = new CookChoice();
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
            ht.Add(UIKit.L(r.name + (r.tier >= 2 ? "  ★ Tier 2" : "") + (Meals.IsFavourite(cls, r) ? " ⭐" : ""), "overlay__title", "cook-pop__title"));
            ht.Add(UIKit.L(r.station + " · " + (r.description ?? ""), "small", "muted"));
            _head.Add(ht);

            // ingredients: what it needs and what you have
            if (r.ingredients.Count == 0) _lines.Add(UIKit.L("No ingredients needed.", "small", "muted"));
            foreach (var ic in r.ingredients)
            {
                var ing = C.Ingredient(ic.ingredientId);
                int have = Economy.PantryCount(p, ic.ingredientId);
                var row = UIKit.E("cook-line");
                row.Add(UIKit.L(UIKit.IngEmoji(C, ic.ingredientId) + " " + ic.count + "× " + UIKit.IngName(C, ic.ingredientId)
                    + (ing != null && ing.deep ? "  ✨ found deep" : ""), "cook-line__name"));
                var haveL = UIKit.L("have " + have, "small", have >= ic.count ? "muted" : "cook-line__short");
                row.Add(haveL);
                _lines.Add(row);
            }

            // element slot: none, or any element ingredient you have at least one of
            var accRow = UIKit.E("cook-line", "cook-line--wrap");
            var none = UIKit.B("None", () => { _choice.accelId = null; Refresh(); }, "filter-chip");
            none.EnableInClassList("filter-chip--on", string.IsNullOrEmpty(_choice.accelId));
            accRow.Add(none);
            int shown = 0;
            foreach (var a in Cooking.Accelerators(C))
            {
                int have = Economy.PantryCount(p, a.id);
                if (have <= 0) continue;
                shown++;
                var acc = a;
                var b = UIKit.B(a.emoji + " " + a.name + " · " + Cooking.SlotText(a) + "  (" + have + ")", () => { _choice.accelId = acc.id; Refresh(); }, "filter-chip");
                b.EnableInClassList("filter-chip--on", _choice.accelId == a.id);
                b.tooltip = a.infuse != Element.None ? "Infuse: your attacks that have no element of their own become " + Elements.Name(a.infuse) + " for the run. One infused meal per tray."
                          : a.guard != Element.None ? "Guard: " + Elements.Name(a.guard) + " attacks hit you for half for the run."
                          : Cooking.SlotText(a);
                accRow.Add(b);
            }
            if (shown == 0) accRow.Add(UIKit.L("No element ingredients in the larder yet (chili, fire honey, frostberry, nettle, salt, fish, glowcap, mint, ginger, garlic, herbs, wheat).", "small", "muted"));
            _accel.Add(accRow);

            // quality odds
            float[] odds = Cooking.Odds(C, p, r, _choice);   // includes the station level's bonus
            for (int g = 0; g < Cooking.Grades; g++)
            {
                var cell = UIKit.E("cook-odds", "cook-odds--" + g);
                cell.style.flexGrow = Mathf.Max(0.0001f, odds[g]);
                cell.Add(UIKit.L(Cooking.GradeGem(g) + " " + Cooking.GradeName(g) + " " + Mathf.RoundToInt(odds[g] * 100f) + "%", "cook-odds__text"));
                UIKit.Show(cell, odds[g] > 0.0005f);
                _odds.Add(cell);
            }

            // what each quality would give this class
            for (int g = 0; g < Cooking.Grades; g++)
            {
                if (odds[g] <= 0.0005f) continue;
                var em = new EatenMeal { recipeId = r.id, potency = 1f, grade = g, accelId = _choice.accelId };
                _preview.Add(UIKit.L(Cooking.GradeGem(g) + " " + Cooking.GradeName(g) + " (×" + UIKit.Num(Cooking.GradeMult(C.tuning, g)) + "): "
                    + UIKit.BuffSummary(Cooking.MealBuffs(C, cls, em)), "small", "cook-pop__line"));
            }
            var slotIng = string.IsNullOrEmpty(_choice.accelId) ? null : C.Ingredient(_choice.accelId);
            if (slotIng != null) _preview.Add(UIKit.L(Cooking.SlotText(slotIng) + " for the whole run", "small", "cook-pop__line"));
            var t2 = C.recipes.Find(x => x.upgradeOf == r.id);
            if (t2 != null) _preview.Add(UIKit.L("★ Tier 2: " + t2.name + " needs " + DeepNames(t2) + " from deep in the dungeons — much stronger.", "small", "muted"));
            _preview.Add(UIKit.L("🔨 " + r.station + " level " + Kitchen.Level(C, p, r.station) + " raises the chance of a better meal", "small", "muted"));

            string why;
            bool can = S.CanCook(_recipeId, _choice, out why);
            _cookBtn.SetEnabled(can);
            _why.text = can ? "Uses the ingredients now · meal pantry " + p.mealPantry.Count + "/" + Kitchen.PantrySlots(C, p) : why;
            _result.text = _lastResult ?? "";
            UIKit.Show(_result, !string.IsNullOrEmpty(_lastResult));
        }

        string DeepNames(RecipeData r)
        {
            var names = new List<string>();
            foreach (var ic in r.ingredients) { var i = C.Ingredient(ic.ingredientId); if (i != null && i.deep) names.Add(i.emoji + " " + i.name); }
            return string.Join(", ", names.ToArray());
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseCook();
            else if ((key == KeyCode.Return || key == KeyCode.KeypadEnter) && _cookBtn.enabledSelf) DoCook();
            return true;   // modal
        }
    }
}
