// Hearth & Hollow — a tile that picks its sprite from a set (Cainos art pack).
//   • pattern mode (patternWidth > 0): sprites are a W×H block (row by row from the top-left) laid down in a repeating grid,
//     e.g. the big 3×3 stone slabs;
//   • random mode: a stable random pick per cell (the same cell always gets the same sprite), so grass varies without seams.
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HearthHollow.Dungeon
{
    [CreateAssetMenu(menuName = "Hearth & Hollow/Art Tile", fileName = "Art Tile")]
    public class ArtTile : TileBase
    {
        public Sprite[] sprites = new Sprite[0];
        [Tooltip("Random mode: chance weights per sprite (empty = equal).")] public float[] weights = new float[0];
        [Tooltip("Pattern mode: sprites form a block this many cells wide (0 = random mode).")] public int patternWidth;
        public int patternHeight;
        public Color color = Color.white;
        public Tile.ColliderType collider = Tile.ColliderType.None;
        [Tooltip("Random mode: changes which cells get which sprite.")] public int seed = 1;
        [Tooltip("Moves the sprite within its cell (e.g. -0.5 y to stand a bush on the bottom of the cell).")] public Vector2 offset;

        public override void GetTileData(Vector3Int p, ITilemap tilemap, ref TileData data)
        {
            data.sprite = Pick(p);
            data.color = color;
            data.colliderType = collider;
            data.flags = TileFlags.LockColor | TileFlags.LockTransform;
            data.transform = offset == Vector2.zero ? Matrix4x4.identity : Matrix4x4.Translate(new Vector3(offset.x, offset.y, 0f));
        }

        public Sprite Pick(Vector3Int p)
        {
            if (sprites == null || sprites.Length == 0) return null;
            if (patternWidth > 0 && patternHeight > 0)
            {
                int cx = Mod(p.x, patternWidth), cy = patternHeight - 1 - Mod(p.y, patternHeight);
                int i = cy * patternWidth + cx;
                return i < sprites.Length ? sprites[i] : sprites[0];
            }
            uint h = (uint)(p.x * 73856093) ^ (uint)(p.y * 19349663) ^ (uint)(seed * 83492791);
            h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15;
            float r = (h % 10000) / 10000f;
            if (weights == null || weights.Length != sprites.Length) return sprites[(int)(r * sprites.Length) % sprites.Length];
            float total = 0f; foreach (var w in weights) total += w;
            float t = r * total;
            for (int i = 0; i < sprites.Length; i++) { t -= weights[i]; if (t <= 0f) return sprites[i]; }
            return sprites[sprites.Length - 1];
        }

        static int Mod(int a, int m) { int r = a % m; return r < 0 ? r + m : r; }
    }
}
