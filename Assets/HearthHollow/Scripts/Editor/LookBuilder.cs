// Hearth & Hollow — "Hearth & Hollow > Set Up HD-2D Look" (light & look plan, step 1).
//   • Resources/HHFx/Glow Sprite + Glow Flat materials (Rendering/GlowSprite.shader) for glowing effects,
//   • Rendering/Tilt Shift material + a "Tilt Shift" Full Screen Pass renderer feature on Settings/Renderer2D,
//   • a colour grade per dungeon theme (DungeonTheme.grade*). Bloom, vignette etc. are made at runtime by GameLook.
// Safe to run again (grades are overwritten with these defaults — tweak them in each theme afterwards if you like).
using System;
using System.Collections.Generic;
using System.IO;
using HearthHollow.Dungeon;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.EditorTools
{
    public static class LookBuilder
    {
        const string Rendering = "Assets/HearthHollow/Rendering";
        const string FxRes = "Assets/HearthHollow/Resources/HHFx";
        const string RendererPath = "Assets/Settings/Renderer2D.asset";
        const string SettingsPath = "Assets/HearthHollow/Dungeon/DungeonSettings.asset";

        // temperature, tint, saturation, contrast, exposure, vignette, bloom
        static readonly Dictionary<string, float[]> Grades = new Dictionary<string, float[]>
        {
            { "meadow",      new[] {   8f,   0f,  12f, 10f,  0.00f, 0.24f, 0.7f } },
            { "berry",       new[] {   4f,   9f,  12f,  8f,  0.00f, 0.26f, 0.8f } },
            { "salt",        new[] { -12f,  -2f,   0f,  8f,  0.05f, 0.24f, 0.7f } },
            { "cavern",      new[] { -18f,   0f, -12f, 16f, -0.10f, 0.36f, 1.1f } },
            { "chili",       new[] {  20f,   0f,  14f, 10f,  0.00f, 0.26f, 0.9f } },
            { "ember",       new[] {  28f,   4f,  10f, 18f, -0.05f, 0.34f, 1.3f } },
            { "frost",       new[] { -26f,   0f,  -6f, 10f,  0.05f, 0.26f, 0.8f } },
            { "heart",       new[] {  -6f,  18f,   6f, 18f, -0.10f, 0.38f, 1.2f } },
            { "firefly",     new[] {  -8f, -10f,  14f, 10f, -0.05f, 0.32f, 1.2f } },
            { "spore",       new[] {   0f, -14f,   6f,  8f,  0.00f, 0.28f, 0.9f } },
            { "watchtower",  new[] {   4f,   0f, -12f, 14f,  0.00f, 0.28f, 0.8f } },
            { "canopy",      new[] {   6f,  -8f,  12f,  8f,  0.00f, 0.28f, 0.8f } },
            { "larder",      new[] {  18f,   2f,  -4f, 14f, -0.05f, 0.32f, 1.0f } },
            { "frosthollow", new[] { -32f,   0f,  -8f, 16f, -0.05f, 0.34f, 1.1f } },
            { "harerun",     new[] {  10f,  -2f,  16f,  6f,  0.05f, 0.22f, 0.7f } },
            { "glowcap",     new[] { -18f,  -6f,  12f, 16f, -0.10f, 0.38f, 1.4f } },
            { "thunder",     new[] { -14f,   4f,  -6f, 16f,  0.00f, 0.30f, 0.9f } },
            { "hive",        new[] {  24f,   0f,  14f,  8f,  0.00f, 0.26f, 0.9f } },
        };

        // each route's own light sources (RoomLights, step 4)
        static readonly Dictionary<string, KeyValuePair<RoomGlow, Color>> Ambient = new Dictionary<string, KeyValuePair<RoomGlow, Color>>
        {
            { "meadow", G(RoomGlow.Fireflies, 0.85f, 1f, 0.45f) }, { "berry", G(RoomGlow.Fireflies, 1f, 0.6f, 0.85f) }, { "salt", G(RoomGlow.Fireflies, 0.6f, 1f, 0.95f) },
            { "firefly", G(RoomGlow.Fireflies, 0.7f, 1f, 0.4f) }, { "canopy", G(RoomGlow.Fireflies, 0.85f, 1f, 0.5f) }, { "harerun", G(RoomGlow.Fireflies, 1f, 0.95f, 0.55f) },
            { "thunder", G(RoomGlow.Fireflies, 0.6f, 0.8f, 1f) }, { "hive", G(RoomGlow.Fireflies, 1f, 0.8f, 0.3f) },
            { "cavern", G(RoomGlow.Mushrooms, 0.35f, 0.9f, 1f) }, { "glowcap", G(RoomGlow.Mushrooms, 0.3f, 1f, 0.9f) }, { "spore", G(RoomGlow.Mushrooms, 0.55f, 1f, 0.35f) },
            { "heart", G(RoomGlow.Mushrooms, 0.75f, 0.45f, 1f) }, { "frost", G(RoomGlow.Crystals, 0.6f, 0.85f, 1f) }, { "frosthollow", G(RoomGlow.Crystals, 0.55f, 0.8f, 1f) },
            { "ember", G(RoomGlow.LavaCracks, 1f, 0.4f, 0.12f) }, { "chili", G(RoomGlow.LavaCracks, 1f, 0.45f, 0.15f) },
        };
        static KeyValuePair<RoomGlow, Color> G(RoomGlow g, float r, float gg, float b) { return new KeyValuePair<RoomGlow, Color>(g, new Color(r, gg, b)); }

        [MenuItem("Hearth & Hollow/Set Up HD-2D Look", priority = 5)]
        public static void Apply()
        {
            Directory.CreateDirectory(FxRes);
            AssetDatabase.Refresh();
            var glowShader = Shader.Find("HearthHollow/Glow Sprite");
            var tiltShader = Shader.Find("HearthHollow/Tilt Shift");
            if (glowShader == null || tiltShader == null) { Debug.LogError("[Hearth & Hollow] HD-2D shaders not found (Rendering/GlowSprite.shader, TiltShift.shader)."); return; }

            var glow = Mat(FxRes + "/Glow Sprite.mat", glowShader); glow.SetFloat("_Glow", 2.3f); glow.SetFloat("_Flat", 0f);
            var flat = Mat(FxRes + "/Glow Flat.mat", glowShader); flat.SetFloat("_Glow", 1.6f); flat.SetFloat("_Flat", 1f);
            var tilt = Mat(Rendering + "/Tilt Shift.mat", tiltShader);
            var rimShader = Shader.Find("HearthHollow/Rim Sprite");
            if (rimShader != null) Mat(FxRes + "/Rim Sprite.mat", rimShader);   // monster silhouettes (EnemyGlow)
            AddTiltFeature(tilt);

            var settings = AssetDatabase.LoadAssetAtPath<DungeonSettings>(SettingsPath);
            int n = 0;
            if (settings != null)
                foreach (var t in settings.themes)
                {
                    float[] g;
                    if (t == null || !Grades.TryGetValue(t.routeId, out g)) continue;
                    t.gradeTemperature = g[0]; t.gradeTint = g[1]; t.gradeSaturation = g[2]; t.gradeContrast = g[3];
                    t.gradeExposure = g[4]; t.gradeVignette = g[5] + 0.08f; t.gradeBloom = g[6] * 1.6f;   // darker edges + bigger glow (31 Oct) t.gradeFilter = Color.white;
                    KeyValuePair<RoomGlow, Color> amb;
                    if (Ambient.TryGetValue(t.routeId, out amb)) { t.ambient = amb.Key; t.ambientColor = amb.Value; } else t.ambient = RoomGlow.None;
                    EditorUtility.SetDirty(t); n++;
                }
            AssetDatabase.SaveAssets();
            Debug.Log("[Hearth & Hollow] HD-2D look set up: glow materials, tilt-shift pass, " + n + " theme grades.");
        }

        static Material Mat(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void AddTiltFeature(Material tilt)
        {
            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (data == null) { Debug.LogError("[Hearth & Hollow] No renderer at " + RendererPath); return; }
            Type type = null;
            foreach (var tp in TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>()) if (tp.Name == "FullScreenPassRendererFeature") { type = tp; break; }
            if (type == null) { Debug.LogError("[Hearth & Hollow] FullScreenPassRendererFeature not found in this URP version."); return; }

            ScriptableRendererFeature feature = null;
            foreach (var f in data.rendererFeatures) if (f != null && f.name == "Tilt Shift") feature = f;
            bool isNew = feature == null;
            if (isNew)
            {
                feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(type);
                feature.name = "Tilt Shift";
                AssetDatabase.AddObjectToAsset(feature, data);
            }
            Set(feature, "passMaterial", tilt);
            Set(feature, "passIndex", 0);
            Set(feature, "fetchColorBuffer", true);
            var inj = type.GetField("injectionPoint");
            if (inj != null) inj.SetValue(feature, Enum.Parse(inj.FieldType, "AfterRenderingPostProcessing"));
            if (isNew)
            {
                data.rendererFeatures.Add(feature);
                string guid; long localId;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out guid, out localId);
                var so = new SerializedObject(data);
                var map = so.FindProperty("m_RendererFeatureMap");
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
        }

        static void Set(object o, string field, object value)
        {
            var f = o.GetType().GetField(field);
            if (f != null) f.SetValue(o, value);
            else Debug.LogWarning("[Hearth & Hollow] FullScreenPassRendererFeature has no field " + field);
        }
    }
}
