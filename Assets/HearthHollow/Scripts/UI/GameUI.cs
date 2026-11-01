// Hearth & Hollow — root UI controller.
// Owns the layer stack, listens to GameSession.Changed (swap/refresh screens by Phase) and
// GameSession.EventRaised (toasts + juice), and routes keyboard input from a focusable root.
//
// Layer stack (all children of the UIDocument root):
//   hh-root  (focusable, receives KeyDownEvent, theme-hearth / theme-hollow class)
//     hh-stage          ← shaken by Juice.Shake
//       ClassSelect, Hearth, RouteBoard, Dungeon HUD, Hollow (old grid), RunSummary screens
//       Fight / Gather / Rest overlays (over the Hollow)
//     settings          ← settings overlay (modal)
//     hh-gear           ← gear button, top right, on every screen
//     hh-drag-layer     ← drag ghosts (not shaken, ignores picking)
//     hh-toasts         ← toast stack (ignores picking)
using System;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class GameUI
    {
        public GameSession Session { get; private set; }
        public readonly VisualElement Root;
        public readonly VisualElement Stage;
        public readonly VisualElement DragLayer;
        public readonly VisualElement ToastLayer;
        public readonly Juice Juice;

        readonly ClassSelectScreen _classSelect;
        readonly HearthScreen _hearth;
        readonly RouteScreen _routes;
        readonly DungeonScreen _dungeon;
        readonly HollowScreen _hollow;
        readonly FightOverlay _fight;
        readonly GatherOverlay _gather;
        readonly RestOverlay _rest;
        readonly RunSummaryScreen _summary;
        readonly SettingsOverlay _settings;
        readonly CampEventOverlay _campEvent;
        readonly PantryOverlay _pantry;
        readonly CookOverlay _cook;
        readonly TownHud _townHud;   // Hearth's Hollow (town preview)
        readonly FishingOverlay _fishing;   // town fishing pond
        readonly DialogueOverlay _dialogue;   // town people talking
        readonly LadderChoiceOverlay _ladder;  // dungeon: go deeper or head home
        readonly Button _gear;
        readonly UIScreen[] _all;

        bool _classSelectOpen;
        bool _settingsOpen;
        bool _campEventOpen;
        bool _pantryOpen;
        bool _cookOpen;
        bool _fishOpen, _dialogueOpen, _ladderOpen;
        bool _townOpen, _townMenuOpen;   // town preview: walking around, or a building's menu (the camp screen) on top
        bool _routesOpen;
        Phase _lastPhase;
        IVisualElementScheduledItem _pulse;
        int _pendingHeal;

        /// Last heal reported while entering a rest tile (shown by the rest overlay).
        public int LastRestHeal { get; private set; }

        public GameUI(GameSession session, VisualElement host, bool firstLaunch)
        {
            Session = session;

            Root = UIKit.E("hh-root", "theme-hearth");
            Root.focusable = true;
            Root.pickingMode = PickingMode.Position;
            host.Add(Root);

            Stage = UIKit.E("hh-stage");
            DragLayer = UIKit.E("hh-drag-layer");
            DragLayer.pickingMode = PickingMode.Ignore;
            ToastLayer = UIKit.E("hh-toasts");
            ToastLayer.pickingMode = PickingMode.Ignore;
            Root.Add(Stage);
            Root.Add(DragLayer);
            Root.Add(ToastLayer);

            Juice = new Juice(Stage, ToastLayer);

            _classSelect = new ClassSelectScreen(this);
            _hearth = new HearthScreen(this);
            _routes = new RouteScreen(this);
            _dungeon = new DungeonScreen(this);
            _hollow = new HollowScreen(this);
            _summary = new RunSummaryScreen(this);
            _rest = new RestOverlay(this);
            _gather = new GatherOverlay(this);
            _fight = new FightOverlay(this);
            _townHud = new TownHud(this);
            _all = new UIScreen[] { _townHud, _classSelect, _hearth, _routes, _dungeon, _hollow, _summary, _rest, _gather, _fight };
            for (int i = 0; i < _all.Length; i++) Stage.Add(_all[i].Root);

            // Settings: a modal overlay above the stage (not shaken) plus the gear button that opens it.
            // Camp-event pop-up: modal above the stage, below Settings.
            _campEvent = new CampEventOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _campEvent.Root);
            _pantry = new PantryOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _pantry.Root);
            _cook = new CookOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _cook.Root);
            _fishing = new FishingOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _fishing.Root);
            _dialogue = new DialogueOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _dialogue.Root);
            _ladder = new LadderChoiceOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _ladder.Root);
            _settings = new SettingsOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _settings.Root);
            _gear = UIKit.B("", ToggleSettings, "hh-gear");
            _gear.tooltip = "Settings (Esc)";
            var gearIcon = UIKit.E("hh-gear__icon");
            gearIcon.pickingMode = PickingMode.Ignore;
            _gear.Add(gearIcon);
            Root.Insert(Root.IndexOf(DragLayer), _gear);

            // Keyboard: listen on the focusable root (TrickleDown so we still see keys if a child has focus).
            Root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            // Clicking anywhere keeps keyboard focus on the root.
            Root.RegisterCallback<PointerDownEvent>(e => Root.Focus(), TrickleDown.TrickleDown);
            Root.schedule.Execute(() => Root.Focus()).StartingIn(50);
            // Gamepad for overlays: A confirms the gather / rest overlays, Start opens or closes Settings.
            // (UI scheduler runs in real time, so this works while the dungeon is paused.)
            Root.schedule.Execute(PollGamepad).Every(0);   // every UI update, so no press is missed

            // Global pulse clock for low-HP bars (USS: .pulse-on .bar--low ...).
            _pulse = Root.schedule.Execute(() => Root.ToggleInClassList("pulse-on")).Every(420);

            _classSelectOpen = firstLaunch;
            _lastPhase = session.Phase;
            Session.Changed += OnChanged;
            Session.EventRaised += OnEvent;
            ApplyLayout();
        }

        public void Dispose()
        {
            if (Session != null)
            {
                Session.Changed -= OnChanged;
                Session.EventRaised -= OnEvent;
            }
            if (_pulse != null) _pulse.Pause();
            Root.RemoveFromHierarchy();
        }

        // ───────── class select ─────────
        public void OpenClassSelect() { if (Session.Phase == Phase.Hearth) { _classSelectOpen = true; ApplyLayout(); } }
        public void CloseClassSelect() { _classSelectOpen = false; ApplyLayout(); }

        // ───────── route board (camp → choose a route → enter) ─────────
        public void OpenRoutes() { if (Session.Phase == Phase.Hearth) { _routesOpen = true; ApplyLayout(); } }
        public void CloseRoutes() { _routesOpen = false; ApplyLayout(); }

        // ───────── skills & loadout page ─────────
        /// Skills live on the camp's Skills tab now.
        public void OpenSkills() { if (Session.Phase == Phase.Hearth) { _routesOpen = false; ApplyLayout(); _hearth.ShowTab(CampTab.Skills); } }
        public void ShowCampTab(CampTab t) { if (Session.Phase == Phase.Hearth) { _routesOpen = false; if (_townOpen) _townMenuOpen = true; ApplyLayout(); _hearth.ShowTab(t); } }

        // ───────── Hearth's Hollow (town preview) ─────────
        /// The town is open (walking, or one of its menus on top).
        public bool TownOpen { get { return _townOpen; } }
        /// The town's world is on screen (not covered by the camp screen, route board or class select).
        public bool TownWorldVisible { get { return _townOpen && Session.Phase == Phase.Hearth && !_townMenuOpen && !_routesOpen && !_classSelectOpen; } }
        /// You can walk around: the world is showing and no pop-up is open.
        public bool TownWalking { get { return TownWorldVisible && !_settingsOpen && !_campEventOpen && !_pantryOpen && !_cookOpen && !_fishOpen && !_dialogueOpen; } }
        /// Someone in town talks to you (pages of text; the last page may offer an action).
        public void OpenDialogue(string speaker, string face, System.Collections.Generic.List<string> pages, string actionLabel = null, Action onAction = null)
        {
            if (!TownWorldVisible) return;
            _dialogue.Show(speaker, face, pages, actionLabel, onAction);
            _dialogueOpen = true; ApplyLayout();
        }
        public void CloseDialogue() { _dialogueOpen = false; ApplyLayout(); }

        /// Dungeon ladder: go deeper or head home (the dungeon pauses while it's open).
        public bool LadderChoiceOpen { get { return _ladderOpen; } }
        public void OpenLadderChoice(Action deeper, Action home) { if (!InDungeonView) return; _ladder.Set(deeper, home); _ladderOpen = true; ApplyLayout(); }
        public void CloseLadderChoice() { _ladderOpen = false; ApplyLayout(); }
        public void OpenFishing() { if (TownWorldVisible) { _fishOpen = true; ApplyLayout(); } }
        public void CloseFishing() { _fishOpen = false; ApplyLayout(); }
        public void OpenTown() { if (Session.Phase != Phase.Hearth) return; _townOpen = true; _townMenuOpen = false; _routesOpen = false; _settingsOpen = false; ApplyLayout(); }
        public void CloseTown() { _townOpen = false; _townMenuOpen = false; ApplyLayout(); }
        /// A building in town opens its menu (the camp screen on that tab). Esc goes back to walking.
        public void OpenTownMenu(CampTab t) { if (!_townOpen || Session.Phase != Phase.Hearth) return; _townMenuOpen = true; ApplyLayout(); _hearth.ShowTab(t); }
        public void CloseTownMenu() { _townMenuOpen = false; ApplyLayout(); }
        public void Toast(string text) { Juice.Toast(text, null, 4f); }

        // ───────── pantry pop-up (camp) ─────────
        public void OpenPantry() { if (Session.Phase == Phase.Hearth) { _pantryOpen = true; ApplyLayout(); } }
        public void ClosePantry() { _pantryOpen = false; ApplyLayout(); }

        // ───────── the stove (camp): cook one recipe ─────────
        public void OpenCook(string recipeId) { if (Session.Phase == Phase.Hearth) { _cook.SetRecipe(recipeId); _cookOpen = true; ApplyLayout(); } }
        public void CloseCook() { _cookOpen = false; ApplyLayout(); }

        // ───────── settings ─────────
        public void OpenSettings() { _settingsOpen = true; ApplyLayout(); }
        public void CloseSettings() { _settingsOpen = false; ApplyLayout(); }
        public void ToggleSettings() { _settingsOpen = !_settingsOpen; ApplyLayout(); }
        /// The settings panel is open (the dungeon pauses while it is).
        public bool SettingsOpen { get { return _settingsOpen; } }

        // ───────── camp event pop-up ─────────
        public void OpenCampEvent() { if (Session.Phase == Phase.Hearth) { _campEventOpen = true; ApplyLayout(); } }
        public void CloseCampEvent() { _campEventOpen = false; ApplyLayout(); }
        public bool CampEventOpen { get { return _campEventOpen; } }
        /// A real-time dungeon run is on screen (dungeon, or a forage / campfire overlay over it).
        public bool InDungeonView
        {
            get
            {
                Phase p = Session.Phase;
                return Session.Run != null && Session.Run.dungeon && (p == Phase.Dungeon || p == Phase.Gather || p == Phase.Rest);
            }
        }

        /// Full wipe from the settings menu: new profile, back to camp, pick a class like a first launch.
        public void ResetGame()
        {
            _lastPhase = Phase.Hearth;   // so wiping mid-fight doesn't play a fight outro
            _settingsOpen = false;
            _campEventOpen = false;
            _routesOpen = false;
            _pantryOpen = false;
            _cookOpen = false;
            _townMenuOpen = false;
            _fishOpen = false;
            _dialogueOpen = false;
            Session.ResetGame();
            _classSelectOpen = true;
            ApplyLayout();
        }

        // ───────── session → UI ─────────
        void OnChanged()
        {
            Phase p = Session.Phase;
            if (p == Phase.Rest && _lastPhase != Phase.Rest) LastRestHeal = _pendingHeal;
            _pendingHeal = 0;
            if (_lastPhase == Phase.Fight && p != Phase.Fight && _fight.Visible)
            {
                RunOutcome ended = (p == Phase.RunEnd && Session.LastSummary != null) ? Session.LastSummary.outcome : RunOutcome.None;
                _fight.PlayOutro(ended, ApplyLayout);
            }
            if (p != Phase.Hearth) { _classSelectOpen = false; _routesOpen = false; _campEventOpen = false; _pantryOpen = false; _cookOpen = false; _townMenuOpen = false; _fishOpen = false; _dialogueOpen = false; }
            // Back at camp from an expedition: pop up today's camp event (if anything is happening).
            // (In the town, events are visitors and weather you walk into instead — no pop-up.)
            else if (_lastPhase != Phase.Hearth && !_townOpen && Session.Hearth != null && Session.Hearth.campEvent != CampEvent.None)
            {
                _campEventOpen = true;
                _routesOpen = false; _pantryOpen = false;
            }
            _lastPhase = p;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            Phase p = Session.Phase;
            bool dungeon = InDungeonView;
            bool world = dungeon || TownWorldVisible;   // the camera draws the world (dungeon or town) behind a transparent UI
            bool inRun = !dungeon && (p == Phase.Explore || p == Phase.Fight || p == Phase.Gather || p == Phase.Rest);
            bool outro = _fight.InOutro;

            _classSelect.SetVisible(p == Phase.Hearth && _classSelectOpen);
            _hearth.SetVisible(p == Phase.Hearth && !_classSelectOpen && !_routesOpen && (!_townOpen || _townMenuOpen));
            _townHud.SetVisible(TownWorldVisible);
            _routes.SetVisible(p == Phase.Hearth && !_classSelectOpen && _routesOpen);
            _hollow.SetVisible(inRun || (p == Phase.RunEnd && outro));
            _dungeon.SetVisible(dungeon);
            _summary.SetVisible(p == Phase.RunEnd && !outro);
            _rest.SetVisible(p == Phase.Rest);
            _gather.SetVisible(p == Phase.Gather);
            _fight.SetVisible(p == Phase.Fight || outro);
            _campEvent.SetVisible(_campEventOpen && p == Phase.Hearth && !_classSelectOpen);
            _pantry.SetVisible(_pantryOpen && !_campEventOpen && p == Phase.Hearth && !_classSelectOpen && !_routesOpen);
            _cook.SetVisible(_cookOpen && !_campEventOpen && !_pantryOpen && p == Phase.Hearth && !_classSelectOpen && !_routesOpen);
            _fishing.SetVisible(_fishOpen && TownWorldVisible && !_campEventOpen);
            _dialogue.SetVisible(_dialogueOpen && TownWorldVisible && !_campEventOpen);
            if (_ladderOpen && !(dungeon && p == Phase.Dungeon)) _ladderOpen = false;
            _ladder.SetVisible(_ladderOpen && !_settingsOpen);
            _settings.SetVisible(_settingsOpen);
            _gear.EnableInClassList("hh-gear--open", _settingsOpen);

            bool hollowTheme = inRun || (p == Phase.RunEnd && outro);
            Root.EnableInClassList("theme-dungeon", world);   // transparent: the dungeon / town is drawn by the camera
            Root.EnableInClassList("theme-hollow", hollowTheme && !world);
            Root.EnableInClassList("theme-hearth", !hollowTheme && !world);
            // Inline override: the root's colour transition never settles on fully transparent, so force it here.
            if (world)
            {
                Root.style.transitionDuration = new StyleList<TimeValue>(new System.Collections.Generic.List<TimeValue> { new TimeValue(0) });
                Root.style.backgroundColor = new Color(0, 0, 0, 0);
                Root.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }
            else
            {
                Root.style.transitionDuration = StyleKeyword.Null;
                Root.style.backgroundColor = StyleKeyword.Null;
                Root.style.backgroundImage = StyleKeyword.Null;
            }
        }

        void OnEvent(GameEvent e)
        {
            if (e == null) return;
            // Heals emitted during a move that lands on a rest tile are shown by the rest overlay.
            if (e.type == GameEventType.PlayerHeal && e.onPlayer && Session.Phase == Phase.Explore)
                _pendingHeal += Mathf.RoundToInt(e.amount);

            // Forward to visible screens for juice (fight numbers, hollow HUD floats, …).
            for (int i = 0; i < _all.Length; i++)
                if (_all[i].Visible) _all[i].OnGameEvent(e);
            Toasts(e);
        }

        void Toasts(GameEvent e)
        {
            switch (e.type)
            {
                case GameEventType.Loot: Juice.Toast("🎒 " + e.text, "toast--loot"); break;
                case GameEventType.BagFull: Juice.Toast("🎒 " + e.text, "toast--warn", 3f); break;
                case GameEventType.LevelUp: Juice.Toast("⭐ Level up! " + e.text, "toast--level", 3.2f); break;
                case GameEventType.Synergy: Juice.Toast("✨ Synergy: " + e.text, "toast--synergy", 3f); break;
                // MealEaten: the whole tray is eaten at set-out; the Hollow's meal panel shows it, so no toast spam.
                // in the dungeon kills pop numbers over the player and the HUD keeps the totals — no toast spam
                case GameEventType.Coins: if (!InDungeonView) Juice.Toast("🪙 " + e.text, "toast--coins"); break;
                case GameEventType.Xp: if (!InDungeonView) Juice.Toast("✨ " + e.text, "toast--xp"); break;
                case GameEventType.RunCleared: Juice.Toast("🌀 " + e.text, "toast--level", 3f); break;
                case GameEventType.RunFled: Juice.Toast("🏃 " + e.text, null); break;
                case GameEventType.Info:
                    if (!string.IsNullOrEmpty(e.text)) Juice.Toast(e.text, null);
                    break;
            }
        }

        // ───────── keyboard ─────────
        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.None) return; // the character-only half of a key press
            if (RouteKey(e.keyCode)) e.StopPropagation();
        }

        /// Sends a key to the top-most screen that wants it. Returns true if something handled it.
        bool RouteKey(KeyCode k)
        {
            // Settings are modal; Esc opens them from anywhere except class select / route board (where Esc goes back).
            if (_settings.Visible) { _settings.HandleKey(k); return true; }
            if (_ladder.Visible) { _ladder.HandleKey(k); return true; }
            if (_campEvent.Visible) { _campEvent.HandleKey(k); return true; }
            if (_pantry.Visible) { _pantry.HandleKey(k); return true; }
            if (_cook.Visible) { _cook.HandleKey(k); return true; }
            if (_fishing.Visible) { _fishing.HandleKey(k); return true; }
            if (_dialogue.Visible) { _dialogue.HandleKey(k); return true; }
            if (k == KeyCode.Escape && _townOpen && _townMenuOpen && _hearth.Visible) { CloseTownMenu(); return true; }   // town: back to walking
            if (k == KeyCode.Escape && !_classSelect.Visible && !_routes.Visible) { OpenSettings(); return true; }
            // Top-most first.
            UIScreen[] order = { _fight, _gather, _rest, _summary, _dungeon, _hollow, _classSelect, _routes, _hearth };
            for (int i = 0; i < order.Length; i++)
            {
                if (!order[i].Visible) continue;
                if (order[i].HandleKey(k)) return true;
                // Overlays swallow keys meant for screens underneath (no walking during a fight).
                if (order[i] == _fight || order[i] == _gather || order[i] == _rest) return false;
            }
            return false;
        }

        void PollGamepad()
        {
            var g = UnityEngine.InputSystem.Gamepad.current;
            if (g == null) return;
            if (g.startButton.wasPressedThisFrame) { RouteKey(KeyCode.Escape); return; }
            if (_ladder.Visible && !_settings.Visible)
            {
                if (g.buttonSouth.wasPressedThisFrame) RouteKey(KeyCode.Return);
                else if (g.buttonWest.wasPressedThisFrame) RouteKey(KeyCode.H);
                else if (g.buttonEast.wasPressedThisFrame) RouteKey(KeyCode.Escape);
                return;
            }
            // A = confirm, only on the overlays a gamepad player can reach from the dungeon (camp screens stay mouse/keyboard)
            if (g.buttonEast.wasPressedThisFrame && _campEvent.Visible && !_settings.Visible) { CloseCampEvent(); return; }
            if (g.buttonSouth.wasPressedThisFrame && (_gather.Visible || _rest.Visible || _summary.Visible || (_campEvent.Visible && !_settings.Visible)))
                RouteKey(KeyCode.Return);
        }
    }
}
