// Hearth & Hollow — how lit is a point? (light plan step 2)
// A cheap estimate from the scene's Light2Ds (refreshed once a frame by DungeonController): global lights plus point
// lights fading from their inner to outer radius. Wall-only lights are skipped. Used for silhouettes now, and later for
// "lit = weak spot", light-shy monsters and secrets.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public static class DungeonLight
    {
        struct L { public Vector2 pos; public float inner, outer, intensity, falloff; public bool source; }
        static readonly List<L> _points = new List<L>();
        static float _global;
        static int _frame = -1;

        public static void Refresh()
        {
            if (_frame == Time.frameCount) return;
            _frame = Time.frameCount;
            _points.Clear(); _global = 0f;
            foreach (var l in Object.FindObjectsByType<Light2D>())
            {
                if (l == null || !l.isActiveAndEnabled || l.intensity <= 0.001f) continue;
                if (l.name.IndexOf("wall", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;   // wall-only glows
                float lum = Mathf.Max(l.color.r, Mathf.Max(l.color.g, l.color.b));
                if (l.lightType == Light2D.LightType.Global) { _global = Mathf.Max(_global, l.intensity * lum); continue; }
                if (l.lightType != Light2D.LightType.Point) continue;
                // a "source" = something monsters react to: torches, braziers, Flare, fire — not your carried light or a monster's own lantern
                bool source = l.name != "Player Light" && l.GetComponentInParent<EnemyController>() == null;
                _points.Add(new L { pos = l.transform.position, inner = l.pointLightInnerRadius, outer = Mathf.Max(0.01f, l.pointLightOuterRadius), intensity = l.intensity * lum, falloff = l.falloffIntensity, source = source });
            }
        }

        /// The strongest light source (torch, brazier, Flare, fire — not your carried light) whose light reaches
        /// <paramref name="p"/> with at least <paramref name="min"/> strength. Light-shy monsters back away from it, drawn ones go to it.
        public static bool StrongestSource(Vector2 p, float min, out Vector2 at, out float strength)
        {
            Refresh();
            at = p; strength = 0f; bool found = false;
            foreach (var l in _points)
            {
                if (!l.source || l.intensity < 0.6f) continue;
                float d = Vector2.Distance(p, l.pos);
                if (d >= l.outer) continue;
                float k = d <= l.inner ? 1f : 1f - (d - l.inner) / Mathf.Max(0.01f, l.outer - l.inner);
                float s = l.intensity * k;
                if (s > strength && s >= min) { strength = s; at = l.pos; found = true; }
            }
            return found;
        }

        /// Roughly how much light reaches <paramref name="p"/> (0 = black, ~1 = well lit).
        public static float At(Vector2 p)
        {
            Refresh();
            float sum = _global;
            foreach (var l in _points)
            {
                float d = Vector2.Distance(p, l.pos);
                if (d >= l.outer) continue;
                float k = d <= l.inner ? 1f : 1f - (d - l.inner) / Mathf.Max(0.01f, l.outer - l.inner);
                sum += l.intensity * Mathf.Pow(k, 1f + l.falloff * 2f);
            }
            return sum;
        }
    }
}
