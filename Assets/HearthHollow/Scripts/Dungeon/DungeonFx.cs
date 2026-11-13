// Hearth & Hollow — small generated sprites + one-shot effects for the dungeon (slashes, rings, puffs,
// arrows, orbs). Everything is made in code so there are no extra assets to manage; swap in real VFX later.
using System.Collections.Generic;
using HearthHollow.Core;
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

        /// Ticket 41: little 12×12 pixel-art element icons (flame, snowflake, leaf, droplet, sparkle) for the marks over monsters.
        public static Sprite ElementIcon(Element e)
        {
            string[] rows; Color32 pa, pb, pc;
            switch (e)
            {
                case Element.Fire: rows = new[] { "......o.....", ".....oao....", ".....oaao...", "....oaaao.o.", "...oaabaaoao", "...oaabbaaao", "..oaabbbbaao", ".oaabbccbbao", ".oabbccccbao", ".oabcccccbao", "..oabbccbao.", "...oooooo..." }; pa = new Color32(232, 70, 30, 255); pb = new Color32(255, 150, 40, 255); pc = new Color32(255, 230, 120, 255); break;
                case Element.Frost: rows = new[] { ".....oo.....", ".oo.oabo.oo.", ".oaooabooao.", "..oaoaboao..", "..ooabbaoo..", "oaaabccbaaao", "oaaabccbaaao", "..ooabbaoo..", "..oaoaboao..", ".oaooabooao.", ".oo.oabo.oo.", ".....oo....." }; pa = new Color32(120, 200, 255, 255); pb = new Color32(225, 248, 255, 255); pc = new Color32(255, 255, 255, 255); break;
                case Element.Nature: rows = new[] { "........ooo.", "......ooaao.", "....ooaaabo.", "...oaaabbao.", "..oaaabbaao.", "..oaabbaaao.", ".oaabbaaao..", ".oabbaaaao..", ".obbaaaoo...", "obbooooo....", "obo.........", "oo.........." }; pa = new Color32(60, 170, 60, 255); pb = new Color32(150, 235, 100, 255); pc = new Color32(220, 255, 180, 255); break;
                case Element.Water: rows = new[] { ".....oo.....", ".....oao....", "....oaao....", "....oaaao...", "...oaaaao...", "..oaaaaaao..", "..oaaaaaaao.", ".oaabaaaaao.", ".oacbaaaaao.", ".oacbbaaaao.", "..oaaaaaao..", "...oooooo..." }; pa = new Color32(40, 110, 230, 255); pb = new Color32(120, 190, 255, 255); pc = new Color32(230, 245, 255, 255); break;
                case Element.Light: rows = new[] { ".....oo.....", ".....oao....", "....oabo....", "....oabo....", ".oooabbaooo.", "oaabbccbbaao", "oaabbccbbaao", ".oooabbaooo.", "....oabo....", "....oabo....", ".....oao....", ".....oo....." }; pa = new Color32(240, 190, 60, 255); pb = new Color32(255, 240, 140, 255); pc = new Color32(255, 255, 240, 255); break;
                default: return Pixel;
            }
            var ink = new Color32(28, 20, 16, 255);
            return Make("el_" + e, 12, 12, (x, y) =>
            {
                char ch = rows[11 - y][x];   // rows are written top-down; textures fill bottom-up
                return ch == 'o' ? (Color)ink : ch == 'a' ? (Color)pa : ch == 'b' ? (Color)pb : ch == 'c' ? (Color)pc : Color.clear;
            }, new Vector2(0.5f, 0.5f));
        }

        /// Ticket 50: a patch of freshly turned earth: something's buried here.
        public static Sprite DirtPatch
        {
            get
            {
                string[] rows = { "............", "............", "............", "....o..o....", "..oaaoaaao..", ".oabaacaabo.", "oaaacaaabaao", ".oaaaaaaaao.", "..oooooooo..", "............", "............", "............" };
                var ink = new Color32(46, 30, 18, 255); var a = new Color32(112, 78, 46, 255); var b = new Color32(150, 110, 68, 255); var c = new Color32(128, 120, 110, 255);
                return Make("dirtpatch", 12, 12, (x, y) => { char ch = rows[11 - y][x]; return ch == 'o' ? (Color)ink : ch == 'a' ? (Color)a : ch == 'b' ? (Color)b : ch == 'c' ? (Color)c : Color.clear; }, new Vector2(0.5f, 0.35f));
            }
        }

        /// Ticket 48: a little burlap pouch for loot lying on the floor.
        public static Sprite Pouch
        {
            get
            {
                string[] rows = { "....oooo....", "...obbbbo...", "....oaao....", "...oaaaao...", "..oaaaaaao..", ".oaacaaaaao.", ".oaaacaaaao.", ".oaaaaaaaao.", ".oaaaaaaaao.", "..oaaaaaao..", "...oooooo...", "............" };
                var ink = new Color32(40, 26, 14, 255); var a = new Color32(176, 124, 70, 255); var b = new Color32(226, 186, 92, 255); var c = new Color32(214, 168, 106, 255);
                return Make("pouch", 12, 12, (x, y) => { char ch = rows[11 - y][x]; return ch == 'o' ? (Color)ink : ch == 'a' ? (Color)a : ch == 'b' ? (Color)b : ch == 'c' ? (Color)c : Color.clear; }, new Vector2(0.5f, 0.1f));
            }
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

        /// Ticket 55: a big soft blob (fog banks, breath, heat shimmer): white in the middle, fading to nothing at the edge.
        public static Sprite Soft
        {
            get
            {
                return Make("soft", 32, 32, (x, y) =>
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16)) / 16f;
                    float a = Mathf.Clamp01(1f - d); a = a * a * (3f - 2f * a);
                    return new Color(1, 1, 1, a);
                }, new Vector2(0.5f, 0.5f));
            }
        }

        static Sprite Rows(string key, string[] rows, Color32 a, Color32 b, Color32 o)
        {
            int h = rows.Length, w = rows[0].Length;
            return Make(key, w, h, (x, y) =>
            {
                char ch = rows[h - 1 - y][x];
                return ch == 'a' ? (Color)a : ch == 'b' ? (Color)b : ch == 'o' ? (Color)o : Color.clear;
            }, new Vector2(0.5f, 0.5f));
        }

        /// Ticket 56: an ice shard (frost deaths), a leaf (nature deaths), a droplet (water) and a twinkle (light). White-ish, tinted by the caller.
        public static Sprite Shard { get { return Rows("shard", new[] { "..o..", ".oao.", ".oao.", "oabao", "oabao", ".oao.", "..o.." }, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(170, 200, 230, 255)); } }
        public static Sprite Leaf { get { return Rows("leaf", new[] { "....oo", "..oaao", ".oabao", "oabao.", "obao..", "oo...." }, new Color32(255, 255, 255, 255), new Color32(200, 230, 170, 255), new Color32(120, 150, 100, 255)); } }
        public static Sprite Drop { get { return Rows("drop", new[] { "..o..", ".oao.", "oaaao", "oabao", ".ooo." }, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(150, 180, 220, 255)); } }
        public static Sprite Twinkle { get { return Rows("twinkle", new[] { "..a..", "..a..", "aabaa", "..a..", "..a.." }, new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255)); } }

        static readonly Dictionary<Sprite, Color> _avg = new Dictionary<Sprite, Color>();

        /// Ticket 56: a sprite's average visible colour (read back once from the GPU and cached), brightened a little.
        public static Color AverageColour(Sprite s, Color fallback)
        {
            if (s == null || s.texture == null) return fallback;
            Color c;
            if (_avg.TryGetValue(s, out c)) return c;
            c = fallback;
            try
            {
                var r = s.textureRect;
                int w = Mathf.Max(1, (int)r.width), h = Mathf.Max(1, (int)r.height);
                Color[] px;
                if (s.texture.isReadable) px = s.texture.GetPixels((int)r.x, (int)r.y, w, h);
                else
                {
                    var rt = RenderTexture.GetTemporary(s.texture.width, s.texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    var prev = RenderTexture.active;
                    Graphics.Blit(s.texture, rt);
                    RenderTexture.active = rt;
                    var tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
                    tmp.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0); tmp.Apply();
                    RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
                    px = tmp.GetPixels();
                    Object.Destroy(tmp);
                }
                float R = 0, G = 0, B = 0, n = 0;
                foreach (var p in px) { if (p.a < 0.5f) continue; float lum = p.r * 0.3f + p.g * 0.59f + p.b * 0.11f; if (lum < 0.12f) continue; R += p.r; G += p.g; B += p.b; n++; }   // skip outlines and see-through
                if (n > 0) { c = new Color(R / n, G / n, B / n); float m = Mathf.Max(c.r, Mathf.Max(c.g, c.b)); if (m > 0.01f) c = Color.Lerp(c, c / m, 0.6f); c.a = 1f; }
            }
            catch (System.Exception) { c = fallback; }
            _avg[s] = c;
            return c;
        }

        /// Ticket 56: a monster's death, in its own colour and element. Fire leaves rising embers, frost shatters into ice,
        /// nature scatters leaves, water splashes, light twinkles; everything also throws chunks of the monster's own colour.
        public static void DeathBurst(Vector3 pos, Element e, Color body, bool boss, bool elite)
        {
            float big = boss ? 2.6f : elite ? 1.5f : 1f;
            Color ec = e == Element.None ? body : HearthHollow.Dungeon.EnemyController.ElementColour(e);
            // the monster comes apart: chunks of its own colour, then a soft puff
            for (int i = 0; i < Mathf.RoundToInt(12 * big); i++)
            {
                Vector2 o = Random.insideUnitCircle.normalized;
                var fx = Spawn(Pixel, pos + (Vector3)(o * 0.1f), Color.Lerp(body, Color.white, Random.Range(0f, 0.3f)), Random.Range(0.55f, 0.9f), Random.Range(6f, 10f), 3f, 0f, 20600, false);
                fx.velocity = o * Random.Range(3f, 7f) + Vector2.up * 1.5f; fx.drag = 3.2f; fx.accel = new Vector2(0f, -7f); fx.spin = Random.Range(-360f, 360f);
            }
            for (int i = 0; i < Mathf.RoundToInt(6 * big); i++)
            {
                var fx = Spawn(Circle, pos + (Vector3)(Random.insideUnitCircle * 0.2f), new Color(body.r, body.g, body.b, 0.55f), Random.Range(0.35f, 0.6f), Random.Range(0.35f, 0.6f) * big, 0.1f);
                fx.velocity = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.6f) * big;
            }
            switch (e)
            {
                case Element.Fire:
                    Embers(pos, new Color(1f, 0.55f, 0.15f), Mathf.RoundToInt(22 * big), 0.5f * big);
                    for (int i = 0; i < Mathf.RoundToInt(5 * big); i++)
                    {
                        var fx = Spawn(Flame, pos + (Vector3)(Random.insideUnitCircle * 0.35f * big), new Color(1f, Random.Range(0.45f, 0.7f), 0.2f, 0.9f), Random.Range(0.5f, 0.8f), Random.Range(0.5f, 0.8f), 0.1f, 0f, 20600, true);
                        fx.velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(1.2f, 2.2f));
                    }
                    break;
                case Element.Frost:
                    for (int i = 0; i < Mathf.RoundToInt(14 * big); i++)
                    {
                        Vector2 o = Random.insideUnitCircle.normalized;
                        var fx = Spawn(Shard, pos + (Vector3)(o * 0.15f), Color.Lerp(ec, Color.white, Random.Range(0.3f, 0.8f)), Random.Range(0.7f, 1.1f), Random.Range(1.6f, 2.4f), 1.2f, Random.Range(0f, 360f), 20600, false);
                        fx.velocity = o * Random.Range(3f, 6.5f) + Vector2.up * 2f; fx.drag = 2.5f; fx.accel = new Vector2(0f, -9f); fx.spin = Random.Range(-540f, 540f);
                    }
                    RingAt(pos, new Color(0.75f, 0.92f, 1f, 0.8f), 0.9f * big, 0.3f);
                    break;
                case Element.Nature:
                    for (int i = 0; i < Mathf.RoundToInt(12 * big); i++)
                    {
                        Vector2 o = Random.insideUnitCircle.normalized;
                        Color leaf = Color.Lerp(ec, new Color(0.85f, 0.75f, 0.25f), Random.Range(0f, 0.35f));
                        var fx = Spawn(Leaf, pos + (Vector3)(o * 0.15f), leaf, Random.Range(1.4f, 2.2f), Random.Range(1.8f, 2.4f), 1.5f, Random.Range(0f, 360f), 20600, false);
                        fx.velocity = o * Random.Range(1.5f, 3.5f) + Vector2.up * 2.5f; fx.drag = 3.5f; fx.accel = new Vector2(0f, -1.2f);
                        fx.spin = Random.Range(-200f, 200f); fx.sway = Random.Range(0.6f, 1.2f) * (Random.value < 0.5f ? -1f : 1f);
                    }
                    break;
                case Element.Water:
                    for (int i = 0; i < Mathf.RoundToInt(14 * big); i++)
                    {
                        Vector2 o = Quaternion.Euler(0, 0, Random.Range(-70f, 70f)) * Vector2.up;
                        var fx = Spawn(Drop, pos, Color.Lerp(ec, Color.white, Random.Range(0.1f, 0.5f)), Random.Range(0.6f, 0.9f), Random.Range(1.5f, 2f), 1f, 0f, 20600, false);
                        fx.velocity = o * Random.Range(3f, 6f); fx.drag = 1.5f; fx.accel = new Vector2(0f, -12f);
                    }
                    RingAt(pos, new Color(0.5f, 0.75f, 1f, 0.7f), 0.8f * big, 0.35f);
                    break;
                case Element.Light:
                    for (int i = 0; i < Mathf.RoundToInt(10 * big); i++)
                    {
                        var fx = Spawn(Twinkle, pos + (Vector3)(Random.insideUnitCircle * 0.8f * big), new Color(1f, 0.95f, 0.6f), Random.Range(0.5f, 0.9f), Random.Range(1.5f, 2.5f), 0.2f, 0f, 20600, true);
                        fx.velocity = Vector2.up * Random.Range(0.3f, 1f); fx.spin = Random.Range(-90f, 90f);
                    }
                    break;
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
                l.GetComponent<FxLight>().Done = FloorLightOut;
            }
        }

        static readonly System.Action FloorLightOut = () => _floorLights = Mathf.Max(0, _floorLights - 1);

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

        // ───────────── pools: one-shot sprites and lights are reused, not made and destroyed ─────────────
        static Transform _poolRoot;
        static readonly Stack<FxSprite> _sprites = new Stack<FxSprite>();
        static readonly Stack<FxLight> _lights = new Stack<FxLight>();
        static readonly List<FxLight> _riders = new List<FxLight>();
        static Material _spriteDefault;

        /// GameObjects the pools have made so far. After a warm-up, effects reuse what they have and this stops growing.
        public static int Created { get; private set; }

        static Transform PoolRoot
        {
            get
            {
                if (_poolRoot == null)   // first use, or the scene that held it is gone
                {
                    _poolRoot = new GameObject("fx_pool").transform;
                    _sprites.Clear(); _lights.Clear();
                }
                return _poolRoot;
            }
        }

        static FxSprite TakeSprite(Transform parent)
        {
            var root = PoolRoot;
            FxSprite fx = null;
            while (fx == null && _sprites.Count > 0) fx = _sprites.Pop();   // skips any destroyed with a parent they rode on
            if (fx == null)
            {
                var go = new GameObject("fx"); Created++;
                var sr = go.AddComponent<SpriteRenderer>();
                if (_spriteDefault == null) _spriteDefault = sr.sharedMaterial;
                fx = go.AddComponent<FxSprite>();
            }
            fx.transform.SetParent(parent != null ? parent : root, false);
            fx.transform.localPosition = Vector3.zero; fx.transform.localRotation = Quaternion.identity; fx.transform.localScale = Vector3.one;
            var r = fx.Renderer;
            r.flipX = false; r.flipY = false; r.sortingLayerID = 0;
            fx.gameObject.SetActive(true);
            return fx;
        }

        /// Back to the pool (FxSprite calls this when it has faded). Lights riding on it go back first.
        public static void Release(FxSprite fx)
        {
            if (fx == null) return;
            fx.GetComponentsInChildren(true, _riders);
            foreach (var l in _riders) Release(l);
            _riders.Clear();
            if (_poolRoot == null) { Object.Destroy(fx.gameObject); return; }
            fx.gameObject.SetActive(false);
            fx.transform.SetParent(_poolRoot, false);
            _sprites.Push(fx);
        }

        static FxLight TakeLight(Transform parent)
        {
            var root = PoolRoot;
            FxLight fl = null;
            while (fl == null && _lights.Count > 0) fl = _lights.Pop();
            if (fl == null)
            {
                var go = new GameObject("fx_light"); Created++;
                var l = go.AddComponent<Light2D>();
                l.lightType = Light2D.LightType.Point;
                LightSetup.ApplyToAllLayers(l);
                var w = new GameObject("fx_wallglow"); Created++;   // walls catch it from further away than the floor
                w.transform.SetParent(go.transform, false);
                var wl = w.AddComponent<Light2D>();
                wl.lightType = Light2D.LightType.Point;
                LightSetup.ApplyToLayers(wl, "Walls");
                fl = go.AddComponent<FxLight>();
                fl.Setup(l, wl);
            }
            var shadowSafe = fl.GetComponent<ShadowSafeLight>();   // a fire that cast shadows last time
            if (shadowSafe != null) Object.Destroy(shadowSafe);
            fl.transform.SetParent(parent != null ? parent : root, true);
            fl.gameObject.SetActive(true);
            return fl;
        }

        /// Back to the pool (FxLight calls this when it has faded).
        public static void Release(FxLight fl)
        {
            if (fl == null) return;
            if (_poolRoot == null) { Object.Destroy(fl.gameObject); return; }
            fl.gameObject.SetActive(false);
            fl.transform.SetParent(_poolRoot, false);
            _lights.Push(fl);
        }

        // ───────────── one-shot effects ─────────────
        public static FxSprite Spawn(Sprite sprite, Vector3 pos, Color color, float life, float startScale, float endScale, float rotation = 0f, int order = 20500, bool glow = false)
        {
            var fx = TakeSprite(null);
            fx.transform.position = pos;
            fx.transform.rotation = Quaternion.Euler(0, 0, rotation);
            var sr = fx.Renderer;
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = order;
            sr.sharedMaterial = glow && FxMaterials.Glow != null ? FxMaterials.Glow : _spriteDefault;
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
            Color c = new Color(color.r, color.g, color.b, 1f);
            if (life <= 0f) return StayingLight(pos, c, radius, intensity, follow);
            var fl = TakeLight(null);
            fl.transform.position = pos;
            if (follow != null) fl.transform.SetParent(follow, true);
            Light(fl.Main, c, intensity, Mathf.Max(0.2f, radius), radius * 0.15f, 0.35f);   // a softer, more gradual edge
            bool wall = radius >= 1.5f;
            fl.Wall.gameObject.SetActive(wall);
            if (wall) Light(fl.Wall, c, intensity * 0.6f, radius * 1.7f, radius * 0.2f, 0.3f);
            fl.Init(life);
            return fl.Main;
        }

        static void Light(Light2D l, Color c, float intensity, float outer, float inner, float falloff)
        {
            l.shadowsEnabled = false;   // URP turns shadows on by default; only CastShadows turns them on (a few big lights)
            l.color = c; l.intensity = intensity;
            l.pointLightOuterRadius = outer; l.pointLightInnerRadius = inner;
            l.falloffIntensity = falloff;
        }

        /// A light that stays until its parent goes (not pooled: arrows, the reticle, beams).
        static Light2D StayingLight(Vector3 pos, Color c, float radius, float intensity, Transform follow)
        {
            var go = new GameObject("fx_light");
            go.transform.position = pos;
            if (follow != null) go.transform.SetParent(follow, true);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            Light(l, c, intensity, Mathf.Max(0.2f, radius), radius * 0.15f, 0.35f);
            LightSetup.ApplyToAllLayers(l);
            if (radius >= 1.5f)
            {
                var w = new GameObject("fx_wallglow");
                w.transform.SetParent(go.transform, false);
                var wl = w.AddComponent<Light2D>();
                wl.lightType = Light2D.LightType.Point;
                Light(wl, c, intensity * 0.6f, radius * 1.7f, radius * 0.2f, 0.3f);
                LightSetup.ApplyToLayers(wl, "Walls");
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
            var fx = TakeSprite(follow ? src.transform : null);
            if (!follow) { fx.transform.position = src.transform.position; fx.transform.rotation = src.transform.rotation; fx.transform.localScale = src.transform.lossyScale; }
            var sr = fx.Renderer;
            sr.sprite = src.sprite; sr.flipX = src.flipX; sr.flipY = src.flipY; sr.color = color;
            sr.sortingLayerID = src.sortingLayerID; sr.sortingOrder = src.sortingOrder + (follow ? 1 : -1);
            var m = flat ? FxMaterials.Flat : FxMaterials.Glow;
            sr.sharedMaterial = m != null ? m : _spriteDefault;
            fx.InitKeep(life, color);
        }
    }

    /// Fades / scales / drifts a sprite, then goes back to DungeonFx's pool. Runs on scaled time (pauses with the game).
    public class FxSprite : MonoBehaviour
    {
        public Vector2 velocity;
        public Vector2 accel;
        public float drag = 6f;
        public float spin;    // degrees a second (shards, leaves)
        public float sway;    // side-to-side flutter (leaves falling)
        float _life, _t, _s0, _s1; Color _c; SpriteRenderer _sr; bool _keep;

        public SpriteRenderer Renderer { get { if (_sr == null) _sr = GetComponent<SpriteRenderer>(); return _sr; } }

        public void Init(float life, float s0, float s1, Color c)
        {
            Restart(life, c, false);
            _s0 = s0; _s1 = s1;
            transform.localScale = Vector3.one * s0;
        }

        /// Fade only — keeps the transform's scale as it is (ghosts).
        public void InitKeep(float life, Color c) { Restart(life, c, true); }

        void Restart(float life, Color c, bool keep)
        {
            _life = Mathf.Max(0.01f, life); _t = 0f; _c = c; _keep = keep;
            velocity = Vector2.zero; accel = Vector2.zero; drag = 6f; spin = 0f; sway = 0f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            if (!_keep) transform.localScale = Vector3.one * Mathf.Lerp(_s0, _s1, k);
            velocity += accel * Time.deltaTime;
            transform.position += (Vector3)(velocity * Time.deltaTime);
            if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
            if (sway != 0f) transform.position += new Vector3(Mathf.Sin(_t * 6f) * sway * Time.deltaTime, 0f, 0f);
            velocity *= 1f - Mathf.Clamp01(Time.deltaTime * drag);
            Renderer.color = new Color(_c.r, _c.g, _c.b, _c.a * (1f - k));
            if (_t >= _life) DungeonFx.Release(this);
        }
    }

    /// Fades a pooled Light2D (and its wall glow) out, then goes back to DungeonFx's pool. Scaled time (freezes with
    /// hit-stop and pause).
    public class FxLight : MonoBehaviour
    {
        float _life, _t, _i0, _w0;
        public Light2D Main { get; private set; }
        public Light2D Wall { get; private set; }
        /// Called once when it has faded (then cleared).
        public System.Action Done;

        public void Setup(Light2D main, Light2D wall) { Main = main; Wall = wall; }

        public void Init(float life)
        {
            _life = Mathf.Max(0.01f, life); _t = 0f; Done = null;
            _i0 = Main.intensity; _w0 = Wall.intensity;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life), f = (1f - k) * (1f - k);
            Main.intensity = _i0 * f;
            if (Wall.gameObject.activeSelf) Wall.intensity = _w0 * f;
            if (_t >= _life)
            {
                var done = Done; Done = null;
                if (done != null) done();
                DungeonFx.Release(this);
            }
        }
    }
}
