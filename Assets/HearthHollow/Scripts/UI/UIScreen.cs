// Hearth & Hollow — base class for every screen / overlay.
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public abstract class UIScreen
    {
        public readonly VisualElement Root;
        protected readonly GameUI Ui;
        protected GameSession S { get { return Ui.Session; } }
        protected GameContent C { get { return Ui.Session.Content; } }

        public bool Visible { get; private set; }

        protected UIScreen(GameUI ui, params string[] classes)
        {
            Ui = ui;
            Root = UIKit.E("screen");
            for (int i = 0; i < classes.Length; i++) Root.AddToClassList(classes[i]);
            UIKit.Show(Root, false);
        }

        public void SetVisible(bool on)
        {
            if (on == Visible) { if (on) Refresh(); return; }
            Visible = on;
            UIKit.Show(Root, on);
            if (on) { OnShow(); Refresh(); }
            else OnHide();
        }

        /// Rebuild/refresh from session state. Called on every session.Changed while visible.
        public virtual void Refresh() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        /// Juice hook: called for each session.EventRaised while visible.
        public virtual void OnGameEvent(GameEvent e) { }

        /// Return true if the key was consumed.
        public virtual bool HandleKey(KeyCode key) { return false; }
    }
}
