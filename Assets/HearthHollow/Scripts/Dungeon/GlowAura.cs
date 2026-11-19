// Hearth & Hollow — a character glows: a halo in the shape of their sprite, bright and tight at the edge and fading
// softly outwards (stacked silhouettes, each a little bigger and fainter, so there's no hard edge), breathing gently,
// with little bolts of lightning crackling off it. Put on the character's Body (the object with the animated
// SpriteRenderer); it copies the frame and facing every frame, so it follows every animation.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class GlowAura : MonoBehaviour
    {
        public Color Inner = new Color(0.9f, 1f, 1f);         // the bright rim right at the edge
        public Color Outer = new Color(0.35f, 0.75f, 1f);     // the soft glow it fades out to
        public float Strength = 1f;                           // overall brightness
        public float Spread = 0.42f;                          // how far the glow reaches (× the sprite's size)
        public float CracklesPerSecond = 4f;                  // little bolts off the body
        public float Fill;                                    // 0 = just the halo; up to 1 = the whole body glows white-hot too

        SpriteRenderer _src;
        SpriteRenderer[] _layers; SpriteRenderer _haze, _fill;
        float _until, _t, _fadeIn, _crackleT;
        const int Layers = 8;

        /// The aura on this sprite (made the first time), glowing for <paramref name="seconds"/> (again: it's extended).
        public static GlowAura On(SpriteRenderer src, float seconds)
        {
            var a = src.GetComponent<GlowAura>();
            if (a == null) { a = src.gameObject.AddComponent<GlowAura>(); a.Make(src); }
            if (Time.time >= a._until) a._fadeIn = 0f;
            a._until = Mathf.Max(a._until, Time.time + seconds);
            a.enabled = true;
            return a;
        }

        public bool Active { get { return Time.time < _until; } }

        void Make(SpriteRenderer src)
        {
            _src = src;
            _layers = new SpriteRenderer[Layers];
            for (int i = 0; i < Layers; i++)
            {
                var go = new GameObject("Aura " + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (FxMaterials.Flat != null) sr.sharedMaterial = FxMaterials.Flat;   // a solid silhouette in its colour, HDR-bright
                _layers[i] = sr;
            }
            // the body itself lit up: a silhouette over the sprite in the rim colour (only shows when Fill > 0)
            var f = new GameObject("Aura fill"); f.transform.SetParent(transform, false);
            _fill = f.AddComponent<SpriteRenderer>();
            if (FxMaterials.Flat != null) _fill.sharedMaterial = FxMaterials.Flat;
            // and a big soft haze behind it all, so the glow melts into the air
            var h = new GameObject("Aura haze"); h.transform.SetParent(transform, false);
            _haze = h.AddComponent<SpriteRenderer>(); _haze.sprite = DungeonFx.Soft;
            if (FxMaterials.Glow != null) _haze.sharedMaterial = FxMaterials.Glow;
        }

        void LateUpdate()
        {
            if (_src == null) { Destroy(this); return; }
            float left = _until - Time.time;
            if (left <= -0.01f) { foreach (var l in _layers) l.enabled = false; _haze.enabled = false; _fill.enabled = false; enabled = false; return; }
            _t += Time.deltaTime;
            _fadeIn = Mathf.Min(1f, _fadeIn + Time.deltaTime / 0.15f);
            float fade = _fadeIn * Mathf.Clamp01(left / 0.4f);                    // eases in, and out over the last 0.4 s
            float breathe = 1f + 0.12f * Mathf.Sin(_t * 9f) + 0.05f * Mathf.Sin(_t * 23f);

            for (int i = 0; i < Layers; i++)
            {
                var l = _layers[i];
                float k = (i + 1f) / Layers;                                       // 0 → tight to the body … 1 → the outermost
                l.enabled = _src.enabled && _src.sprite != null;
                l.sprite = _src.sprite; l.flipX = _src.flipX; l.flipY = _src.flipY;
                l.sortingLayerID = _src.sortingLayerID; l.sortingOrder = _src.sortingOrder - 1;   // behind the sprite: it stays crisp
                float s = 1f + Spread * k * breathe;
                l.transform.localScale = new Vector3(s, s, 1f);
                Vector3 mid = _src.sprite != null ? _src.sprite.bounds.center : Vector3.zero;   // grow round the figure's middle, not its pivot
                if (_src.flipX) mid.x = -mid.x;
                l.transform.localPosition = mid * (1f - s);
                Color c = Color.Lerp(Inner, Outer, Mathf.SmoothStep(0f, 1f, k));
                c.a = (Mathf.Pow(1f - k, 1.5f) * 0.9f + 0.05f) * Strength * fade;   // bright at the edge, fading softly out
                l.color = c;
            }
            _fill.enabled = Fill > 0f && _src.enabled && _src.sprite != null;
            if (_fill.enabled)
            {
                _fill.sprite = _src.sprite; _fill.flipX = _src.flipX; _fill.flipY = _src.flipY;
                _fill.sortingLayerID = _src.sortingLayerID; _fill.sortingOrder = _src.sortingOrder + 1;   // over the sprite: the body becomes light
                Color fc = Inner; fc.a = Mathf.Clamp01(Fill * (0.88f + 0.12f * Mathf.Sin(_t * 13f))) * fade;
                _fill.color = fc;
            }
            if (_src.sprite != null)
            {
                var hb = _src.sprite.bounds;
                Vector3 hm = hb.center; if (_src.flipX) hm.x = -hm.x;
                _haze.enabled = _src.enabled;
                _haze.transform.localPosition = hm;
                float hs = Mathf.Max(hb.size.x, hb.size.y) * (1.3f + Spread) * breathe / Mathf.Max(0.01f, _haze.sprite.bounds.size.x);
                _haze.transform.localScale = new Vector3(hs, hs, 1f);
                Color hc = Outer; hc.a = 0.28f * Strength * fade;
                _haze.color = hc;
                _haze.sortingLayerID = _src.sortingLayerID; _haze.sortingOrder = _src.sortingOrder - 2;
            }

            if (CracklesPerSecond <= 0f || fade < 0.5f) return;
            _crackleT -= Time.deltaTime;
            if (_crackleT > 0f) return;
            _crackleT = Random.Range(0.5f, 1.5f) / CracklesPerSecond;
            var b = _src.bounds;
            Vector2 from = (Vector2)b.center + Vector2.Scale(Random.insideUnitCircle, (Vector2)b.extents * 0.8f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            Bolt(from, from + dir * Random.Range(0.35f, 0.7f), Color.Lerp(Inner, Outer, 0.3f), _src.sortingOrder + 2);
        }

        void OnDisable() { if (_layers != null) foreach (var l in _layers) if (l != null) l.enabled = false; if (_haze != null) _haze.enabled = false; if (_fill != null) _fill.enabled = false; }

        // ───────── lightning: a jagged line of a few bright segments that flickers out ─────────
        public static void Bolt(Vector2 from, Vector2 to, Color c, int order = 20470, float width = 0.06f, float life = 0.12f)
        {
            const int Segs = 5;
            Vector2 d = to - from; float len = d.magnitude;
            if (len < 0.01f) return;
            Vector2 n = new Vector2(-d.y, d.x) / len;
            Vector2 prev = from;
            for (int i = 1; i <= Segs; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)Segs);
                if (i < Segs) p += n * Random.Range(-0.18f, 0.18f) * len;   // jagged, but the ends stay put
                Segment(prev, p, c, width * Mathf.Lerp(1.2f, 0.6f, i / (float)Segs), order, life);
                prev = p;
            }
        }

        static void Segment(Vector2 a, Vector2 b, Color c, float width, int order, float life)
        {
            Vector2 d = b - a;
            var go = new GameObject("fx_bolt");
            go.transform.position = (a + b) * 0.5f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            go.transform.localScale = new Vector3(d.magnitude * DungeonFx.PPU, width * DungeonFx.PPU, 1f);   // the Pixel sprite is one art pixel
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DungeonFx.Pixel; sr.color = c; sr.sortingOrder = order;
            if (FxMaterials.Glow != null) sr.sharedMaterial = FxMaterials.Glow;
            go.AddComponent<BoltFade>().Life = life;
        }

        class BoltFade : MonoBehaviour
        {
            public float Life = 0.12f; float _t; SpriteRenderer _sr; Color _c;
            void Start() { _sr = GetComponent<SpriteRenderer>(); _c = _sr.color; }
            void Update()
            {
                _t += Time.deltaTime;
                float k = _t / Life;
                if (k >= 1f) { Destroy(gameObject); return; }
                var c = _c; c.a *= (1f - k) * (Random.value < 0.25f ? 0.4f : 1f);   // flickers as it dies
                _sr.color = c;
            }
        }
    }
}
