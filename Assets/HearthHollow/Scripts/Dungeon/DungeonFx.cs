// Hearth & Hollow — small generated sprites + one-shot effects for the dungeon (slashes, rings, puffs,
// arrows, orbs). Everything is made in code so there are no extra assets to manage; swap in real VFX later.
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public static class DungeonFx
    {
        public const float PPU = 32f;
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        static Sprite Make(string key, int w, int h, System.Func<int, int, Color> px, Vector2 pivot)
        {
            Sprite s;
            if (_cache.TryGetValue(key, out s) && s != null) return s;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "fx_" + key };
            var cols = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) cols[y * w + x] = px(x, y);
            tex.SetPixels(cols); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, PPU, 0, SpriteMeshType.FullRect);
            s.name = key;
            _cache[key] = s;
            return s;
        }

        public static Sprite Pixel { get { return Make("pixel", 1, 1, (x, y) => Color.white, new Vector2(0.5f, 0.5f)); } }
        /// 1×1 white with a left pivot — for bars that fill from the left.
        public static Sprite BarPixel { get { return Make("barpixel", 1, 1, (x, y) => Color.white, new Vector2(0f, 0.5f)); } }

        public static Sprite Circle
        {
            get
            {
                return Make("circle", 16, 16, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(8, 8));
                    return d <= 7.5f ? Color.white : Color.clear;
                }, new Vector2(0.5f, 0.5f));
            }
        }

        public static Sprite Orb
        {
            get
            {
                return Make("orb", 12, 12, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(6, 6));
                    if (d > 5.5f) return Color.clear;
                    float a = d < 2.5f ? 1f : 0.85f;
                    return new Color(1, 1, 1, a) * (d < 2.5f ? 1f : 0.8f);
                }, new Vector2(0.5f, 0.5f));
            }
        }

        public static Sprite Ring
        {
            get
            {
                return Make("ring", 64, 64, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32));
                    return d <= 31.5f && d >= 28f ? Color.white : Color.clear;
                }, new Vector2(0.5f, 0.5f));
            }
        }

        /// A crescent pointing right (+x); rotate to face the swing.
        public static Sprite Slash
        {
            get
            {
                return Make("slash", 32, 32, (x, y) =>
                {
                    var p = new Vector2(x + 0.5f - 8f, y + 0.5f - 16f);
                    float d = p.magnitude;
                    float ang = Mathf.Abs(Mathf.Atan2(p.y, p.x)) * Mathf.Rad2Deg;
                    if (ang > 70f || d > 22f || d < 15f) return Color.clear;
                    float a = 1f - ang / 70f;
                    return new Color(1, 1, 1, Mathf.Clamp01(a * 1.4f));
                }, new Vector2(0.25f, 0.5f));
            }
        }

        /// An arrow pointing right (+x).
        public static Sprite Arrow
        {
            get
            {
                return Make("arrow", 16, 5, (x, y) =>
                {
                    if (y == 2 && x >= 1 && x <= 12) return new Color(0.78f, 0.62f, 0.4f);
                    if (x >= 12 && Mathf.Abs(y - 2) <= 15 - x) return new Color(0.85f, 0.88f, 0.92f);
                    if (x <= 2 && (y == 1 || y == 3)) return new Color(0.95f, 0.95f, 0.95f);
                    return Color.clear;
                }, new Vector2(0.5f, 0.5f));
            }
        }

        // ───────────── one-shot effects ─────────────
        public static FxSprite Spawn(Sprite sprite, Vector3 pos, Color color, float life, float startScale, float endScale, float rotation = 0f, int order = 20500)
        {
            var go = new GameObject("fx_" + (sprite != null ? sprite.name : "x"));
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, rotation);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            var fx = go.AddComponent<FxSprite>();
            fx.Init(life, startScale, endScale, color);
            return fx;
        }

        public static void SlashAt(Vector3 pos, Vector2 dir, Color color, float size)
        {
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Spawn(Slash, pos, color, 0.16f, size, size * 1.15f, rot);
        }

        public static void RingAt(Vector3 pos, Color color, float radius, float life = 0.35f)
        {
            // ring sprite is 64 px = 2 units across at scale 1 → scale = radius
            Spawn(Ring, pos, color, life, radius * 0.3f, radius, 0f);
        }

        public static void Puff(Vector3 pos, Color color, int count = 7, float spread = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                var fx = Spawn(Circle, pos + (Vector3)(Random.insideUnitCircle * 0.2f), color, Random.Range(0.25f, 0.45f), Random.Range(0.25f, 0.45f), 0.05f);
                fx.velocity = Random.insideUnitCircle.normalized * Random.Range(spread, spread * 2.5f);
            }
        }

        public static void Spark(Vector3 pos, Color color) { Spawn(Circle, pos, color, 0.12f, 0.5f, 0.1f); }
    }

    /// Fades / scales / drifts a sprite, then destroys it. Runs on scaled time (pauses with the game).
    public class FxSprite : MonoBehaviour
    {
        public Vector2 velocity;
        float _life, _t, _s0, _s1; Color _c; SpriteRenderer _sr;

        public void Init(float life, float s0, float s1, Color c)
        {
            _life = Mathf.Max(0.01f, life); _s0 = s0; _s1 = s1; _c = c;
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * s0;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            transform.localScale = Vector3.one * Mathf.Lerp(_s0, _s1, k);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 1f - Mathf.Clamp01(Time.deltaTime * 6f);
            if (_sr != null) _sr.color = new Color(_c.r, _c.g, _c.b, _c.a * (1f - k));
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
