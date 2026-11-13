// Hearth & Hollow — setting up Light2Ds for the dungeon and the town: normal maps, shadows, which sorting layers
// a light reaches. URP keeps these settings private, so some are set by reflection.
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public static class LightSetup
    {
        /// How high lights sit above the ground for normal mapping (higher = flatter, softer shading).
        public static float LightHeight = 2.4f;

        /// Every light reads the sprites' normal maps (Hearth & Hollow > Generate Normal Maps), so it wraps round shapes.
        public static void UseNormals(Light2D light)
        {
            if (light == null || light.lightType == Light2D.LightType.Global) return;
            var q = typeof(Light2D).GetField("m_NormalMapQuality", BindingFlags.NonPublic | BindingFlags.Instance);
            var d = typeof(Light2D).GetField("m_NormalMapDistance", BindingFlags.NonPublic | BindingFlags.Instance);
            if (q != null) q.SetValue(light, System.Enum.Parse(q.FieldType, "Accurate"));
            if (d != null) d.SetValue(light, LightHeight);
        }

        /// This light is blocked by walls and pillars (their ShadowCaster2D). Use on a few big lights, not every spark.
        public static void CastShadows(Light2D light, float intensity = 0.85f, float softness = 0.35f)
        {
            if (light == null) return;
            light.shadowsEnabled = true;
            light.shadowIntensity = intensity;
            light.shadowSoftness = softness;
            if (light.GetComponent<ShadowSafeLight>() == null) light.gameObject.AddComponent<ShadowSafeLight>();   // stays lit if its centre ends up inside a wall or rock
        }

        /// Walls and pillars block light: a shadow caster shaped by the walls tilemap's collider. Shadows fall on the
        /// ground and on characters, not on the walls themselves (they keep their wall glow).
        public static void AddWallShadows(UnityEngine.Tilemaps.Tilemap walls)
        {
            if (walls == null) return;
            var col = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
            if (col == null || walls.GetComponent<ShadowCaster2D>() != null) return;
            var sc = walls.gameObject.AddComponent<ShadowCaster2D>();
            var bf = BindingFlags.NonPublic | BindingFlags.Instance;
            var src = typeof(ShadowCaster2D).GetField("m_ShadowCastingSource", bf);
            var comp = typeof(ShadowCaster2D).GetField("m_ShadowShape2DComponent", bf);
            if (src != null) src.SetValue(sc, System.Enum.Parse(src.FieldType, "ShapeProvider"));
            if (comp != null) comp.SetValue(sc, col);
            sc.castingOption = ShadowCaster2D.ShadowCastingOptions.CastShadow;
            var layers = typeof(ShadowCaster2D).GetField("m_ApplyToSortingLayers", bf);
            if (layers != null)
            {
                var ids = new List<int>();
                foreach (var n in new[] { "Ground", "Default" }) { int id = SortingLayer.NameToID(n); if (SortingLayer.IsValid(id)) ids.Add(id); }
                layers.SetValue(sc, ids.ToArray());
            }
            sc.enabled = false; sc.enabled = true;   // rebuild with the new source
        }

        /// Lights only the named sorting layers (e.g. "Walls" for a glow that reaches walls but not the floor).
        public static void ApplyToLayers(Light2D light, params string[] names)
        {
            UseNormals(light);
            var f = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return;
            var ids = new List<int>();
            foreach (var n in names) { int id = SortingLayer.NameToID(n); if (SortingLayer.IsValid(id)) ids.Add(id); }
            f.SetValue(light, ids.ToArray());
        }

        public static void ApplyToAllLayers(Light2D light)
        {
            UseNormals(light);
            var f = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null) return;
            var layers = SortingLayer.layers;
            var ids = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++) ids[i] = layers[i].id;
            f.SetValue(light, ids);
        }
    }
}
