// Hearth & Hollow — the spice rack (ticket 27). Element ingredients go into a meal after it's cooked, here, one
// decision at a time: 1) pick a cooked meal, 2) pick an element ingredient, and the effect is spelled out large.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class SeasonOverlay : UIScreen
    {
        readonly VisualElement _meals, _spices, _effect;
        readonly Label _why;
        readonly Button _go;
        CookedMeal _meal;
        string _spice;

        public SeasonOverlay(GameUI ui) : base(ui, "overlay", "season-pop")
        {
            var box = UIKit.E("overlay__box", "season-pop__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.CloseSeason(); });
            var close = UIKit.B("", () => Ui.CloseSeason(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);
            box.Add(UIKit.L("🧂 Spice rack", "overlay__title"));
            box.Add(UIKit.L("Season a cooked meal with one element ingredient. It's used up, and the meal carries it for the whole run.", "small", "muted", "season-pop__intro"));

            box.Add(UIKit.L("1 · Pick a meal", "subhead", "season-pop__step"));
            _meals = UIKit.E("season-pop__meals");
            box.Add(_meals);
            box.Add(UIKit.L("2 · Pick an element ingredient", "subhead", "season-pop__step"));
            _spices = UIKit.E("season-pop__spices");
            box.Add(_spices);
            _effect = UIKit.E("season-pop__effect");
            box.Add(_effect);

            var foot = UIKit.E("cook-pop__foot");
            _why = UIKit.L("", "small", "cook-pop__why");
            foot.Add(_why);
            _go = UIKit.B("🧂 Season", DoSeason, "btn--primary", "cook-pop__cook");
            foot.Add(_go);
            box.Add(foot);
        }

        protected override void OnShow() { _meal = null; _spice = null; }

        void DoSeason()
        {
            string why;
            if (S.SeasonMeal(_meal, _spice, out why)) { _meal = null; _spice = null; }
            else _why.text = why;
            Refresh();
        }

        public override void Refresh()
        {
            var p = S.Profile;
            _meals.Clear(); _spices.Clear(); _effect.Clear();

            var meals = S.SeasonableMeals();
            if (_meal != null && !meals.Contains(_meal)) _meal = null;
            if (meals.Count == 0) _meals.Add(UIKit.L("No unseasoned meals. Cook something at the stove first.", "muted"));
            foreach (var m in meals)
            {
                var r = C.Recipe(m.recipeId);
                var card = UIKit.E("meal-card", "clickable", "season-pop__meal");
                card.EnableInClassList("season-pop__meal--on", m == _meal);
                card.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(m.recipeId), r != null ? r.emoji : "🍽️", "meal-card__emoji"), "meal-card__well"));
                var txt = UIKit.E("meal-card__text");
                txt.Add(UIKit.L(Cooking.GradeGem(m.grade) + " " + (r != null ? r.name : m.recipeId), "meal-card__name"));
                txt.Add(UIKit.L(p.packed.Contains(m) ? "on the tray" : "in the meal pantry", "meal-card__accel"));
                card.Add(txt);
                var mm = m;
                card.RegisterCallback<ClickEvent>(e => { _meal = mm; _why.text = ""; Refresh(); });
                foreach (var ch in card.Children()) ch.pickingMode = PickingMode.Ignore;
                _meals.Add(card);
            }

            int shown = 0;
            foreach (var a in Cooking.Accelerators(C))
            {
                if (a.infuse == Element.None && a.guard == Element.None) continue;
                int have = Economy.PantryCount(p, a.id);
                if (have <= 0) continue;
                shown++;
                var acc = a;
                var chip = UIKit.B(a.emoji + " " + a.name + "  (" + have + ")", () => { _spice = acc.id; _why.text = ""; Refresh(); }, "filter-chip", "season-pop__spice");
                chip.EnableInClassList("filter-chip--on", _spice == a.id);
                UIKit.ElementTint(chip, a.infuse != Element.None ? a.infuse : a.guard);
                _spices.Add(chip);
            }
            if (shown == 0) _spices.Add(UIKit.L("No element ingredients yet. Chilies (Fire) grow on Chili Ridge; ginger (guards Frost) is found deep in Meadow Hollow.", "small", "muted"));
            if (_spice != null && Economy.PantryCount(p, _spice) <= 0) _spice = null;

            // the effect, spelled out
            var ing = _spice != null ? C.Ingredient(_spice) : null;
            if (ing != null)
            {
                Element e = ing.infuse != Element.None ? ing.infuse : ing.guard;
                UIKit.ElementTint(_effect, e);
                _effect.Add(UIKit.L(Cooking.SlotText(ing), "season-pop__effect-head"));
                if (ing.infuse != Element.None)
                {
                    _effect.Add(UIKit.L("For the whole run, your attacks that have no element of their own hit as " + Elements.Name(e) + ".", "season-pop__effect-line"));
                    var beats = Elements.StrongAgainst(e);
                    if (beats != Element.None) _effect.Add(UIKit.L("Strong against " + Elements.Emoji(beats) + " " + Elements.Name(beats) + " monsters (×1.5 damage). Only one infused meal per tray.", "season-pop__effect-line"));
                }
                else _effect.Add(UIKit.L(Elements.Name(e) + " attacks hit you for half, for the whole run.", "season-pop__effect-line"));
            }
            else UIKit.Show(_effect, false);
            if (ing != null) UIKit.Show(_effect, true);

            string why;
            bool can = _meal != null && ing != null && CanSeason(out why);
            if (_meal == null) why = "Pick a meal";
            else if (ing == null) why = "Pick an element ingredient";
            else if (!can) CanSeason(out why);
            else why = "Uses 1 " + ing.emoji + " " + ing.name;
            if (string.IsNullOrEmpty(_why.text) || can) _why.text = why;
            _go.SetEnabled(can);
        }

        bool CanSeason(out string why)
        {
            why = null;
            var ing = C.Ingredient(_spice);
            if (ing != null && ing.infuse != Element.None && S.Profile.packed.Contains(_meal))
                foreach (var q in S.Profile.packed) if (q != _meal && Cooking.InfusionOf(C, q.accelId) != Element.None) { why = "Only one infused meal per tray"; return false; }
            return true;
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseSeason();
            else if ((key == KeyCode.Return || key == KeyCode.KeypadEnter) && _go.enabledSelf) DoSeason();
            return true;
        }
    }
}
