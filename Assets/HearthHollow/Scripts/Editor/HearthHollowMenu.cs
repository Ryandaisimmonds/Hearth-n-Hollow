// Hearth & Hollow — editor menu: content asset, one-click scene builder, save reset.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using HearthHollow.Core;
using HearthHollow.Data;
using HearthHollow.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace HearthHollow.EditorTools
{
    public static class HearthHollowMenu
    {
        const string Root = "Assets/HearthHollow";
        const string ContentFolder = Root + "/Content";
        const string UIFolder = Root + "/UI";
        const string ScenesFolder = Root + "/Scenes";
        const string ContentPath = ContentFolder + "/GameContent.asset";
        const string PanelPath = UIFolder + "/HearthHollowPanel.asset";
        const string UssPath = UIFolder + "/HearthHollow.uss";
        const string ScenePath = ScenesFolder + "/HearthHollow.unity";
        const string BootstrapTypeName = "HearthHollow.Runtime.GameBootstrap";

        // ───────────── Content asset ─────────────
        [MenuItem("Hearth & Hollow/Create Content Asset", false, 1)]
        public static void CreateContentAssetMenu()
        {
            GameContentAsset asset = EnsureContentAsset();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        static GameContentAsset EnsureContentAsset()
        {
            EnsureFolder(ContentFolder);
            GameContentAsset asset = AssetDatabase.LoadAssetAtPath<GameContentAsset>(ContentPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<GameContentAsset>();
                asset.content = DefaultContent.Create();
                AssetDatabase.CreateAsset(asset, ContentPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[Hearth & Hollow] Created content asset at " + ContentPath);
            }
            return asset;
        }

        // ───────────── Scene builder ─────────────
        [MenuItem("Hearth & Hollow/Build Game Scene", false, 2)]
        public static void BuildGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(ContentFolder);
            EnsureFolder(UIFolder);
            EnsureFolder(ScenesFolder);

            GameContentAsset content = EnsureContentAsset();
            PanelSettings panel = EnsurePanelSettings();
            StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (uss == null) Debug.LogWarning("[Hearth & Hollow] No stylesheet at " + UssPath + " yet — GameBootstrap.styleSheet left empty.");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x12, 0x20, 0x19, 0xFF);
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // Game root: UIDocument + GameBootstrap
            var game = new GameObject("Game");
            UIDocument doc = game.AddComponent<UIDocument>();
            doc.panelSettings = panel;

            Type bootType = FindType(BootstrapTypeName);
            if (bootType == null)
            {
                Debug.LogWarning("[Hearth & Hollow] Type " + BootstrapTypeName + " not found — add the GameBootstrap component to 'Game' manually once it compiles.");
            }
            else if (!typeof(Component).IsAssignableFrom(bootType))
            {
                Debug.LogWarning("[Hearth & Hollow] " + BootstrapTypeName + " is not a Component.");
            }
            else
            {
                Component boot = game.AddComponent(bootType);
                var so = new SerializedObject(boot);
                SetObjectField(so, "content", content);
                if (uss != null) SetObjectField(so, "styleSheet", uss);
                var theme = AssetDatabase.LoadAssetAtPath<StyleSheet>(UIFolder + "/Theme.uss");
                if (theme != null) SetObjectField(so, "themeSheet", theme);
                SetObjectField(so, "art", ArtLibraryBuilder.EnsureLibrary());
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[Hearth & Hollow] Failed to save scene at " + ScenePath);
                return;
            }
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Hearth & Hollow] Built scene " + ScenePath + " — press Play!");
        }

        static PanelSettings EnsurePanelSettings()
        {
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            bool created = false;
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                created = true;
            }
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;

            if (panel.themeStyleSheet == null)
            {
                ThemeStyleSheet theme = FindThemeStyleSheet();
                if (theme != null) panel.themeStyleSheet = theme;
                else Debug.LogWarning("[Hearth & Hollow] No ThemeStyleSheet found in the project. Create one via Assets > Create > UI Toolkit > Default Runtime Theme File and assign it to " + PanelPath + ".");
            }

            if (created) AssetDatabase.CreateAsset(panel, PanelPath);
            else EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();
            return panel;
        }

        static ThemeStyleSheet FindThemeStyleSheet()
        {
            string[] guids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            ThemeStyleSheet fallback = null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ThemeStyleSheet t = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                if (t == null || !path.StartsWith("Assets/")) continue; // skip package themes (e.g. URP debug window)
                // Prefer the project's default runtime theme if present.
                if (path.IndexOf("Default", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase) >= 0) return t;
                if (fallback == null) fallback = t;
            }
            return fallback;
        }

        static void SetObjectField(SerializedObject so, string field, UnityEngine.Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning("[Hearth & Hollow] GameBootstrap has no serialized object field '" + field + "'.");
                return;
            }
            prop.objectReferenceValue = value;
        }

        static Type FindType(string fullName)
        {
            // the project's own assemblies first, then any Unity object type Unity knows about (no AppDomain scan: it can see unloaded assemblies)
            foreach (var asm in new[] { "Assembly-CSharp", "Assembly-CSharp-Editor" })
            {
                Type t = Type.GetType(fullName + ", " + asm, false);
                if (t != null) return t;
            }
            foreach (var t in TypeCache.GetTypesDerivedFrom<UnityEngine.Object>()) if (t.FullName == fullName) return t;
            return null;
        }

        static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    if (!scenes[i].enabled) { scenes[i].enabled = true; EditorBuildSettings.scenes = scenes.ToArray(); }
                    return;
                }
            }
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        // ───────────── Save ─────────────
        [MenuItem("Hearth & Hollow/Delete Save", false, 20)]
        public static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete Save", "Delete the Hearth & Hollow save file?\n" + SaveService.SavePath, "Delete", "Cancel")) return;
            SaveService.Delete();
            Debug.Log("[Hearth & Hollow] Save deleted: " + SaveService.SavePath);
        }
    }
}
#endif
