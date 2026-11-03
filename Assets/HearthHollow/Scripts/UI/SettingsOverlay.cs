// Hearth & Hollow — settings overlay (gear button, top right). Full game reset for everyone;
// the Dev mode section (coins, pantry) only appears in the Unity editor and development builds.
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class SettingsOverlay : UIScreen
    {
        /// Dev tools are hidden in release builds.
        Button _sliceBtn;
        public static bool DevModeEnabled { get { return Application.isEditor || Debug.isDebugBuild; } }

        public const int DevCoins = 9999;
        public const int DevIngredientCount = 99;

        readonly ConfirmButton _reset, _flee;
        readonly VisualElement _runSection, _townSection;
        readonly Button _townBtn;
        readonly Label _townHint;

        public SettingsOverlay(GameUI ui) : base(ui, "overlay", "settings")
        {
            var box = UIKit.E("overlay__box", "settings__box");
            Root.Add(box);

            var close = UIKit.B("", () => Ui.CloseSettings(), "settings__close");
            close.tooltip = "Close (Esc)";
            box.Add(close);
            box.Add(UIKit.L("Settings", "overlay__title"));

            // ── Run (only while you're out) ──
            _runSection = UIKit.E("settings__section");
            _runSection.Add(UIKit.L("This run", "subhead"));
            _flee = new ConfirmButton("Flee home", "Click again — keep your bag, no clear bonus", () => { Ui.CloseSettings(); S.FleeHome(); }, "btn--secondary", "settings__btn");
            _runSection.Add(_flee.Button);
            _runSection.Add(UIKit.L("The dungeon is paused while this menu is open.", "muted", "small", "settings__hint"));
            box.Add(_runSection);

            // ── Hearth's Hollow: the walkable town, in preview until it does everything the camp does ──
            _townSection = UIKit.E("settings__section");
            _townSection.Add(UIKit.L("Hearth's Hollow (preview)", "subhead"));
            _townBtn = UIKit.B("", () => { if (Ui.TownOpen) Ui.CloseTown(); else Ui.OpenTown(); Ui.CloseSettings(); }, "btn--secondary", "settings__btn");
            _townSection.Add(_townBtn);
            _townHint = UIKit.L("", "muted", "small", "settings__hint");
            _townSection.Add(_townHint);
            box.Add(_townSection);

            // ── Game ──
            var game = UIKit.E("settings__section");
            game.Add(UIKit.L("Game", "subhead"));
            _reset = new ConfirmButton("Reset game", "Click again to wipe everything", () => Ui.ResetGame(), "btn--danger", "settings__btn");
            game.Add(_reset.Button);
            game.Add(UIKit.L("Deletes all progress — coins, gear, pantry and routes — and you start again.", "muted", "small", "settings__hint"));
            box.Add(game);

            // ── Dev mode ──
            if (DevModeEnabled)
            {
                var dev = UIKit.E("settings__section", "settings__section--dev");
                dev.Add(UIKit.L("Dev mode", "subhead", "subhead--dev"));
                var row = UIKit.E("settings__row");
                row.Add(Art.IconButton("coin", "🪙", "Coins → " + DevCoins.ToString("N0"), () => S.DevSetCoins(DevCoins), "btn--primary", "settings__btn"));
                row.Add(Art.IconButton("pantry", "🥕", DevIngredientCount + " of every ingredient", () => S.DevFillPantry(DevIngredientCount), "btn--primary", "settings__btn"));
                dev.Add(row);
                var rowSlice = UIKit.E("settings__row");
                _sliceBtn = UIKit.B("", ToggleSlice, "btn--secondary", "btn--small", "settings__btn");
                rowSlice.Add(_sliceBtn);
                dev.Add(rowSlice);
                var row2 = UIKit.E("settings__row");
                row2.Add(Art.IconButton("trophy", "👑", "Max everything", () => S.DevMaxEverything(), "btn--primary", "settings__btn"));   // all classes, skills, gear, kitchen + a Prime tray
                dev.Add(row2);
                var row3 = UIKit.E("settings__row");
                row3.Add(Art.IconButton("map", "📖", "Skip story chapter", () => S.DevClearChapter(), "btn--primary", "settings__btn"));
                row3.Add(Art.IconButton("trophy", "🗝️", "All key items", () => S.DevGiveKeyItems(), "btn--primary", "settings__btn"));
                dev.Add(row3);
                var row3b = UIKit.E("settings__row");
                row3b.Add(UIKit.B("📖 Reset story chapters", () => S.DevResetStory(), "btn--secondary", "btn--small", "settings__btn"));
                row3b.Add(UIKit.B("🪵 +20 building materials", () => S.DevGiveMaterials(20), "btn--secondary", "btn--small", "settings__btn"));
                dev.Add(row3b);

                // ── today: weather & visitors (also what the town shows) ──
                dev.Add(UIKit.L("Today's weather / event", "small", "settings__devhead"));
                var wx = UIKit.E("settings__row");
                wx.Add(UIKit.B("☀️ Calm", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.None), "btn--secondary", "btn--small", "settings__btn"));
                wx.Add(UIKit.B("🌧️ Storm", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.Storm), "btn--secondary", "btn--small", "settings__btn"));
                wx.Add(UIKit.B("🌕 Harvest moon", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.HarvestMoon), "btn--secondary", "btn--small", "settings__btn"));
                dev.Add(wx);
                var vis = UIKit.E("settings__row");
                vis.Add(UIKit.B("🛒 Merchant", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.Merchant), "btn--secondary", "btn--small", "settings__btn"));
                vis.Add(UIKit.B("🛡️ Armourer", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.Armourer), "btn--secondary", "btn--small", "settings__btn"));
                vis.Add(UIKit.B("🧺 Forager", () => S.DevSetCampEvent(HearthHollow.Core.CampEvent.Forager), "btn--secondary", "btn--small", "settings__btn"));
                vis.Add(UIKit.B("➡️ Next day", () => S.DevNewDay(), "btn--secondary", "btn--small", "settings__btn"));
                dev.Add(vis);

                // ── town & kitchen ──
                dev.Add(UIKit.L("Town & kitchen", "small", "settings__devhead"));
                var tw = UIKit.E("settings__row");
                tw.Add(UIKit.B("🎣 Fish again today", () => S.DevResetFishing(), "btn--secondary", "btn--small", "settings__btn"));
                tw.Add(UIKit.B("👵 Meet Old Wren again", () => S.DevResetWren(), "btn--secondary", "btn--small", "settings__btn"));
                dev.Add(tw);
                // opening arc: jump between tutorial stages (0 arrival … 4 town)
                var arc = UIKit.E("settings__row");
                arc.Add(UIKit.L("Opening arc stage:", "small"));
                for (int st = 0; st <= HearthHollow.Core.Arc.LastStage(S.Arc); st++)
                {
                    int stage = st;
                    string nm = S.Arc.stages[st] != null ? S.Arc.stages[st].name : st.ToString();
                    arc.Add(UIKit.B(st + " " + nm, () => S.DevSetArcStage(stage), "btn--secondary", "btn--small", "settings__btn"));
                }
                dev.Add(arc);
                var kt = UIKit.E("settings__row");
                kt.Add(UIKit.B("🔨 Kitchen: max all", () => S.DevKitchen(true), "btn--secondary", "btn--small", "settings__btn"));
                kt.Add(UIKit.B("🔨 Kitchen: back to level 1", () => S.DevKitchen(false), "btn--secondary", "btn--small", "settings__btn"));
                dev.Add(kt);

                dev.Add(UIKit.L("Only shown in the Unity editor and development builds.", "muted", "small", "settings__hint"));
                var devScroll = new ScrollView(ScrollViewMode.Vertical);
                devScroll.AddToClassList("settings__devscroll");
                devScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                devScroll.Add(dev);
                box.Add(devScroll);
            }
        }

        /// Dev: switch the vertical-slice gate (remembered between sessions). Old routes, classes and events come back when off.
        void ToggleSlice()
        {
            HearthHollow.Core.Slice.On = !HearthHollow.Core.Slice.On;
            PlayerPrefs.SetInt(HearthHollow.Runtime.GameBootstrap.SlicePref, HearthHollow.Core.Slice.On ? 1 : 0);
            PlayerPrefs.Save();
            if (HearthHollow.Core.Slice.On && S.Phase == HearthHollow.Core.Phase.Hearth) Ui.OpenTown();
            Refresh();
        }

        protected override void OnShow() { _reset.Reset(); _flee.Reset(); }

        public override void Refresh()
        {
            var p = S.Phase;
            UIKit.Show(_runSection, S.Run != null && p != HearthHollow.Core.Phase.Hearth && p != HearthHollow.Core.Phase.RunEnd);
            UIKit.Show(_townSection, p == HearthHollow.Core.Phase.Hearth && HearthHollow.Town.TownController.Instance != null && !HearthHollow.Core.Slice.On);   // the slice has no camp screen to go back to
            if (_sliceBtn != null) _sliceBtn.text = HearthHollow.Core.Slice.On ? "🧪 Vertical slice: ON (Cook, Meadow, Chili Ridge)" : "🧪 Vertical slice: OFF (everything visible)";
            _townBtn.text = Ui.TownOpen ? "🏕️ Back to the camp screen" : "🏘️ Visit Hearth's Hollow";
            _townHint.text = Ui.TownOpen ? "Leave the town preview and go back to the usual camp screen." : "A walkable town that will replace the camp screen once it's finished. Uses your real save.";
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseSettings();
            return true; // modal: nothing underneath reacts while settings are open
        }
    }
}
