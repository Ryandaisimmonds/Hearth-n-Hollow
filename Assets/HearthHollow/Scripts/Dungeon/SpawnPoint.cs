// Hearth & Hollow — a marker inside a room prefab saying where something appears.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public enum SpawnKind { Enemy, Elite, Forage, Campfire, Ladder, Exit, PlayerStart }

    public class SpawnPoint : MonoBehaviour
    {
        public SpawnKind kind = SpawnKind.Enemy;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Color c;
            switch (kind)
            {
                case SpawnKind.Enemy: c = new Color(1f, 0.35f, 0.3f); break;
                case SpawnKind.Elite: c = new Color(1f, 0.8f, 0.2f); break;
                case SpawnKind.Forage: c = new Color(0.4f, 1f, 0.4f); break;
                case SpawnKind.Campfire: c = new Color(1f, 0.6f, 0.2f); break;
                case SpawnKind.Ladder: c = new Color(0.7f, 0.5f, 0.3f); break;
                case SpawnKind.Exit: c = new Color(0.5f, 0.7f, 1f); break;
                default: c = Color.white; break;
            }
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, 0.35f);
            Gizmos.DrawSphere(transform.position, 0.12f);
        }
#endif
    }
}
