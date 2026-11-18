// Hearth & Hollow — scene entry point. Put this on a GameObject with a UIDocument (+ PanelSettings).
// Loads the profile, creates the GameSession, builds the UI and saves when the session asks.
//
// Emoji: assign an emoji-capable font asset as a fallback in the PanelSettings' Text Settings,
// otherwise emoji show as empty boxes (every emoji has a text label alongside, so it stays playable).
using System;
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.Runtime
{
    [RequireComponent(typeof(UIDocument))]
    public class GameBootstrap : MonoBehaviour
    {
        public GameContentAsset content;
        public StyleSheet styleSheet;
        [Tooltip("The colour palette (UI/Theme.uss): --hh-… variables that HearthHollow.uss uses. Loaded before the main style sheet.")]
        public StyleSheet themeSheet;
        [Tooltip("Sprites for every icon, monster and class. Built by Hearth & Hollow > Build Art Library.")]
        public ArtLibrary art;
        [Tooltip("Themes, room prefabs and monster behaviour for the real-time dungeon. Built by Hearth & Hollow > Build Dungeon Kit.")]
        public HearthHollow.Dungeon.DungeonSettings dungeon;
        [Tooltip("The Effects Lab (Scenes/EffectsLab): a throwaway profile with everything unlocked, straight into a dungeon, " +
                 "with the lab menu (F1). Never loads or writes the save.")]
        public bool effectsLab;

        public GameSession Session { get; private set; }
        GameUI _ui;
        /// The UI facade (the play-mode smoke test drives it).
        public GameUI UI { get { return _ui; } }
        public const string SlicePref = "hh_slice";

        // ── text size (ticket 22): one knob that scales the whole UI, text and boxes together ──
        public const string TextSizePref = "hh_text_size";
        public static readonly string[] TextSizeNames = { "Normal", "Large", "Extra large" };
        public static readonly float[] TextSizeScales = { 1f, 1.15f, 1.3f };
        public static int TextSize { get; private set; }
        static PanelSettings _panel;
        static Vector2Int _panelBaseRes;

        /// Apply a text size (0 Normal, 1 Large, 2 Extra large) and remember it. Default: Large.
        public static void SetTextSize(int level)
        {
            TextSize = Mathf.Clamp(level, 0, TextSizeScales.Length - 1);
            PlayerPrefs.SetInt(TextSizePref, TextSize); PlayerPrefs.Save();
            if (_panel == null) return;
            float k = TextSizeScales[TextSize];   // a smaller reference resolution draws everything bigger
            _panel.referenceResolution = new Vector2Int(Mathf.RoundToInt(_panelBaseRes.x / k), Mathf.RoundToInt(_panelBaseRes.y / k));
        }
        UIDocument _doc;

        void Start()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc.panelSettings != null)   // a runtime copy, so the text-size setting never edits the asset
            {
                _panelBaseRes = _doc.panelSettings.referenceResolution;
                _panel = Instantiate(_doc.panelSettings);
                _doc.panelSettings = _panel;
                SetTextSize(PlayerPrefs.GetInt(TextSizePref, 1));
            }
            ArtLibrary.Active = art;   // the UI reads art through ArtLibrary.Active (emoji fallback when null)

            GameContent gc = content != null ? content.GetContentOrDefault() : DefaultContent.Create();
            HearthHollow.UI.FoodArt.Install(gc);   // ticket 54: pixel art for every ingredient and dish
            Slice.On = effectsLab ? false : PlayerPrefs.GetInt(SlicePref, 1) == 1;   // the vertical slice gate (dev panel can switch it off; the lab has everything)
            Profile profile = effectsLab ? null : SaveService.Load();   // the lab never touches the save
            bool firstLaunch = profile == null && !effectsLab;
            int seed = (int)(DateTime.Now.Ticks & 0x7FFFFFFF);

            Session = new GameSession(gc, profile, seed);
            GameAudio.Create(Session);   // ticket 62: sound and music
            if (dungeon != null) Session.Arc = dungeon.arc;   // the opening arc's stages and speeds live in the Dungeon Settings
            if (!effectsLab) Session.ProfileSaveRequested += SaveNow;
            if (firstLaunch) SaveNow();

            VisualElement root = _doc.rootVisualElement;
            if (themeSheet != null) root.styleSheets.Add(themeSheet);   // colours first: the main sheet reads them via var(--hh-…)
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            else Debug.LogWarning("[Hearth & Hollow] No style sheet assigned on GameBootstrap — UI will be unstyled.");
            _ui = new GameUI(Session, root, firstLaunch);
            if (Features.TownIsHome && !effectsLab) _ui.OpenTown();   // the town is the hub; the old camp screen isn't used

            if (dungeon != null)
            {
                gameObject.AddComponent<HearthHollow.Dungeon.DungeonController>().Init(Session, _ui, dungeon);
                if (effectsLab) { gameObject.AddComponent<HearthHollow.Lab.EffectsLab>().Init(Session, _ui); return; }   // no town, no fall
                gameObject.AddComponent<HearthHollow.Town.TownController>().Init(Session, _ui, dungeon);   // Hearth's Hollow (town preview)
                if (Session.Story.BeginFall())   // ticket 42: a new game opens with a fall into Meadow Hollow
                    root.schedule.Execute(() => _ui.Juice.Toast(Story.FallIntro, "toast--level", 7f)).StartingIn(1200);   // once the dungeon's on screen
            }
            else
            {
                Debug.LogError("[Hearth & Hollow] No Dungeon Settings on GameBootstrap — run Hearth & Hollow > Build Dungeon Kit.");
            }
        }

        void SaveNow()
        {
            if (Session != null && !effectsLab) SaveService.Save(Session.Profile);
        }

        void OnApplicationQuit()
        {
            SaveNow();
        }

        // a force-quit or crash never reaches OnApplicationQuit, so also save whenever the game is put in the background
        void OnApplicationFocus(bool focused) { if (!focused) SaveNow(); }
        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        void OnDestroy()
        {
            if (Session != null) Session.ProfileSaveRequested -= SaveNow;
            if (_ui != null) _ui.Dispose();
            // domain reload is off: the lab's "everything on" mustn't leak into edit-mode tests or the next Play
            if (effectsLab) Slice.On = PlayerPrefs.GetInt(SlicePref, 1) == 1;
        }
    }
}
