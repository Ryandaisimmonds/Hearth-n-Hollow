// Hearth & Hollow — the fishing minigame at the pond in Hearth's Hollow (one quick catch a day).
// Cast (E / Space / (A)), wait for the bite — don't reel in too soon! — then stop the sweeping marker:
// the gold centre = Prime fish, the amber zone = Fine, anywhere else = Plain. You always catch something.
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class FishingOverlay : UIScreen
    {
        enum State { Ready, Waiting, Bite, Done }

        readonly Label _title, _hint, _bobber, _result;
        readonly VisualElement _track, _good, _perfect, _marker;
        readonly Button _btn;
        IVisualElementScheduledItem _tick;
        State _state;
        float _t0, _biteAt, _openedAt, _marker01, _centre;

        public FishingOverlay(GameUI ui) : base(ui, "overlay", "fishing")
        {
            var box = UIKit.E("overlay__box", "gather__box", "fishing__box");
            Root.Add(box);
            Root.RegisterCallback<PointerDownEvent>(e => { if (e.target == Root) Ui.CloseFishing(); });
            var close = UIKit.B("", () => Ui.CloseFishing(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);

            _title = UIKit.L("🎣 The fishing jetty", "overlay__title");
            box.Add(_title);
            _hint = UIKit.L("", "muted", "fishing__hint");
            box.Add(_hint);
            _bobber = UIKit.L("", "fishing__bobber");
            box.Add(_bobber);

            _track = UIKit.E("gather-bar");
            _track.Add(UIKit.E("gather-bar__fill"));
            _good = UIKit.E("gather-bar__safe");
            _perfect = UIKit.E("gather-bar__perfect");
            _marker = UIKit.E("gather-bar__marker");
            _track.Add(_good); _track.Add(_perfect); _track.Add(_marker);
            foreach (var c in _track.Children()) c.pickingMode = PickingMode.Ignore;
            _track.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Press(); });
            box.Add(_track);

            _btn = UIKit.B("", Press, "btn--primary", "btn--huge");
            box.Add(_btn);
            _result = UIKit.L("", "gather__result");
            box.Add(_result);
            box.Add(UIKit.L("Gold centre = 🔵 Prime · amber = 🟢 Fine · anywhere else = ⚪ Plain", "small", "muted"));
        }

        protected override void OnShow()
        {
            _openedAt = UIKit.Now;
            SetState(S.CanFish ? State.Ready : State.Done);
            if (!S.CanFish) { _result.text = "You've had today's catch. The fish will be biting again after your next expedition."; }
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(0); else _tick.Resume();
        }

        protected override void OnHide() { if (_tick != null) _tick.Pause(); }

        void SetState(State s)
        {
            _state = s;
            _t0 = UIKit.Now;
            UIKit.Show(_track, s == State.Bite);
            switch (s)
            {
                case State.Ready:
                    _hint.text = "Cast your line, then wait for a bite.";
                    _bobber.text = "🌊";
                    _btn.text = "🎣 Cast  (E / Space)";
                    _result.text = "";
                    _btn.SetEnabled(true);
                    break;
                case State.Waiting:
                    _hint.text = "Wait for it… don't reel in too soon!";
                    _bobber.text = "🌊 · · ·";
                    _btn.text = "Reel in";
                    _biteAt = UIKit.Now + Random.Range(1.2f, 3.2f);
                    break;
                case State.Bite:
                    _hint.text = "A bite! Stop the marker in the gold!";
                    _bobber.text = "❗🐟";
                    _btn.text = "Reel in!  (E / Space)";
                    _centre = Random.Range(0.25f, 0.75f);
                    var t = C.tuning;
                    _good.style.left = Length.Percent((_centre - t.fishGoodWidth / 2f) * 100f);
                    _good.style.width = Length.Percent(t.fishGoodWidth * 100f);
                    _perfect.style.left = Length.Percent((_centre - t.fishPerfectWidth / 2f) * 100f);
                    _perfect.style.width = Length.Percent(t.fishPerfectWidth * 100f);
                    break;
                case State.Done:
                    _hint.text = "";
                    _bobber.text = "🌅";
                    _btn.text = "Back to town";
                    _btn.SetEnabled(true);
                    break;
            }
        }

        void Tick()
        {
            if (!Visible) return;
            if (_state == State.Waiting && UIKit.Now >= _biteAt) SetState(State.Bite);
            if (_state == State.Bite)
            {
                _marker01 = Fishing.MarkerAt(C.tuning, UIKit.Now - _t0);
                _marker.style.left = Length.Percent(_marker01 * 100f);
            }
            // gamepad / keyboard (ignore the press that opened this)
            if (UIKit.Now - _openedAt > 0.25f && (DungeonInput.Interact || DungeonInput.Dash || DungeonInput.Attack)) Press();
        }

        void Press()
        {
            if (UIKit.Now - _openedAt < 0.25f) return;
            switch (_state)
            {
                case State.Ready: SetState(State.Waiting); break;
                case State.Waiting:
                    SetState(State.Ready);
                    _result.text = "Too soon — the fish swam off. Cast again.";
                    break;
                case State.Bite:
                {
                    int grade = Fishing.GradeFor(C.tuning, _marker01, _centre);
                    S.LandCatch(grade);
                    var ing = C.Ingredient(C.tuning.fishIngredient);
                    SetState(State.Done);
                    _result.text = (grade == 2 ? "Perfect! " : grade == 1 ? "Nice! " : "Got one! ") + (C.tuning.fishCatchCount + grade) + " " +
                                   (ing != null ? ing.name : "fish") + " — into the larder. Come back tomorrow.";
                    break;
                }
                case State.Done: Ui.CloseFishing(); break;
            }
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseFishing();
            return true;   // modal (E / Space are read by Tick so the gamepad works too)
        }
    }
}
