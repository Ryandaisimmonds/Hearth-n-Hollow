// Hearth & Hollow — the effect materials, loaded once (Resources/HHFx, made by "Hearth & Hollow > Set Up HD-2D Look").
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public static class FxMaterials
    {
        static Material _glow, _flat, _rim, _unlit;

        /// Additive HDR sprite: effects that bloom.
        public static Material Glow { get { return Load(ref _glow, "HHFx/Glow Sprite"); } }
        /// Additive solid silhouette: hit flashes and after-images.
        public static Material Flat { get { return Load(ref _flat, "HHFx/Glow Flat"); } }
        /// A coloured rim around a monster (EnemyGlow).
        public static Material Rim { get { return Load(ref _rim, "HHFx/Rim Sprite"); } }
        /// Ignores the room's lights: readable in the dark (boss markers, the fall's fog).
        public static Material Unlit { get { return Load(ref _unlit, "HHFx/Unlit Sprite"); } }

        static Material Load(ref Material m, string path)
        {
            if (m == null) m = Resources.Load<Material>(path);
            return m;
        }
    }
}
