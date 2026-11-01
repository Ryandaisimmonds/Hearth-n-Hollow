// Hearth & Hollow — a talking pop-up for the people of Hearth's Hollow (Old Wren, your other classes…).
// Pages of text with a speaker; Next / E / Space moves on, Esc closes. The last page can offer an action button
// (e.g. "Swap to the Warden").
using System;
using System.Collections.Generic;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class DialogueOverlay : UIScreen
    {
        readonly Label _who, _face, _text, _page;
        readonly Button _next, _action;
        List<string> _pages = new List<string>();
        int _i;
        Action _onAction;
        float _openedAt;
        IVisualElementScheduledItem _tick;

        public DialogueOverlay(GameUI ui) : base(ui, "overlay", "dialogue")
        {
            Root.pickingMode = PickingMode.Position;
            var box = UIKit.E("panel", "dialogue__box");
            Root.Add(box);
            var row = UIKit.E("dialogue__row");
            _face = UIKit.L("", "dialogue__face");
            row.Add(_face);
            var col = UIKit.E("dialogue__col");
            _who = UIKit.L("", "dialogue__who");
            _text = UIKit.L("", "dialogue__text");
            col.Add(_who); col.Add(_text);
            row.Add(col);
            box.Add(row);
            var foot = UIKit.E("dialogue__foot");
            _page = UIKit.L("", "small", "muted", "dialogue__page");
            foot.Add(_page);
            _action = UIKit.B("", DoAction, "btn--primary", "dialogue__btn");
            foot.Add(_action);
            _next = UIKit.B("", Next, "btn--secondary", "dialogue__btn");
            foot.Add(_next);
            box.Add(foot);
        }

        public void Show(string speaker, string face, List<string> pages, string actionLabel, Action onAction)
        {
            _who.text = speaker; _face.text = face;
            _pages = pages != null && pages.Count > 0 ? pages : new List<string> { "…" };
            _i = 0;
            _onAction = onAction;
            _action.text = actionLabel ?? "";
            _openedAt = UIKit.Now;
            Apply();
        }

        void Apply()
        {
            _text.text = _pages[_i];
            bool last = _i >= _pages.Count - 1;
            _page.text = _pages.Count > 1 ? (_i + 1) + " / " + _pages.Count : "";
            _next.text = last ? (_onAction != null ? "Not now" : "Close") : "Next ▸";
            UIKit.Show(_action, last && _onAction != null);
        }

        void Next()
        {
            if (_i < _pages.Count - 1) { _i++; Apply(); }
            else Ui.CloseDialogue();
        }

        void DoAction()
        {
            var a = _onAction;
            Ui.CloseDialogue();
            if (a != null) a();
        }

        protected override void OnShow()
        {
            if (_tick == null) _tick = Root.schedule.Execute(Tick).Every(0); else _tick.Resume();
        }

        protected override void OnHide() { if (_tick != null) _tick.Pause(); }

        void Tick()
        {
            if (!Visible || UIKit.Now - _openedAt < 0.25f) return;
            if (DungeonInput.Interact || DungeonInput.Dash) { _openedAt = UIKit.Now; Next(); }
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseDialogue();
            return true;
        }
    }
}
