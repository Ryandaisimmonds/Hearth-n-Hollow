// Hearth & Hollow — the camp screen (in town: one station's window). It only hosts and switches its parts:
//   tabs — 🍳 Cooking (CookingTab), ⚒️ Upgrades (GearTab), 🔨 Workshop (KitchenTab), ⭐ Skills (SkillsTab);
//   right — who's going and Set out (CampColumn).
// Rebuilt from session state on every Changed (cheap at this size); only the showing tab refreshes.
using System.Collections.Generic;
using HearthHollow.Core;
using HearthHollow.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public enum CampTab { Cooking, Upgrades, Workshop, Skills }   // Upgrades = gear/trinkets (was "Workshop"); Workshop = kitchen upgrades

    public class HearthScreen : UIScreen
    {
        // header
        readonly Label _day, _coins;
        readonly Button _backToTown;   // shown when this screen is a building's menu in the town preview
        readonly Label _titleL;               // "The Hearth", or the station's name in town
        readonly VisualElement _tabBarEl, _rightEl;   // hidden in town: each station shows only its own page
        readonly VisualElement _eventChip, _eventIcon;
        readonly Label _eventText;
        // tabs
        readonly Button[] _tabButtons = new Button[4];
        readonly Label _skillsBadge;
        readonly VisualElement[] _tabPages = new VisualElement[4];
        CampTab _tab = CampTab.Cooking;
        readonly CookingTab _cooking;
        readonly GearTab _gear;
        readonly KitchenTab _kitchen;
        readonly SkillsTab _skills;
        readonly CampColumn _column;   // who's going, then Set out

        static readonly string[] TabNames = { "🍳 Cooking", "⚒️ Upgrades", "🔨 Workshop", "⭐ Skills" };

        float _shownAt;

        protected override void OnShow() { _shownAt = UIKit.Now; }

        public HearthScreen(GameUI ui) : base(ui, "hearth")
        {
            // ───── header ─────
            var header = UIKit.E("header", "header--hearth");
            var brand = UIKit.E("header__brand");
            brand.Add(Art.Icon("event_none", "🔥", "ico", "header__icon"));
            _titleL = UIKit.L("The Hearth", "header__title");
            brand.Add(_titleL);
            header.Add(brand);
            var info = UIKit.E("header__info");
            // Today's camp event: click to reopen its pop-up (the merchant's wares live there).
            _eventChip = UIKit.E("chip", "chip--event", "clickable");
            _eventIcon = UIKit.E("chip__icon-holder");
            _eventChip.Add(_eventIcon);
            _eventText = UIKit.L("", "chip__text");
            _eventChip.Add(_eventText);
            _eventChip.AddManipulator(new Clickable(() => Ui.OpenCampEvent()));
            info.Add(_eventChip);
            info.Add(Art.Chip("day", "☀️", out _day));
            info.Add(Art.Chip("coin", "🪙", out _coins, "chip--coins"));
            _backToTown = UIKit.B("🏘️ Back to town", () => Ui.CloseTownMenu(), "btn--secondary", "btn--small", "header__town");
            _backToTown.tooltip = "Back to walking around Hearth's Hollow (Esc)";
            info.Add(_backToTown);
            header.Add(info);
            Root.Add(header);

            var body = UIKit.E("hearth__body");
            Root.Add(body);

            // ═════ left: tabs ═════
            var left = UIKit.E("hearth__left");
            body.Add(left);
            var tabBar = UIKit.E("camp-tabs");
            for (int i = 0; i < TabNames.Length; i++)
            {
                CampTab t = (CampTab)i;
                var b = UIKit.B(TabNames[i], () => ShowTab(t), "camp-tab");
                b.tooltip = TabNames[i].Substring(TabNames[i].IndexOf(' ') + 1) + "  (" + (i + 1) + ")";
                _tabButtons[i] = b;
                tabBar.Add(b);
            }
            _skillsBadge = UIKit.L("", "camp-tab__badge");
            _skillsBadge.pickingMode = PickingMode.Ignore;
            _tabButtons[3].Add(_skillsBadge);
            left.Add(tabBar);
            _tabBarEl = tabBar;

            var pages = UIKit.E("panel", "camp-page");
            left.Add(pages);
            for (int i = 0; i < TabNames.Length; i++)
            {
                var sv = new ScrollView(ScrollViewMode.Vertical);
                sv.AddToClassList("camp-page__scroll");
                sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                _tabPages[i] = sv;
                pages.Add(sv);
            }
            _cooking = new CookingTab(ui, _tabPages[(int)CampTab.Cooking]);
            _gear = new GearTab(ui, _tabPages[(int)CampTab.Upgrades]);
            _kitchen = new KitchenTab(ui, _tabPages[(int)CampTab.Workshop]);
            _skills = new SkillsTab(ui);
            _tabPages[(int)CampTab.Skills].Add(_skills.Root);

            // ═════ right: who's going (class, stats, skills, equipment), then set out ═════
            var right = UIKit.E("hearth__right");
            body.Add(right);
            _rightEl = right;
            _column = new CampColumn(ui, right, ShowTab);

            ApplyTab();
        }

        public void ShowTab(CampTab t) { _tab = t; ApplyTab(); if (t == CampTab.Skills) _skills.OnShow(); Refresh(); }
        public CampTab Tab { get { return _tab; } }

        void ApplyTab()
        {
            for (int i = 0; i < TabNames.Length; i++)
            {
                UIKit.Show(_tabPages[i], i == (int)_tab);
                _tabButtons[i].EnableInClassList("camp-tab--on", i == (int)_tab);
            }
        }

        // ───────────────────────── refresh ─────────────────────────
        public override void Refresh()
        {
            var p = S.Profile;
            _day.text = "Day " + p.day;
            _coins.text = p.coins + " coins";
            UIKit.Show(_backToTown, Ui.TownOpen);
            ApplyStationMode(Ui.TownOpen);
            UIKit.Show(_skillsBadge, false);   // no skill points any more (Design v2)

            RefreshEventChip();
            _column.Refresh();
            switch (_tab)
            {
                case CampTab.Cooking: _cooking.Refresh(); break;
                case CampTab.Upgrades: _gear.Refresh(); break;     // gear, trinkets, key items
                case CampTab.Workshop: _kitchen.Refresh(); break;  // kitchen upgrades
                case CampTab.Skills: _skills.Refresh(); break;
            }
        }

        void RefreshEventChip()
        {
            var h = S.Hearth;
            CampEvent ev = h != null ? h.campEvent : CampEvent.None;
            _eventChip.EnableInClassList("chip--event-on", ev != CampEvent.None);
            _eventChip.EnableInClassList("chip--event-merchant", ev == CampEvent.Merchant);
            Art.Set(_eventIcon, Art.Icon(Art.EventIconId(ev), CampEventOverlay.Emoji(ev), "ico", "chip__icon"));
            _eventText.text = CampEventOverlay.ChipText(ev);
            _eventChip.tooltip = CampEventOverlay.Title(ev) + " — click to see today's event";
        }

        /// Town: this screen is one station's window over the town — just that page (no tabs, no class column, no Set out:
        /// runs start at the Route board by the gate). At the camp screen everything is as before.
        void ApplyStationMode(bool on)
        {
            Root.EnableInClassList("hearth--station", on);
            UIKit.Show(_tabBarEl, !on);
            UIKit.Show(_rightEl, !on);
            _titleL.text = on ? StationTitle(_tab) : "The Hearth";
            _backToTown.text = on ? "✕ Close" : "🏘️ Back to town";
            if (on)
            {
                Root.style.backgroundColor = new Color(0.02f, 0.015f, 0.01f, 0.55f);   // the town, dimmed, behind the window
                Root.style.paddingLeft = Length.Percent(8); Root.style.paddingRight = Length.Percent(8);
                Root.style.paddingTop = Length.Percent(4); Root.style.paddingBottom = Length.Percent(4);
            }
            else
            {
                Root.style.backgroundColor = StyleKeyword.Null;
                Root.style.paddingLeft = StyleKeyword.Null; Root.style.paddingRight = StyleKeyword.Null;
                Root.style.paddingTop = StyleKeyword.Null; Root.style.paddingBottom = StyleKeyword.Null;
            }
        }

        static string StationTitle(CampTab t)
        {
            return t == CampTab.Cooking ? "🍳 Cooking area" : t == CampTab.Upgrades ? "⚒️ Smithy" : t == CampTab.Workshop ? "🔨 Workshop" : "⭐ Training yard";
        }

        public override bool HandleKey(KeyCode key)
        {
            if (Ui.TownOpen) return false;   // in town: no tab hotkeys, and no Enter-to-set-out (that's the Route board's job)
            if (key == KeyCode.Return || key == KeyCode.KeypadEnter) { if (UIKit.Now - _shownAt > 0.8f) _column.SetOut(); return true; }
            if (key == KeyCode.Alpha1 || key == KeyCode.Keypad1) { ShowTab(CampTab.Cooking); return true; }
            if (key == KeyCode.Alpha2 || key == KeyCode.Keypad2) { ShowTab(CampTab.Upgrades); return true; }
            if (key == KeyCode.Alpha3 || key == KeyCode.Keypad3) { ShowTab(CampTab.Workshop); return true; }
            if (key == KeyCode.Alpha4 || key == KeyCode.Keypad4) { ShowTab(CampTab.Skills); return true; }
            return false;
        }
    }
}
