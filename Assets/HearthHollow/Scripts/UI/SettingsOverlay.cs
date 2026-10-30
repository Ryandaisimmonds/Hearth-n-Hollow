// Hearth & Hollow — settings overlay (gear button, top right). Full game reset for everyone;
// the Dev mode section (coins, pantry) only appears in the Unity editor and development builds.
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class SettingsOverlay : UIScreen
    {
        /// Dev tools are hidden in release builds.
        public static bool DevModeEnabled { get { return Application.isEditor || Debug.isDebugBuild; } }

        public const int DevCoins = 9999;
        public const int DevIngredientCount = 99;

        readonly ConfirmButton _reset, _flee;
        readonly VisualElement _runSection;

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

            // ── Game ──
            var game = UIKit.E("settings__section");
            game.Add(UIKit.L("Game", "subhead"));
            _reset = new ConfirmButton("Reset game", "Click again to wipe everything", () => Ui.ResetGame(), "btn--danger", "settings__btn");
            game.Add(_reset.Button);
            game.Add(UIKit.L("Deletes all progress — coins, levels, gear, pantry and routes — and you pick a class again.", "muted", "small", "settings__hint"));
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
                var row2 = UIKit.E("settings__row");
                row2.Add(Art.IconButton("xp", "⭐", "+10 levels (current class)", () => S.DevAddLevels(S.Run != null ? S.Run.classId : S.Profile.selectedClass, 10), "btn--primary", "settings__btn"));
                dev.Add(row2);
                var row3 = UIKit.E("settings__row");
                row3.Add(Art.IconButton("map", "📖", "Skip story chapter", () => S.DevClearChapter(), "btn--primary", "settings__btn"));
                row3.Add(Art.IconButton("trophy", "🗝️", "All key items", () => S.DevGiveKeyItems(), "btn--primary", "settings__btn"));
                dev.Add(row3);
                dev.Add(UIKit.L("Only shown in the Unity editor and development builds.", "muted", "small", "settings__hint"));
                box.Add(dev);
            }
        }

        protected override void OnShow() { _reset.Reset(); _flee.Reset(); }

        public override void Refresh()
        {
            var p = S.Phase;
            UIKit.Show(_runSection, S.Run != null && p != HearthHollow.Core.Phase.Hearth && p != HearthHollow.Core.Phase.RunEnd);
        }

        public override bool HandleKey(KeyCode key)
        {
            if (key == KeyCode.Escape) Ui.CloseSettings();
            return true; // modal: nothing underneath reacts while settings are open
        }
    }
}
