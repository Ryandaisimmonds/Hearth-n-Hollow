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

        public GameSession Session { get; private set; }
        GameUI _ui;
        UIDocument _doc;

        void Start()
        {
            _doc = GetComponent<UIDocument>();
            ArtLibrary.Active = art;   // the UI reads art through ArtLibrary.Active (emoji fallback when null)

            GameContent gc = content != null ? content.GetContentOrDefault() : DefaultContent.Create();
            Profile profile = SaveService.Load();
            bool firstLaunch = profile == null;
            int seed = (int)(DateTime.Now.Ticks & 0x7FFFFFFF);

            Session = new GameSession(gc, profile, seed);
            if (dungeon != null) Session.Arc = dungeon.arc;   // the opening arc's stages and speeds live in the Dungeon Settings
            Session.ProfileSaveRequested += SaveNow;
            if (firstLaunch) SaveNow();

            VisualElement root = _doc.rootVisualElement;
            if (themeSheet != null) root.styleSheets.Add(themeSheet);   // colours first: the main sheet reads them via var(--hh-…)
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            else Debug.LogWarning("[Hearth & Hollow] No style sheet assigned on GameBootstrap — UI will be unstyled.");
            _ui = new GameUI(Session, root, firstLaunch);

            if (dungeon != null)
            {
                gameObject.AddComponent<HearthHollow.Dungeon.DungeonController>().Init(Session, _ui, dungeon);
                gameObject.AddComponent<HearthHollow.Town.TownController>().Init(Session, _ui, dungeon);   // Hearth's Hollow (town preview)
            }
            else
            {
                Session.UseDungeon = false;   // no dungeon kit yet: fall back to the fog-grid runs
                Debug.LogWarning("[Hearth & Hollow] No Dungeon Settings on GameBootstrap — run Hearth & Hollow > Build Dungeon Kit.");
            }
        }

        void SaveNow()
        {
            if (Session != null) SaveService.Save(Session.Profile);
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
        }
    }
}
