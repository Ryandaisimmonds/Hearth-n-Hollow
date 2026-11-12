// Hearth & Hollow — the stove. Opened by clicking a recipe on the Cooking tab.
// Ticket 45: "you put in this, you get this, done". Four things on screen: the dish, its ingredients (have / need),
// what it gives, and Cook. Cook starts the heat bar, which bounces back and forth at a steady pace until you stop
// it: gold = Prime, warm = Fine, anywhere else = Plain (kitchen upgrades widen the zones). Then a result card that
// goes by itself after a couple of seconds (or on a click), back to the cooking area's recipe list.
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class CookOverlay : UIScreen
    {
        enum Mode { Ready, Cooking, Done }

        readonly VisualElement _main, _head, _lines, _card, _cardPic;
        readonly Label _gives, _why, _cardTitle, _cardBuffs;
        readonly Button _cookBtn;
        IVisualElementScheduledItem _cardTimer;
        readonly VisualElement _game, _gameTrack, _gameGood, _gamePerfect, _gameMarker;
        readonly Label _gameHint;
        Mode _mode;
        float _gameStart, _gameMarker01;
        IVisualElementScheduledItem _tick;
        string _recipeId;

        public CookOverlay(GameUI ui) : base(ui, "overlay", "cook-pop")
        {
            var box = UIKit.E("overlay__box", "cook-pop__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root && _mode != Mode.Cooking) Close(); });
            var close = UIKit.B("", Close, "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);

            // ── the recipe ──
            _main = UIKit.E("cook-pop__main");
            box.Add(_main);
            _head = UIKit.E("cook-pop__head");
            _main.Add(_head);
            _lines = UIKit.E("cook-pop__lines");
            _main.Add(_lines);
            _gives = UIKit.L("", "cook-pop__gives");
            _main.Add(_gives);

            // the heat bar: shown once you press Cook
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
            _gameTrack.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0 && _mode == Mode.Cooking) StopGame(); });
            _game.Add(_gameTrack);
            _main.Add(_game);

            var foot = UIKit.E("cook-pop__foot");
            _why = UIKit.L("", "small", "cook-pop__why");
            foot.Add(_why);
            _cookBtn = UIKit.B("🔥 Cook", CookPressed, "btn--primary", "btn--huge", "cook-pop__cook");
            _cookBtn.tooltip = "Enter / Space";
            foot.Add(_cookBtn);
            _main.Add(foot);

            // ── the result card ──
            _card = UIKit.E("cook-card");
            _cardPic = UIKit.E("cook-card__pic");
            _card.Add(_cardPic);
            _cardTitle = UIKit.L("", "cook-card__title");
            _card.Add(_cardTitle);
            _cardBuffs = UIKit.L("", "cook-card__buffs");
            _card.Add(_cardBuffs);
            _card.RegisterCallback<PointerDownEvent>(e => { if (_mode == Mode.Done) Close(); });   // click to dismiss
            box.Add(_card);
        }

        public void SetRecipe(string recipeId)
        {
            _recipeId = recipeId;
            SetMode(Mode.Ready);
        }

        protected override void OnHide() { StopTick(); _mode = Mode.Ready; }

        void Close()
        {
            StopTick();
            if (_cardTimer != null) _cardTimer.Pause();
            _mode = Mode.Ready;
            Ui.CloseCook();   // one meal per visit: back to the cooking area (the recipe list)
        }

        void SetMode(Mode m)
        {
            _mode = m;
            UIKit.Show(_main, m != Mode.Done);
            UIKit.Show(_card, m == Mode.Done);
            UIKit.Show(_game, m == Mode.Cooking);
            _cookBtn.text = m == Mode.Cooking ? "✋ Now!" : "🔥 Cook";
            if (m != Mode.Cooking) StopTick();
            Refresh();
        }

        void StopTick() { if (_tick != null) _tick.Pause(); }

        // ───── the heat bar ─────
        void CookPressed()
        {
            if (_mode == Mode.Cooking) { StopGame(); return; }
            if (_mode != Mode.Ready) return;
            string why;
            if (!S.CanCook(_recipeId, new CookChoice(), out why)) return;
            var r = C.Recipe(_recipeId);
            float perfect, good;
            Cooking.CookZones(C, S.Profile, r.station, out perfect, out good);
            float target = C.tuning.cookGameTarget;
            _gamePerfect.style.left = Length.Percent((target - perfect / 2f) * 100f);
            _gamePerfect.style.width = Length.Percent(perfect * 100f);
            _gameGood.style.left = Length.Percent((target - good / 2f) * 100f);
            _gameGood.style.width = Length.Percent(good * 100f);
            _gameHint.text = "Stop the heat in the gold for Prime, the warm zone for Fine";
            _gameStart = UIKit.Now;
            _gameMarker01 = 0f;
            _gameMarker.style.left = Length.Percent(0f);
            SetMode(Mode.Cooking);
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(16); else _tick.Resume();
        }

        void Tick()
        {
            if (_mode != Mode.Cooking) return;
            _gameMarker01 = Cooking.CookMarkerAt(C.tuning, UIKit.Now - _gameStart);
            _gameMarker.style.left = Length.Percent(_gameMarker01 * 100f);
        }

        void StopGame()
        {
            if (_mode != Mode.Cooking) return;
            if (UIKit.Now - _gameStart < 0.12f) return;   // the same press that started it (focused button + key)
            StopTick();
            var r = C.Recipe(_recipeId);
            int grade = Cooking.CookGrade(C, S.Profile, r.station, _gameMarker01);
            var meal = S.Cook(_recipeId, new CookChoice { grade = grade });
            if (meal == null) { SetMode(Mode.Ready); return; }
            // the result card
            _cardPic.Clear();
            _cardPic.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(r.id), r.emoji, "cook-card__emoji"), "cook-pop__well"));
            _cardTitle.text = Cooking.GradeGem(meal.grade) + " " + Cooking.GradeName(meal.grade) + " " + r.name + (meal.grade == 2 ? "!" : "");
            _cardBuffs.text = GivesText(r, meal.grade);
            _card.EnableInClassList("cook-card--prime", meal.grade == 2);
            SetMode(Mode.Done);
            Juice.Pop(_cardTitle, 1.25f, 0.05f, 0.25f);
            // the card goes by itself after a couple of seconds (or on a click / Enter)
            if (_cardTimer == null) _cardTimer = Root.schedule.Execute(() => { if (_mode == Mode.Done) Close(); }).StartingIn(2200);
            else { _cardTimer.ExecuteLater(2200); _cardTimer.Resume(); }
        }

        string GivesText(RecipeData r, int grade)
        {
            var cls = C.Class(S.Profile.selectedClass);
            var em = new EatenMeal { recipeId = r.id, potency = 1f, grade = grade };
            string kind = Tray.StationEmoji(r) + " ";   // ticket 52: the station tells you the special's family
            return kind + UIKit.MealSummary(Cooking.MealBuffs(C, cls, em));
        }

        public override void Refresh()
        {
            var r = C.Recipe(_recipeId);
            var p = S.Profile;
            _head.Clear(); _lines.Clear();
            if (r == null) return;

            // 1 · the dish
            _head.Add(Art.Framed(Art.Pic(ArtLibrary.GetRecipe(r.id), r.emoji, "cook-pop__emoji"), "cook-pop__well"));
            _head.Add(UIKit.L(r.name, "overlay__title", "cook-pop__title"));

            // 2 · you put in this
            foreach (var ic in r.ingredients)
            {
                int have = Economy.PantryCount(p, ic.ingredientId);
                bool known = Routes.IngredientKnown(C, p, ic.ingredientId);   // ticket 24
                var row = UIKit.E("cook-line");
                row.Add(UIKit.L(known ? UIKit.IngEmoji(C, ic.ingredientId) + " " + UIKit.IngName(C, ic.ingredientId) : "❓ not found yet", "cook-line__name"));
                row.Add(UIKit.L(ic.count + " needed · have " + have, have >= ic.count ? "cook-line__have" : "cook-line__short"));
                _lines.Add(row);
            }

            // 3 · you get this
            _gives.text = "You get: " + GivesText(r, 0);

            // 4 · Cook
            string why;
            bool can = S.CanCook(_recipeId, new CookChoice(), out why);
            _cookBtn.SetEnabled(can || _mode == Mode.Cooking);
            _why.text = can ? "" : why;
            UIKit.Show(_why, !can);
        }

        public override bool HandleKey(KeyCode key)
        {
            bool go = key == KeyCode.Return || key == KeyCode.KeypadEnter || key == KeyCode.Space;
            if (key == KeyCode.Escape) { if (_mode == Mode.Cooking) SetMode(Mode.Ready); else Close(); }
            else if (go && _mode == Mode.Done) Close();
            else if (go && _cookBtn.enabledSelf) CookPressed();
            return true;   // modal
        }
    }
}
