// Hearth & Hollow — small generated sprites + one-shot effects for the dungeon (slashes, rings, puffs,
// arrows, orbs). Everything is made in code so there are no extra assets to manage; swap in real VFX later.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

        /// A white pixel-art flame (pivot at the bottom), tinted at runtime: braziers, torches.
        public static Sprite Flame
        {
            get
            {
                return Make("flame", 16, 24, (x, y) =>
                {
                    float fy = y / 23f;
                    float half = fy < 0.35f ? Mathf.Lerp(5.5f, 7.5f, fy / 0.35f) : Mathf.Lerp(7.5f, 0.5f, (fy - 0.35f) / 0.65f);
                    half += fy > 0.4f ? Mathf.Sin(fy * 17f) * 0.8f : 0f;
                    return Mathf.Abs(x + 0.5f - 8f) <= half ? Color.white : Color.clear;
                }, new Vector2(0.5f, 0f));
            }
        }

        static int _floorLights;
        /// Fire left on the floor where a hit landed: a few glowing pixels that burn down over a few seconds (light only).
        public static void FloorEmbers(Vector3 pos, Color color, int count = 5)
        {
            float life = Random.Range(3f, 5f);
            for (int i = 0; i < count; i++)
            {
                var fx = Spawn(Pixel, pos + (Vector3)(Random.insideUnitCircle * 0.35f), Color.Lerp(color, new Color(1f, 0.45f, 0.15f), Random.value * 0.5f),
                               life * Random.Range(0.6f, 1f), Random.Range(2.5f, 4f), 1.5f, 0f, 6, true);   // order 6: on the ground, under characters
                fx.drag = 10f;
            }
            if (_floorLights < 12)
            {
                _floorLights++;
                var l = Flash(pos + new Vector3(0, 0.1f), color, 1.6f, 0.85f, life);
                l.gameObject.AddComponent<FloorLightCount>();
            }
        }

        class FloorLightCount : MonoBehaviour { void OnDestroy() { _floorLights = Mathf.Max(0, _floorLights - 1); } }

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

        // ───────────── glow materials (Resources/HHFx, made by "Hearth & Hollow > Set Up HD-2D Look") ─────────────
        static Material _glowMat, _flatMat;
        /// Additive HDR sprite: effects that bloom.
        public static Material GlowMat { get { if (_glowMat == null) _glowMat = Resources.Load<Material>("HHFx/Glow Sprite"); return _glowMat; } }
        /// Additive solid silhouette: hit flashes and after-images.
        public static Material FlatMat { get { if (_flatMat == null) _flatMat = Resources.Load<Material>("HHFx/Glow Flat"); return _flatMat; } }

        // ───────────── one-shot effects ─────────────
        public static FxSprite Spawn(Sprite sprite, Vector3 pos, Color color, float life, float startScale, float endScale, float rotation = 0f, int order = 20500, bool glow = false)
        {
            var go = new GameObject("fx_" + (sprite != null ? sprite.name : "x"));
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, rotation);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            if (glow && GlowMat != null) sr.sharedMaterial = GlowMat;
            var fx = go.AddComponent<FxSprite>();
            fx.Init(life, startScale, endScale, color);
            return fx;
        }

        public static void SlashAt(Vector3 pos, Vector2 dir, Color color, float size)
        {
            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Spawn(Slash, pos, color, 0.2f, size, size * 1.2f, rot, 20500, true);
            Flash(pos + (Vector3)(dir.normalized * 0.4f * size), color, 3f + size * 1.2f, 1.9f, 0.28f);   // every swing lights the room for a moment
            // embers flung off the arc of the swing
            Vector2 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
            for (int i = 0; i < 9; i++)
            {
                float a = Random.Range(-65f, 65f);
                Vector2 o = Quaternion.Euler(0, 0, a) * d;
                Throw(pos + (Vector3)(o * 0.55f * size), o, color, 1, 2.5f, 5.5f);
            }
        }

        public static void RingAt(Vector3 pos, Color color, float radius, float life = 0.35f)
        {
            // ring sprite is 64 px = 2 units across at scale 1 → scale = radius
            Spawn(Ring, pos, color, life, radius * 0.3f, radius, 0f, 20500, true);
            Flash(pos, color, radius * 2f + 1.5f, 2f, life * 1.6f);
            for (int i = 0; i < 14; i++) { Vector2 o = Random.insideUnitCircle.normalized; Throw(pos + (Vector3)(o * radius * 0.8f), o, color, 1, 1.5f, 4f); }
        }

        public static void Puff(Vector3 pos, Color color, int count = 7, float spread = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                var fx = Spawn(Circle, pos + (Vector3)(Random.insideUnitCircle * 0.2f), color, Random.Range(0.25f, 0.45f), Random.Range(0.25f, 0.45f), 0.05f);
                fx.velocity = Random.insideUnitCircle.normalized * Random.Range(spread, spread * 2.5f);
            }
        }

        public static void Spark(Vector3 pos, Color color) { Spawn(Circle, pos, color, 0.12f, 0.5f, 0.1f, 0f, 20500, true); }

        // ───────────── light & glow (light & look plan, step 1) ─────────────
        /// A point light that fades out over <paramref name="life"/> seconds (life ≤ 0 = stays; parent it to something).
        public static Light2D Flash(Vector3 pos, Color color, float radius, float intensity, float life, Transform follow = null)
        {
            var go = new GameObject("fx_light");
            go.transform.position = pos;
            if (follow != null) go.transform.SetParent(follow, true);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(color.r, color.g, color.b, 1f);
            l.intensity = intensity;
            l.pointLightOuterRadius = Mathf.Max(0.2f, radius);
            l.pointLightInnerRadius = radius * 0.15f;
            l.falloffIntensity = 0.35f;   // softer, more gradual edge
            DungeonController.ApplyToAllLayers(l);
            if (life > 0f) go.AddComponent<FxLight>().Init(life, intensity);
            if (radius >= 1.5f)
            {
                // walls catch it from further away than the floor
                var w = new GameObject("fx_wallglow");
                w.transform.SetParent(go.transform, false);
                var wl = w.AddComponent<Light2D>();
                wl.lightType = Light2D.LightType.Point;
                wl.color = l.color;
                wl.intensity = intensity * 0.6f;
                wl.pointLightOuterRadius = radius * 1.7f;
                wl.pointLightInnerRadius = radius * 0.2f;
                wl.falloffIntensity = 0.3f;
                DungeonController.ApplyToLayers(wl, "Walls");
                if (life > 0f) w.AddComponent<FxLight>().Init(life, wl.intensity);
            }
            return l;
        }

        /// Glowing chips flying out along <paramref name="dir"/> (±55°) — a hit landing.
        public static void Sparks(Vector3 pos, Vector2 dir, Color color, int count)
        {
            Vector2 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Random.insideUnitCircle.normalized;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = Quaternion.Euler(0, 0, Random.Range(-55f, 55f)) * d;
                float life = Random.Range(0.4f, 0.8f);
                var fx = Spawn(Pixel, pos + (Vector3)(Random.insideUnitCircle * 0.12f), color, life, Random.Range(4.5f, 7.5f), 1.2f, 0f, 20600, true);
                fx.velocity = a * Random.Range(5f, 11f);
                fx.drag = 3.2f;
                fx.accel = new Vector2(0f, -2.5f);   // arcs and falls like a real spark
                if (i < 2) Flash(fx.transform.position, color, 1.6f, 1.4f, life, fx.transform);   // a couple carry their own light
            }
        }

        /// Embers flung along <paramref name="dir"/> (fast at first, then drifting up and fading).
        public static void Throw(Vector3 pos, Vector2 dir, Color color, int count, float minSpeed, float maxSpeed)
        {
            Vector2 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Random.insideUnitCircle.normalized;
            Color warm = Color.Lerp(color, new Color(1f, 0.6f, 0.2f), 0.45f);   // everything burns a little orange
            for (int i = 0; i < count; i++)
            {
                Vector2 a = Quaternion.Euler(0, 0, Random.Range(-25f, 25f)) * d;
                var fx = Spawn(Pixel, pos, Random.value < 0.5f ? color : warm, Random.Range(0.6f, 1.3f), Random.Range(3f, 5.5f), 0.6f, 0f, 20600, true);
                fx.velocity = a * Random.Range(minSpeed, maxSpeed);
                fx.drag = 3.5f;
                fx.accel = new Vector2(0f, 0.9f);
            }
        }

        /// Embers drifting up and fading — fires, deaths, glowing projectiles.
        public static void Embers(Vector3 pos, Color color, int count, float spread = 0.35f)
        {
            for (int i = 0; i < count; i++)
            {
                var fx = Spawn(Pixel, pos + (Vector3)(Random.insideUnitCircle * spread), color, Random.Range(0.9f, 1.8f), Random.Range(3f, 5f), 0.6f, 0f, 20600, true);
                fx.velocity = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.4f, 1.3f));
                fx.drag = 0.8f;
                fx.accel = new Vector2(0f, 0.7f);
            }
        }

        /// A copy of a character's current frame: a flat glowing silhouette (hit flash, follows the character)
        /// or a fading after-image left behind (dashes).
        public static void Ghost(SpriteRenderer src, Color color, float life, bool flat, bool follow)
        {
            if (src == null || src.sprite == null) return;
            var go = new GameObject("fx_ghost");
            if (follow) go.transform.SetParent(src.transform, false);
            else { go.transform.position = src.transform.position; go.transform.rotation = src.transform.rotation; go.transform.localScale = src.transform.lossyScale; }
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite; sr.flipX = src.flipX; sr.flipY = src.flipY; sr.color = color;
            sr.sortingLayerID = src.sortingLayerID; sr.sortingOrder = src.sortingOrder + (follow ? 1 : -1);
            var m = flat ? FlatMat : GlowMat;
            if (m != null) sr.sharedMaterial = m;
            go.AddComponent<FxSprite>().InitKeep(life, color);
        }
    }

    /// Fades / scales / drifts a sprite, then destroys it. Runs on scaled time (pauses with the game).
    public class FxSprite : MonoBehaviour
    {
        public Vector2 velocity;
        public Vector2 accel;
        public float drag = 6f;
        float _life, _t, _s0, _s1; Color _c; SpriteRenderer _sr; bool _keep;

        public void Init(float life, float s0, float s1, Color c)
        {
            _life = Mathf.Max(0.01f, life); _s0 = s0; _s1 = s1; _c = c;
            _sr = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * s0;
        }

        /// Fade only — keeps the transform's scale as it is (ghosts).
        public void InitKeep(float life, Color c)
        {
            _life = Mathf.Max(0.01f, life); _c = c; _keep = true;
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            if (!_keep) transform.localScale = Vector3.one * Mathf.Lerp(_s0, _s1, k);
            velocity += accel * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= 1f - Mathf.Clamp01(Time.deltaTime * drag);
            if (_sr != null) _sr.color = new Color(_c.r, _c.g, _c.b, _c.a * (1f - k));
            if (_t >= _life) Destroy(gameObject);
        }
    }

    /// Fades a Light2D out, then destroys it. Scaled time (freezes with hit-stop and pause).
    public class FxLight : MonoBehaviour
    {
        float _life, _t, _i0; Light2D _l;
        public void Init(float life, float intensity) { _life = Mathf.Max(0.01f, life); _i0 = intensity; _l = GetComponent<Light2D>(); }
        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            if (_l != null) _l.intensity = _i0 * (1f - k) * (1f - k);
            if (_t >= _life) Destroy(gameObject);
        }
    }
}
