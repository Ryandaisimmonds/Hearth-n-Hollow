// Hearth & Hollow — the stove (cooking v10, Design v2). Opened by clicking a recipe on the Cooking tab.
// Shows what the dish needs, lets you put one ingredient in the element slot (infuse or guard), shows the odds of
// each quality, then cooks: the ingredients are spent, the quality is rolled and the meal goes into the meal pantry.
// Ticket 11: "Cook" plays a short timing tap first (a heat marker sweeps once; stop it in the gold for a better
// chance of Fine / Prime); "Quick cook" skips it at no cost. The tap never changes the dish or its tier.
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
        readonly Button _cookBtn, _quickBtn;
        readonly VisualElement _game, _gameTrack, _gameGood, _gamePerfect, _gameMarker;
        readonly Label _gameHint;
        bool _gameOn;
        float _gameStart, _gameMarker01;
        IVisualElementScheduledItem _tick;
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

            // the timing tap: hidden until you press Cook
            _game = UIKit.E("cook-game");
            _gameHint = UIKit.L("", "small", "cook-game__hint");
            _game.Add(_gameHint);
            _gameTrack = UIKit.E("gather-bar", "cook-game__bar");
            _gameTrack.Add(UIKit.E("gather-bar__fill", "cook-game__fill"));
            _gameGood = UIKit.E("gather-bar__safe");
            _gamePerfect = UIKit.E("gather-bar__perfect");
            _gameMarker = UIKit.E("gather-bar__marker");
            _gameTrack.Add(_gameGood); _gameTrack.Add(_gamePerfect); _gameTrack.Add(_gameMarker);
            foreach (var ch in _gameTrack.Children()) ch.pickingMode = PickingMode.Ignore;
            _gameTrack.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && _gameOn) StopGame(); });
            _game.Add(_gameTrack);
            box.Add(_game);
            UIKit.Show(_game, false);

            var foot = UIKit.E("cook-pop__foot");
            _why = UIKit.L("", "small", "cook-pop__why");
            foot.Add(_why);
            _quickBtn = UIKit.B("Quick cook", () => DoCook(0f, null), "btn--secondary", "cook-pop__quick");
            _quickBtn.tooltip = "Skip the timing tap: cook straight away at the normal odds (Q / gamepad X)";
            foot.Add(_quickBtn);
            _cookBtn = UIKit.B("🔥 Cook", CookPressed, "btn--primary", "cook-pop__cook");
            _cookBtn.tooltip = "Tap again when the heat hits the gold for a better chance of Fine or Prime (Enter / Space / gamepad A)";
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
            EndGame();
        }

        protected override void OnHide() { EndGame(); }

        // ───── the timing tap ─────
        void CookPressed()
        {
            if (_gameOn) { StopGame(); return; }
            string why;
            if (!S.CanCook(_recipeId, _choice, out why)) return;
            var t = C.tuning;
            _gameOn = true;
            _gameStart = UIKit.Now;
            _gameMarker01 = 0f;
            _gamePerfect.style.left = Length.Percent((t.cookGameTarget - t.cookGamePerfect / 2f) * 100f);
            _gamePerfect.style.width = Length.Percent(t.cookGamePerfect * 100f);
            _gameGood.style.left = Length.Percent((t.cookGameTarget - t.cookGameGood / 2f) * 100f);
            _gameGood.style.width = Length.Percent(t.cookGameGood * 100f);
            _gameMarker.style.left = Length.Percent(0f);
            _gameHint.text = "Tap when the heat hits the gold!";
            UIKit.Show(_game, true);
            _cookBtn.text = "✋ Now!";
            _quickBtn.SetEnabled(false);
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(16); else _tick.Resume();
        }

        void Tick()
        {
            if (!_gameOn) return;
            _gameMarker01 = Cooking.CookMarkerAt(C.tuning, UIKit.Now - _gameStart);
            _gameMarker.style.left = Length.Percent(_gameMarker01 * 100f);
            if (_gameMarker01 >= 1f) StopGame();   // ran off the end: cooks at the normal odds
        }

        void StopGame()
        {
            if (!_gameOn) return;
            if (_gameMarker01 < 1f && UIKit.Now - _gameStart < 0.12f) return;   // the same press that started it (focused button + key)
            float m = _gameMarker01;
            var timing = Cooking.CookTiming(C.tuning, m);
            float nudge = Cooking.CookNudge(C.tuning, m);
            EndGame();
            UIKit.Show(_game, true);   // keep the bar up to show where it stopped
            _gameMarker.style.left = Length.Percent(m * 100f);
            Juice.Pop(_gameMarker, 1.6f, 0.05f, 0.2f);
            _gameHint.text = timing == Cooking.Timing.Perfect ? "✨ Perfect timing! Best chance of a better meal"
                           : timing == Cooking.Timing.Good ? "👍 Nicely done — a better chance of Fine / Prime"
                           : "Cooked at the normal odds";
            DoCook(nudge, timing == Cooking.Timing.Perfect ? "Perfect timing! " : null);
        }

        void EndGame()
        {
            _gameOn = false;
            if (_tick != null) _tick.Pause();
            _cookBtn.text = "🔥 Cook";
            if (_quickBtn != null) _quickBtn.SetEnabled(true);
            UIKit.Show(_game, false);
        }

        void DoCook(float nudge, string prefix)
        {
            var r = C.Recipe(_recipeId);
            _choice.qualityNudge = nudge;   // ticket 11: only the quality roll; never the dish or its tier
            var meal = S.Cook(_recipeId, _choice);
            if (meal == null) return;
            _lastResult = (prefix ?? "") + "Cooked: " + Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + (r != null ? r.name : "") + "!";
            // keep the element ingredient if there's another one
            string acc = _choice.accelId;
            _choice = new CookChoice();
            if (!string.IsNullOrEmpty(acc) && Economy.PantryCount(S.Profile, acc) > 0) _choice.accelId = acc;
            bool keepBar = _game.style.display != DisplayStyle.None;
            Refresh();
            UIKit.Show(_game, keepBar);
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
            ht.Add(UIKit.L(r.name + (r.tier >= 2 ? "  ★ Tier 2" : ""), "overlay__title", "cook-pop__title"));
            ht.Add(UIKit.L(r.station + " · " + (r.description ?? ""), "small", "muted"));
            _head.Add(ht);

            // ingredients: what it needs and what you have
            if (r.ingredients.Count == 0) _lines.Add(UIKit.L("No ingredients needed.", "small", "muted"));
            foreach (var ic in r.ingredients)
            {
                var ing = C.Ingredient(ic.ingredientId);
                int have = Economy.PantryCount(p, ic.ingredientId);
                var row = UIKit.E("cook-line");
                bool known = Routes.IngredientKnown(C, p, ic.ingredientId);   // ticket 24
                row.Add(UIKit.L(known ? UIKit.IngEmoji(C, ic.ingredientId) + " " + ic.count + "× " + UIKit.IngName(C, ic.ingredientId) + (ing != null && ing.deep ? "  ✨ found deep" : "")
                                      : "❓ " + ic.count + "× ? — not found yet" + (ing != null && ing.deep ? " (somewhere deep)" : ""), "cook-line__name"));
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
            var perfect = Cooking.Odds(C.tuning, Mathf.Max(0f, Kitchen.Jackpot(C, p, r.station) - C.tuning.jackpotChance), 1f);
            _preview.Add(UIKit.L("🔥 Perfect timing on the tap: Fine " + Mathf.RoundToInt(perfect[1] * 100f) + "% · Prime " + Mathf.RoundToInt(perfect[2] * 100f) + "%  (Quick cook skips it)", "small", "muted"));

            string why;
            bool can = S.CanCook(_recipeId, _choice, out why);
            _cookBtn.SetEnabled(can || _gameOn);
            _quickBtn.SetEnabled(can && !_gameOn);
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
            if (key == KeyCode.Escape) { if (_gameOn) EndGame(); else Ui.CloseCook(); }
            else if ((key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space) && _cookBtn.enabledSelf) CookPressed();
            else if (key == KeyCode.Q && !_gameOn && _quickBtn.enabledSelf) DoCook(0f, null);
            return true;   // modal
        }
    }
}
