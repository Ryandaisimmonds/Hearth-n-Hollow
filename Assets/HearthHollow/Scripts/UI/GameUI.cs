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
        readonly GatherOverlay _gather;
        readonly RestOverlay _rest;
        readonly RunSummaryScreen _summary;
        readonly SettingsOverlay _settings;
        readonly CampEventOverlay _campEvent;
        readonly PantryOverlay _pantry;
        readonly CookOverlay _cook;
        readonly SeasonOverlay _season;   // the spice rack
        readonly TownHud _townHud;   // Hearth's Hollow (town preview)
        readonly FishingOverlay _fishing;   // town fishing pond
        readonly DialogueOverlay _dialogue;   // town people talking
        readonly LadderChoiceOverlay _ladder;  // dungeon: go deeper or head home
        readonly Button _gear;
        readonly UIScreen[] _all;

        /// What's open, oldest first. Places (Town, a Station over it, the Route board, class select) and pop-ups
        /// share one stack: the newest pop-up is the one you see, and Esc goes to whatever is on top. With no Town
        /// in it, home is the old camp screen.
        enum Layer { Town, Station, ClassSelect, Routes, CampEvent, Pantry, Cook, Season, Fishing, Dialogue, Ladder, Settings }
        readonly System.Collections.Generic.List<Layer> _open = new System.Collections.Generic.List<Layer>();
        static readonly Layer[] PopUps = { Layer.CampEvent, Layer.Pantry, Layer.Cook, Layer.Season, Layer.Fishing, Layer.Dialogue };
        /// Only make sense in town at home: they close when a run starts.
        static readonly Layer[] HomeOnly = { Layer.Station, Layer.ClassSelect, Layer.Routes, Layer.CampEvent, Layer.Pantry, Layer.Cook, Layer.Season, Layer.Fishing, Layer.Dialogue };

        bool Has(Layer l) { return _open.Contains(l); }
        void Open(Layer l) { _open.Remove(l); _open.Add(l); ApplyLayout(); }
        void Close(Layer l) { _open.Remove(l); ApplyLayout(); }
        void Drop(params Layer[] ls) { foreach (var l in ls) _open.Remove(l); }
        /// The newest pop-up that's open (null = none).
        Layer? TopPopUp
        {
            get
            {
                for (int i = _open.Count - 1; i >= 0; i--) if (System.Array.IndexOf(PopUps, _open[i]) >= 0) return _open[i];
                return null;
            }
        }
        bool Top(Layer l) { var t = TopPopUp; return t.HasValue && t.Value == l; }
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
#if UNITY_EDITOR
            UseRuntimeFont();
#endif

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
            _summary = new RunSummaryScreen(this);
            _rest = new RestOverlay(this);
            _gather = new GatherOverlay(this);
            _townHud = new TownHud(this);
            _all = new UIScreen[] { _townHud, _classSelect, _hearth, _routes, _dungeon, _summary, _rest, _gather };
            for (int i = 0; i < _all.Length; i++) Stage.Add(_all[i].Root);

            // Settings: a modal overlay above the stage (not shaken) plus the gear button that opens it.
            // Camp-event pop-up: modal above the stage, below Settings.
            _campEvent = new CampEventOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _campEvent.Root);
            _pantry = new PantryOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _pantry.Root);
            _cook = new CookOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _cook.Root);
            _season = new SeasonOverlay(this);
            Root.Insert(Root.IndexOf(DragLayer), _season.Root);
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

            if (firstLaunch && Features.ClassSelect) _open.Add(Layer.ClassSelect);   // the slice has only the Cook, and opens with a fall
            _lastPhase = session.Phase;
            Session.Changed += OnChanged;
            Session.EventRaised += OnEvent;
            ApplyLayout();
        }

#if UNITY_EDITOR
        /// Editor only: the text draws from a font built in memory from the same TTF and settings, so pressing Play
        /// never writes new glyphs into UI/Fonts/Changa SDF.asset (its dynamic atlas used to change on every Play and
        /// show up in git). Builds use the asset as it is (it clears its dynamic data on build).
        void UseRuntimeFont()
        {
            const string Saved = "Assets/HearthHollow/UI/Fonts/Changa SDF.asset", Ttf = "Assets/HearthHollow/UI/Fonts/Changa-Variable.ttf";
            var saved = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(Saved);
            var ttf = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(Ttf);
            if (saved == null || ttf == null) return;   // fall back to the stylesheet's font
            var f = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(ttf, (int)saved.faceInfo.pointSize, saved.atlasPadding, saved.atlasRenderMode,
                                                                        saved.atlasWidth, saved.atlasHeight, UnityEngine.TextCore.Text.AtlasPopulationMode.Dynamic, true);
            if (f == null) return;
            f.name = saved.name + " (play)";
            Root.style.unityFontDefinition = FontDefinition.FromSDFFont(f);
        }
