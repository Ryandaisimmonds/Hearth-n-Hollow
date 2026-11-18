// Hearth & Hollow — the Effects Lab's straw training dummy: the look (a little pixel sprite) and the rock on its post.
// EnemyController.MakeDummy puts this on the monster's Body; every hit rocks it and sheds straw, like the town's dummies.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class TrainingDummy : MonoBehaviour
    {
        float _ang, _vel;

        public void Hit(Vector2 dir, bool hard, int damage)
        {
            float push = (hard ? 300f : 200f) + Mathf.Min(damage, 60) * 3f;
            _vel += (dir.x >= 0f ? -1f : 1f) * push;
            Vector2 at = (Vector2)transform.position + new Vector2(0, 0.75f);
            DungeonFx.Puff(at, new Color(0.92f, 0.82f, 0.45f, 0.75f), hard ? 7 : 4, 0.35f);   // straw
            DungeonFx.Sparks(at, Vector2.up, new Color(0.95f, 0.85f, 0.5f), hard ? 5 : 3);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _vel += (-90f * _ang - 7f * _vel) * dt;   // a damped spring back to upright
            _ang = Mathf.Clamp(_ang + _vel * dt, -35f, 35f);
            transform.localRotation = Quaternion.Euler(0, 0, _ang);   // the sprite's pivot is its foot
        }

        // ── the sprite: 18 × 38 art pixels, drawn top row first ──
        static readonly string[] Rows =
        {
            "......oooooo......",
            ".....ohhhhhho.....",
            "....ohhhhhhhho....",
            "....ohxhhhhxho....",
            "....ohhxhhxhho....",
            "....ohxhhhhxho....",
            "....ohhhhhhhho....",
            "....ohhhxxhhho....",
            ".....ohhhhhho.....",
            "......oossoo......",
            "oooooooossoooooooo",
            "owwwwwwwwwwwwwwwwo",
            "oooooosssssoooooso",
            "s....osSsSsso...s.",
            ".....osssSsso.....",
            "....osSssssSso....",
            "....orrrrrrrro....",
            "....osssSssssso...",
            "....osSsssSssso...",
            "....ossssSssso....",
            "....osSsssssso....",
            ".....ossSsSso.....",
            ".....osssssso.....",
            "......osssso......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            ".......owwo.......",
            "......oowwoo......",
            ".....owwwwwwo.....",
            "....oooooooooo....",
        };

        static Sprite _sprite;
        public static Sprite Sprite
        {
            get
            {
                if (_sprite != null) return _sprite;
                int h = Rows.Length, w = Rows[0].Length;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "fx_training_dummy" };
                var px = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        string row = Rows[h - 1 - y];
                        char ch = x < row.Length ? row[x] : '.';
                        px[y * w + x] = Colour(ch);
                    }
                tex.SetPixels(px); tex.Apply();
                _sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), DungeonFx.PPU);
                _sprite.name = "Training dummy";
                return _sprite;
            }
        }

        static Color Colour(char c)
        {
            switch (c)
            {
                case 'o': return new Color32(48, 32, 18, 255);     // ink
                case 'h': return new Color32(196, 160, 104, 255);  // burlap head
                case 'x': return new Color32(90, 56, 30, 255);     // stitches
                case 's': return new Color32(222, 188, 92, 255);   // straw
                case 'S': return new Color32(246, 222, 140, 255);  // straw, light
                case 'w': return new Color32(128, 84, 46, 255);    // wood
                case 'r': return new Color32(170, 58, 46, 255);    // red sash
                default: return Color.clear;
            }
        }
    }
}
