// Hearth & Hollow — gather minigame: a ping-pong marker over a bar with a safe (amber) zone and a
// golden "perfect" zone. Click / Space / "Pick!" stops it → session.ResolveGather(marker).
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class GatherOverlay : UIScreen
    {
        readonly Label _title, _hint, _result;
        readonly VisualElement _track, _safe, _perfect, _marker, _ingWell;
        readonly Button _pick;
        IVisualElementScheduledItem _tick;
        float _startTime;
        float _marker01;
        bool _stopped;
        GatherState _g;

        public GatherOverlay(GameUI ui) : base(ui, "overlay", "gather")
        {
            var box = UIKit.E("overlay__box", "gather__box");
            Root.Add(box);
            _title = UIKit.L("", "overlay__title");
            _hint = UIKit.L("Stop the marker in the amber zone — gold for a double haul!", "muted");
            _ingWell = UIKit.E("gather__well");
            box.Add(_ingWell);
            box.Add(_title);
            box.Add(_hint);

            _track = UIKit.E("gather-bar");
            var fill = UIKit.E("gather-bar__fill");   // slider-art track fill under the zones
            _track.Add(fill);
            _safe = UIKit.E("gather-bar__safe");
            _perfect = UIKit.E("gather-bar__perfect");
            _marker = UIKit.E("gather-bar__marker");
            _track.Add(_safe);
            _track.Add(_perfect);
            _track.Add(_marker);
            foreach (var c in _track.Children()) c.pickingMode = PickingMode.Ignore;
            _track.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) Stop(); });
            box.Add(_track);

            _pick = Art.IconButton("gather", "🧺", "Pick!  (Space)", Stop, "btn--primary", "btn--huge");
            box.Add(_pick);

            _result = UIKit.L("", "gather__result");
            box.Add(_result);
        }

        protected override void OnShow()
        {
            _g = S.Run != null ? S.Run.gather : null;
            _stopped = false;
            _startTime = UIKit.Now;
            _result.text = "";
            _result.RemoveFromClassList("gather__result--perfect");
            _result.RemoveFromClassList("gather__result--miss");
            _pick.SetEnabled(true);
            if (_g != null)
            {
                // zones: perfect centred inside safe
                _safe.style.left = Length.Percent((_g.safeCenter - _g.safeWidth / 2f) * 100f);
                _safe.style.width = Length.Percent(_g.safeWidth * 100f);
                _perfect.style.left = Length.Percent((_g.safeCenter - _g.perfectWidth / 2f) * 100f);
                _perfect.style.width = Length.Percent(_g.perfectWidth * 100f);
                var ing = C.Ingredient(_g.ingredientId);
                _title.text = "Gathering " + (ing != null ? ing.name : "something tasty");
                Art.Set(_ingWell, Art.Framed(Art.Pic(ArtLibrary.GetIngredient(_g.ingredientId), ing != null ? ing.emoji : "🧺", "gather__emoji"), "item-frame--lg"));
            }
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(16);
            else _tick.Resume();
        }

        protected override void OnHide()
        {
            if (_tick != null) _tick.Pause();
        }

        void Tick()
        {
            if (_g == null || _stopped) return;
            _marker01 = Explore.MarkerAt(_g, UIKit.Now - _startTime);
            _marker.style.left = Length.Percent(_marker01 * 100f);
        }

        void Stop()
        {
            if (_stopped || _g == null || S.Phase != Phase.Gather) return;
            _stopped = true;
            _pick.SetEnabled(false);
            // grade locally only to show the result briefly before handing over to the session
            GatherGrade grade = Explore.EvaluateGather(_g, _marker01);
            int yield = Explore.GatherYield(C.tuning, grade, S.Run.mod);
            var ing = C.Ingredient(_g.ingredientId);
            string name = ing != null ? ing.emoji + " " + ing.name : "";
            if (grade == GatherGrade.Perfect) _result.text = "✨ Perfect! +" + yield + " " + name;
            else if (grade == GatherGrade.Safe) _result.text = "👍 Nice! +" + yield + " " + name;
            else _result.text = "🍂 It slipped away…";
            _result.EnableInClassList("gather__result--perfect", grade == GatherGrade.Perfect);
            _result.EnableInClassList("gather__result--miss", grade == GatherGrade.Miss);
            Juice.Pop(_result, 1.3f, 0.05f, 0.2f);
            Juice.Pop(_marker, 1.6f, 0.05f, 0.2f);

            float m = _marker01;
            Root.schedule.Execute(() => { if (S.Phase == Phase.Gather) S.ResolveGather(m); }).StartingIn(700);
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Space || key == KeyCode.Return || key == KeyCode.KeypadEnter) { Stop(); return true; }
            return false;
        }
    }
}