#endif

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
        public void OpenClassSelect() { if (Session.Phase == Phase.Hearth) Open(Layer.ClassSelect); }
        public void CloseClassSelect() { Close(Layer.ClassSelect); }

        // ───────── route board (camp → choose a route → enter) ─────────
        public void OpenRoutes() { if (Session.Phase == Phase.Hearth) Open(Layer.Routes); }
        public void CloseRoutes() { Close(Layer.Routes); }

        // ───────── skills & loadout page ─────────
        /// Skills live on the camp's Skills tab now.
        public void OpenSkills() { if (Session.Phase == Phase.Hearth) { Close(Layer.Routes); _hearth.ShowTab(CampTab.Skills); } }
        public void ShowCampTab(CampTab t) { if (Session.Phase == Phase.Hearth) { Drop(Layer.Routes); if (Has(Layer.Town)) Open(Layer.Station); else ApplyLayout(); _hearth.ShowTab(t); } }

        // ───────── Hearth's Hollow (the town) ─────────
        /// The town is open (walking, or one of its menus on top).
        public bool TownOpen { get { return Has(Layer.Town); } }
        /// The town's world is on screen (not covered by the camp screen, route board or class select).
        public bool TownWorldVisible { get { return Has(Layer.Town) && Session.Phase == Phase.Hearth && !Has(Layer.Station) && !Has(Layer.Routes) && !Has(Layer.ClassSelect); } }
        /// A town station's page (cooking, smithy, workshop, training yard) is open as a window over the town.
        public bool TownStationOpen { get { return Has(Layer.Town) && Has(Layer.Station) && Session.Phase == Phase.Hearth && !Has(Layer.Routes) && !Has(Layer.ClassSelect); } }
        /// You can walk around: the world is showing and no pop-up is open.
        public bool TownWalking { get { return TownWorldVisible && !Has(Layer.Settings) && !TopPopUp.HasValue; } }
        /// Someone in town talks to you (pages of text; the last page may offer an action).
        public void OpenDialogue(string speaker, string face, System.Collections.Generic.List<string> pages, string actionLabel = null, Action onAction = null)
        {
            if (!TownWorldVisible) return;
            _dialogue.Show(speaker, face, pages, actionLabel, onAction);
            Open(Layer.Dialogue);
        }
        public void CloseDialogue() { Close(Layer.Dialogue); }

        /// Dungeon ladder: go deeper or head home (the dungeon pauses while it's open).
        public bool LadderChoiceOpen { get { return Has(Layer.Ladder); } }
        public void OpenLadderChoice(Action deeper, Action home) { if (!InDungeonView) return; _ladder.Set(deeper, home); Open(Layer.Ladder); }
        public void CloseLadderChoice() { Close(Layer.Ladder); }
        public void OpenFishing() { if (TownWorldVisible) Open(Layer.Fishing); }
        public void CloseFishing() { Close(Layer.Fishing); }
        public void OpenTown() { if (Session.Phase != Phase.Hearth) return; Drop(Layer.Station, Layer.Routes, Layer.Settings); Open(Layer.Town); }
        public void CloseTown() { Drop(Layer.Station); Close(Layer.Town); }
        /// A building in town opens its menu (the camp screen on that tab). Esc goes back to walking.
        public void OpenTownMenu(CampTab t) { if (!Has(Layer.Town) || Session.Phase != Phase.Hearth) return; Open(Layer.Station); _hearth.ShowTab(t); }
        public void CloseTownMenu() { Close(Layer.Station); }
        public void Toast(string text) { Juice.Toast(text, null, 4f); }

        // ───────── pantry pop-up (camp) ─────────
        public void OpenPantry() { if (Session.Phase == Phase.Hearth) Open(Layer.Pantry); }
        public void ClosePantry() { Close(Layer.Pantry); }

        // ───────── the stove (camp): cook one recipe ─────────
        public void OpenCook(string recipeId) { if (Session.Phase == Phase.Hearth) { _cook.SetRecipe(recipeId); Open(Layer.Cook); } }
        public void CloseCook() { Close(Layer.Cook); }
        public void OpenSeason() { if (Session.Phase == Phase.Hearth) Open(Layer.Season); }
        public void CloseSeason() { Close(Layer.Season); }

        // ───────── settings ─────────
        public void OpenSettings() { Open(Layer.Settings); }
        public void CloseSettings() { Close(Layer.Settings); }
        public void ToggleSettings() { if (Has(Layer.Settings)) CloseSettings(); else OpenSettings(); }
        /// The settings panel is open (the dungeon pauses while it is).
        public bool SettingsOpen { get { return Has(Layer.Settings); } }

        // ───────── camp event pop-up ─────────
        public void OpenCampEvent() { if (Session.Phase == Phase.Hearth) Open(Layer.CampEvent); }
        public void CloseCampEvent() { Close(Layer.CampEvent); }
        public bool CampEventOpen { get { return Has(Layer.CampEvent); } }
        /// A real-time dungeon run is on screen (dungeon, or a forage / campfire overlay over it).
        public bool InDungeonView
        {
            get
            {
                Phase p = Session.Phase;
                return Session.Run != null && Session.Run.dungeon && (p == Phase.Dungeon || p == Phase.Gather || p == Phase.Rest);
            }
        }

        /// Full wipe from the settings menu: new profile, back home, pick a class like a first launch.
        public void ResetGame()
        {
            _lastPhase = Phase.Hearth;
            _open.RemoveAll(l => l != Layer.Town && l != Layer.Ladder);
            Session.ResetGame();
            if (Features.ClassSelect) _open.Add(Layer.ClassSelect);
            ApplyLayout();
            if (Features.OpensWithFall && Session.Story.BeginFall())   // a fresh game opens with the fall
                Root.schedule.Execute(() => Juice.Toast(Story.FallIntro, "toast--level", 7f)).StartingIn(1200);
        }

        // ───────── session → UI ─────────
        void OnChanged()
        {
            Phase p = Session.Phase;
            if (p == Phase.Rest && _lastPhase != Phase.Rest) LastRestHeal = _pendingHeal;
            _pendingHeal = 0;
            if (p == Phase.Hearth && _lastPhase != Phase.Hearth && Features.TownIsHome) { Drop(Layer.Station); if (!Has(Layer.Town)) _open.Insert(0, Layer.Town); }   // home is the town
            if (p != Phase.Hearth) Drop(HomeOnly);
            // Back at camp from an expedition: pop up today's camp event (if anything is happening).
            // (In the town, events are visitors and weather you walk into instead — no pop-up.)
            else if (_lastPhase != Phase.Hearth && !Has(Layer.Town) && Session.Hearth != null && Session.Hearth.campEvent != CampEvent.None)
            {
                Drop(Layer.Routes, Layer.Pantry);
                _open.Remove(Layer.CampEvent); _open.Add(Layer.CampEvent);
            }
            if (p == Phase.RunEnd && _lastPhase != Phase.RunEnd && Session.Story.FallEnding) FadeOutOfTheFall();
            _lastPhase = p;
            ApplyLayout();
        }

        public void ApplyLayout()
        {
            Phase p = Session.Phase;
            bool home = p == Phase.Hearth;
            bool dungeon = InDungeonView;
            bool world = dungeon || TownWorldVisible || TownStationOpen;   // the camera draws the world (dungeon or town) behind a transparent UI (town stations open over it)
            bool classSelect = Has(Layer.ClassSelect), routes = Has(Layer.Routes), settings = Has(Layer.Settings);

            _classSelect.SetVisible(home && classSelect);
            _hearth.SetVisible(home && !classSelect && !routes && (!Has(Layer.Town) || Has(Layer.Station)));
            _townHud.SetVisible(TownWorldVisible);
            _routes.SetVisible(home && !classSelect && routes);
            _dungeon.SetVisible(dungeon);
            _summary.SetVisible(p == Phase.RunEnd && !Session.Story.FallEnding);   // the fall has no summary
            _rest.SetVisible(p == Phase.Rest);
            _gather.SetVisible(p == Phase.Gather);
            // pop-ups: only the newest one shows
            _campEvent.SetVisible(Top(Layer.CampEvent) && home && !classSelect);
            _pantry.SetVisible(Top(Layer.Pantry) && home && !classSelect && !routes);
            _cook.SetVisible(Top(Layer.Cook) && home && !classSelect && !routes);
            _season.SetVisible(Top(Layer.Season) && home && !classSelect && !routes);
            _fishing.SetVisible(Top(Layer.Fishing) && TownWorldVisible);
            _dialogue.SetVisible(Top(Layer.Dialogue) && TownWorldVisible);
            if (Has(Layer.Ladder) && !(dungeon && p == Phase.Dungeon)) _open.Remove(Layer.Ladder);
            _ladder.SetVisible(Has(Layer.Ladder) && !settings);
            _settings.SetVisible(settings);
            _gear.EnableInClassList("hh-gear--open", settings);

            Root.EnableInClassList("theme-dungeon", world);   // transparent: the dungeon / town is drawn by the camera
            Root.EnableInClassList("theme-hearth", !world);
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
            // Heals emitted as you sit down at a campfire are shown by the rest overlay.
            if (e.type == GameEventType.PlayerHeal && e.onPlayer && Session.Phase == Phase.Dungeon)
                _pendingHeal += Mathf.RoundToInt(e.amount);

            // Forward to visible screens for juice (fight numbers, hollow HUD floats, …).
            for (int i = 0; i < _all.Length; i++)
                if (_all[i].Visible) _all[i].OnGameEvent(e);
            Toasts(e);
        }

        void Toasts(GameEvent e)
        {
            switch (e.type)   // the sound of it
            {
                case GameEventType.Loot: HearthHollow.Runtime.GameAudio.Play("pickup"); break;
                case GameEventType.Coins: HearthHollow.Runtime.GameAudio.Play("coin"); break;
                case GameEventType.LevelUp: HearthHollow.Runtime.GameAudio.Play("level_up"); break;
                case GameEventType.RunCleared: HearthHollow.Runtime.GameAudio.Play("run_cleared"); break;
                case GameEventType.PlayerDied: HearthHollow.Runtime.GameAudio.Play("died"); break;
            }
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

        // ───────── the fall ends in a fade, then you wake in town ─────────
        VisualElement _fade;
        void FadeOutOfTheFall()
        {
            if (_fade == null)
            {
                _fade = UIKit.E("fall-fade");
                _fade.Add(UIKit.L("…the fog closes in", "fall-fade__text"));
                Root.Add(_fade);
            }
            _fade.BringToFront();
            _fade.style.display = DisplayStyle.Flex;
            _fade.RemoveFromClassList("fall-fade--out");
            _fade.style.opacity = 0f;
            float start = UIKit.Now;
            bool woke = false;
            _fade.schedule.Execute(() =>
            {
                float t = UIKit.Now - start;
                if (t < 1.2f) _fade.style.opacity = t / 1.2f;                       // to black
                else if (!woke && t >= 3.2f) { woke = true; Session.ReturnToHearth(); }   // …and wake up in town
                else if (woke) _fade.style.opacity = Mathf.Clamp01(1f - (t - 3.2f) / 1.4f);
                else _fade.style.opacity = 1f;
                if (woke && t >= 4.6f) _fade.style.display = DisplayStyle.None;
            }).Every(16).Until(() => woke && UIKit.Now - start >= 4.6f);
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
            if (_season.Visible) { _season.HandleKey(k); return true; }
            if (_fishing.Visible) { _fishing.HandleKey(k); return true; }
            if (_dialogue.Visible) { _dialogue.HandleKey(k); return true; }
            if (k == KeyCode.Escape && Has(Layer.Town) && Has(Layer.Station) && _hearth.Visible) { CloseTownMenu(); return true; }   // town: back to walking
            if (k == KeyCode.Escape && !_classSelect.Visible && !_routes.Visible) { OpenSettings(); return true; }
            // Top-most first.
            UIScreen[] order = { _gather, _rest, _summary, _dungeon, _classSelect, _routes, _hearth };
            for (int i = 0; i < order.Length; i++)
            {
                if (!order[i].Visible) continue;
                if (order[i].HandleKey(k)) return true;
                // Overlays swallow keys meant for screens underneath (no walking during a fight).
                if (order[i] == _gather || order[i] == _rest) return false;
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
            // the stove: A = cook / tap, X = quick cook, B = close
            if (_cook.Visible && !_settings.Visible)
            {
                if (g.buttonSouth.wasPressedThisFrame) RouteKey(KeyCode.Return);
                else if (g.buttonWest.wasPressedThisFrame) RouteKey(KeyCode.Q);
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
