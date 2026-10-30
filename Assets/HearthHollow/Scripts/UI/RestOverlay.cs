// Hearth & Hollow — rest overlay: a cosy campfire break.
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class RestOverlay : UIScreen
    {
        readonly VisualElement _fire;
        readonly Label _healed;
        readonly Bar _hp;
        IVisualElementScheduledItem _flicker;

        public RestOverlay(GameUI ui) : base(ui, "overlay", "rest")
        {
            var box = UIKit.E("overlay__box", "rest__box");
            Root.Add(box);
            _fire = Art.Icon("tile_rest", "🔥", "rest__fire");
            box.Add(_fire);
            box.Add(UIKit.L("A little campfire in the fog", "overlay__title"));
            box.Add(UIKit.L("You warm your hands, eat a crust of bread and rest your feet.", "muted"));
            _healed = UIKit.L("", "rest__healed");
            box.Add(_healed);
            _hp = new Bar("bar--hp");
            box.Add(_hp);
            box.Add(UIKit.B("Continue  (Space)", () => S.LeaveRest(), "btn--primary", "btn--huge"));
        }

        protected override void OnShow()
        {
            int h = Ui.LastRestHeal;
            _healed.text = h > 0 ? "💚 Healed " + h + " HP" : "You're already feeling hale.";
            Juice.Pop(_healed, 1.25f, 0.05f, 0.3f);
            float start = UIKit.Now;
            if (_flicker == null)
                _flicker = _fire.schedule.Execute(() =>
                {
                    float t = UIKit.Now - start;
                    float s = 1f + 0.06f * Mathf.Sin(t * 7f) + 0.03f * Mathf.Sin(t * 13f);
                    _fire.style.scale = new Scale(new Vector2(s, s));
                }).Every(33);
            else _flicker.Resume();
        }

        protected override void OnHide() { if (_flicker != null) _flicker.Pause(); }

        public override void Refresh()
        {
            var run = S.Run;
            if (run == null) return;
            int max = S.Stats != null ? S.Stats.maxHp : run.hp;
            _hp.Set(run.hp, max, run.hp + " / " + max);
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Space || key == KeyCode.Return || key == KeyCode.KeypadEnter) { S.LeaveRest(); return true; }
            return false;
        }
    }
}
