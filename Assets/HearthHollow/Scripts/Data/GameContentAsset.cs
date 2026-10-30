// Hearth & Hollow — ScriptableObject wrapper so all designer content is an editable asset.
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Data
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Game Content", fileName = "GameContent")]
    public class GameContentAsset : ScriptableObject
    {
        public GameContent content;

        [ContextMenu("Reset To Defaults")]
        public void ResetToDefaults()
        {
            content = DefaultContent.Create();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        // Called by Unity when the asset is first created or "Reset" is picked in the inspector.
        void Reset()
        {
            content = DefaultContent.Create();
        }

        /// Content to play with: the asset's data, or defaults if the asset is empty.
        public GameContent GetContentOrDefault()
        {
            if (content == null || content.recipes == null || content.recipes.Count == 0)
                return DefaultContent.Create();
            // older assets gain the moves, skill trees and level-50 tuning (kept, so you can edit them here)
            if (DefaultContent.Upgrade(content))
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            return content;
        }
    }
}
