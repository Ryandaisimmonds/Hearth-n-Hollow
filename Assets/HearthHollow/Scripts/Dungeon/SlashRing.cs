// Hearth & Hollow — a huge sweeping slash all the way round a character (the Effects Lab's Verdant Crescent, first).
// The blade — a line from near the character out past the trail's edge, like a sword held out — travels round an
// ellipse (the ground seen at an angle). Where the blade is now it burns white-hot
// and lights the room; behind it the trail cools to a pale green, then a deeper green, and grows wider and drifts
// outwards the longer it hangs, before fading. The half of the ring above the character draws behind them, the half
// below in front. Everything runs on scaled time, so the lab's slow motion (and hit-stops) slow it down too.
//
// The trail is a mesh ribbon (two of them: back and front) with streaks along its length (Rendering/GlowTrail.shader).
// Swept tells whoever cast it which angles the blade crossed this frame, so they can hit what's there.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class SlashRing : MonoBehaviour
    {
        // ── the shape (set by Play; tweak freely in the lab) ──
        public float Radius = 2.6f;        // world units to the blade's edge
        public float Width = 1.3f;         // the trail's widest (deep in the tail)
        public float Squash = 0.72f;       // the ellipse's height ÷ width (ground seen at an angle)
        public float Turns = 1.15f;        // how far round it sweeps (1 = once)
        public float SweepTime = 0.5f;     // seconds the blade takes (fast at first, dragging at the end)
        public float TrailLife = 0.6f;     // seconds a bit of trail hangs before it's gone
        public float Grow = 0.45f;         // how much wider the trail gets as it ages (× Width)
        public float Drift = 0.14f;        // how far it drifts outwards as it ages (× Radius)
        public int Spin = 1;               // +1 anticlockwise, −1 clockwise
        public float BladeFrom = 0.3f;     // where the blade starts (× Radius out from the centre)…
        public float BladeReach = 0.18f;   // …and how far its tip pokes past the trail's edge (world units)
        public float BladeThickness = 0.36f;
        public bool ShowBlade = true;      // false: just the trail (echo rings and wind lines round a bigger swing)
        public float Delay;                // seconds before it starts (echoes trail the main swing a moment behind)
        public float StartFadeDeg = 40f;   // the swing's first bit fades in over this many degrees (0 = a full ring at once)
        public float HeatScale = 1f;       // × how bright the whole trail burns (a stronger green)
        public int Sparks = 1;             // sparks thrown off the tip each beat…
        public float LeafChance = 0.35f;   // …and the chance of a leaf

        // the colour the trail cools through as it ages: white-hot → very light green → light green → green
        public Color HotColour = new Color(1f, 1f, 0.96f);
        public Color PaleColour = new Color(0.78f, 1f, 0.5f);
        public Color LightColour = new Color(0.42f, 1f, 0.2f);
        public Color DeepColour = new Color(0.1f, 0.72f, 0.16f);
        /// How bright the blade burns (HDR: over 1 blooms) and how much it lights the room.
        public float HeadHeat = 3.2f, LightIntensity = 0.55f;

        /// (from, to) in radians, unwrapped, each frame the blade moves.
        public System.Action<float, float> Swept;
        /// Where the ring's centre is right now (follows the caster).
        public Vector2 Centre { get { return transform.position; } }
        public bool Sweeping { get { return _t < SweepTime; } }

        Transform _follow; Vector2 _offset;
        float _start, _t, _lastA, _lastT;
        bool _done;
        struct Sample { public float a, born; }
        readonly List<Sample> _samples = new List<Sample>(256);

        MeshRenderer _backR, _frontR; Mesh _back, _front;
        SpriteRenderer _blade, _halo; Transform _lightAt; Light2D _light;
        float _emitT;

        /// Start a sweep round <paramref name="follow"/> (+ offset, e.g. the body's height above the feet).
        public static SlashRing Play(Transform follow, Vector2 offset, float startRadians, int spin)
        {
            var go = new GameObject("fx_slash_ring");
            var r = go.AddComponent<SlashRing>();
            r._follow = follow; r._offset = offset; r._start = startRadians; r.Spin = spin >= 0 ? 1 : -1;
            r._lastA = startRadians;
            go.transform.position = (Vector2)follow.position + offset;
            return r;
        }

        void Start()
        {
            _back = NewMesh("Back", out _backR);
            _front = NewMesh("Front", out _frontR);
            if (ShowBlade)
            {
                _halo = BladeSprite("Blade glow");   // a wide soft glow round…
                _blade = BladeSprite("Blade");       // …a thin white-hot line
                _halo.enabled = _blade.enabled = false;   // placed on the first frame
                _lightAt = new GameObject("Blade light").transform; _lightAt.SetParent(transform, false);
                if (LightIntensity > 0f) _light = DungeonFx.Flash(transform.position, PaleColour, Radius * 1.3f, 0f, 0f, _lightAt);   // the blade lights the room as it goes
            }
            AddSample(_start, 0f);
        }

        SpriteRenderer BladeSprite(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BladeShape;
            if (FxMaterials.Glow != null) sr.sharedMaterial = FxMaterials.Glow;
            return sr;
        }

        Mesh NewMesh(string name, out MeshRenderer mr)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = new Mesh { name = "slash ring " + name };
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return go.GetComponent<MeshFilter>().sharedMesh;
        }

        /// Where the swing starts (radians on the ellipse) — for a caller that needs the ring before it knows the angle.
        public void SetStart(float radians) { _start = radians; _lastA = radians; }

        float AngleAt(float t)
        {
            float p = Mathf.Clamp01(t / Mathf.Max(0.01f, SweepTime));
            float eased = 1f - Mathf.Pow(1f - p, 2.2f);   // whips round, then drags
            return _start + Spin * Turns * Mathf.PI * 2f * eased;
        }

        void AddSample(float a, float born) { _samples.Add(new Sample { a = a, born = born }); }

        void Update()
        {
            if (_follow != null) transform.position = (Vector2)_follow.position + _offset;
            float dt = Time.deltaTime;
            if (Delay > 0f) { Delay -= dt; return; }
            _t += dt;

            // the blade moves: lay trail every few degrees so the ribbon stays smooth however fast it goes
            if (!_done)
            {
                float tNow = Mathf.Min(_t, SweepTime);
                float a = AngleAt(tNow);
                float step = 3f * Mathf.Deg2Rad;
                int n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(a - _lastA) / step));
                for (int i = 1; i <= n; i++)
                {
                    float k = i / (float)n;
                    AddSample(Mathf.Lerp(_lastA, a, k), Mathf.Lerp(_lastT, tNow, k));
                }
                if (Swept != null && a != _lastA) Swept(_lastA, a);
                _lastA = a; _lastT = tNow;
                if (_t >= SweepTime) _done = true;
            }

            // the old trail goes
            while (_samples.Count > 0 && _t - _samples[0].born > TrailLife) _samples.RemoveAt(0);
            if (_done && _samples.Count == 0) { Destroy(gameObject); return; }

            Rebuild();
            UpdateHead(dt);
        }

        // ───────────────────────── the look ─────────────────────────
        Vector2 Point(float a, float r) { return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r * Squash); }

        /// The trail's colour and brightness at an age (seconds behind the blade).
        void Cool(float age, out Color c, out float heat)
        {
            float k = age / Mathf.Max(0.01f, TrailLife);
            if (age < 0.06f) { float u = age / 0.06f; c = Color.Lerp(HotColour, PaleColour, u * u); heat = Mathf.Lerp(HeadHeat, 1.5f, u); }
            else if (k < 0.3f) { float u = Mathf.InverseLerp(0.06f / TrailLife, 0.3f, k); c = Color.Lerp(PaleColour, LightColour, u); heat = Mathf.Lerp(1.5f, 1.15f, u); }
            else { float u = Mathf.InverseLerp(0.3f, 1f, k); c = Color.Lerp(LightColour, DeepColour, u); heat = Mathf.Lerp(1.15f, 0.95f, u); }
            c.a = k < 0.5f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.5f) / 0.5f);
        }

        readonly List<Vector3> _bv = new List<Vector3>(), _fv = new List<Vector3>();
        readonly List<Color> _bc = new List<Color>(), _fc = new List<Color>();
        readonly List<Vector2> _bu = new List<Vector2>(), _fu = new List<Vector2>(), _bh = new List<Vector2>(), _fh = new List<Vector2>();
        readonly List<int> _bt = new List<int>(), _ft = new List<int>();

        void Rebuild()
        {
            _bv.Clear(); _fv.Clear(); _bc.Clear(); _fc.Clear(); _bu.Clear(); _fu.Clear(); _bh.Clear(); _fh.Clear(); _bt.Clear(); _ft.Clear();
            int n = _samples.Count;
            float headA = n > 0 ? _samples[n - 1].a : _start;
            for (int i = 0; i + 1 < n; i++)
            {
                var s0 = _samples[i]; var s1 = _samples[i + 1];
                float mid = (s0.a + s1.a) * 0.5f;
                bool back = Mathf.Sin(mid) > 0f;   // the half above the centre is behind the character
                var V = back ? _bv : _fv; var C = back ? _bc : _fc; var U = back ? _bu : _fu; var H = back ? _bh : _fh; var T = back ? _bt : _ft;
                int b = V.Count;
                Edge(s0, headA, V, C, U, H);
                Edge(s1, headA, V, C, U, H);
                T.Add(b); T.Add(b + 1); T.Add(b + 3);
                T.Add(b); T.Add(b + 3); T.Add(b + 2);
            }
            Fill(_back, _bv, _bc, _bu, _bh, _bt);
            Fill(_front, _fv, _fc, _fu, _fh, _ft);
            int feet = DungeonController.SortOrder(_follow != null ? _follow.position.y : transform.position.y);
            _backR.sortingOrder = feet - 3;     // behind the caster, over the floor
            _frontR.sortingOrder = 20400;       // over everyone (hit sparks still on top)
        }

        void Edge(Sample s, float headA, List<Vector3> V, List<Color> C, List<Vector2> U, List<Vector2> H)
        {
            float age = Mathf.Max(0f, _t - s.born);
            float k = age / Mathf.Max(0.01f, TrailLife);
            float behind = Mathf.Abs(headA - s.a);                                  // radians behind the blade
            float taper = Mathf.SmoothStep(0f, 1f, behind / (35f * Mathf.Deg2Rad));   // a little narrower right at the blade, full just behind
            float w = Width * (0.8f + 0.2f * taper) * (1f + Grow * k);
            float fromStart = Mathf.Abs(s.a - _start);                                  // the very start of the swing fades in (no hard cut)
            float r = Radius * (1f + Drift * k);
            Color c; float heat; Cool(age, out c, out heat);
            if (StartFadeDeg > 0f) c.a *= Mathf.SmoothStep(0f, 1f, fromStart / (StartFadeDeg * Mathf.Deg2Rad));
            heat *= HeatScale;
            V.Add(Point(s.a, r - w)); V.Add(Point(s.a, r));
            Color inner = c; inner.a *= 0.85f;
            C.Add(inner); C.Add(c);
            float u = s.a / (Mathf.PI * 2f) * 3f;   // streaks repeat three times round
            U.Add(new Vector2(u, 0f)); U.Add(new Vector2(u, 1f));
            H.Add(new Vector2(heat, 0f)); H.Add(new Vector2(heat, 0f));
        }

        static void Fill(Mesh m, List<Vector3> v, List<Color> c, List<Vector2> u, List<Vector2> h, List<int> t)
        {
            m.Clear();
            if (v.Count == 0) return;
            m.SetVertices(v); m.SetColors(c); m.SetUVs(0, u); m.SetUVs(1, h); m.SetTriangles(t, 0);
            m.RecalculateBounds();
        }

        void UpdateHead(float dt)
        {
            float p = Mathf.Clamp01(_t / Mathf.Max(0.01f, SweepTime));
            float a = _lastA;
            // the blade: a line from near the centre out past the edge, along the ellipse's spoke
            Vector2 root = Point(a, Radius * BladeFrom);
            Vector2 tipDir = Point(a, Radius) - root;
            float len = tipDir.magnitude + BladeReach;
            float rot = Mathf.Atan2(tipDir.y, tipDir.x) * Mathf.Rad2Deg;
            Vector2 at = Point(a, Radius);   // the tip (sparks fly from here)
            // white-hot while it whips round, greener as it drags to a stop, then gone
            float fade = _done ? Mathf.Clamp01(1f - (_t - SweepTime) / 0.15f) : 1f;
            if (!ShowBlade) { Emit(dt, a, at); return; }
            _halo.enabled = _blade.enabled = true;
            Color hc = Color.Lerp(HotColour, PaleColour, p * p); hc.a = fade;
            Color gc = Color.Lerp(PaleColour, LightColour, p); gc.a = 0.75f * fade;
            float flicker = 1f + 0.12f * Mathf.Sin(_t * 70f);
            int order = Mathf.Sin(a) > 0f ? _backR.sortingOrder + 1 : 20410;
            PlaceBlade(_blade, root, rot, len, BladeThickness * flicker * Mathf.Lerp(1f, 0.7f, p), hc, order + 1);
            PlaceBlade(_halo, root, rot, len * 1.04f, BladeThickness * 3.5f * Mathf.Lerp(1f, 0.75f, p), gc, order);
            _lightAt.localPosition = Point(a, Radius * 0.75f);
            if (_light != null)
            {
                _light.color = Color.Lerp(PaleColour, LightColour, p);
                _light.intensity = Mathf.Lerp(LightIntensity, LightIntensity * 0.5f, p) * fade;
                if (fade <= 0f) { Destroy(_light.gameObject); _light = null; }
            }

            Emit(dt, a, at);
        }

        /// Sparks and leaves thrown off the blade's tip, along it and outwards.
        void Emit(float dt, float a, Vector2 at)
        {
            if (_done || (Sparks <= 0 && LeafChance <= 0f)) return;
            _emitT -= dt;
            if (_emitT > 0f) return;
            _emitT = 0.018f;
            Vector2 world = (Vector2)transform.position + at;
            Vector2 tangent = new Vector2(-Mathf.Sin(a), Mathf.Cos(a) * Squash) * Spin;
            Vector2 outward = new Vector2(Mathf.Cos(a), Mathf.Sin(a) * Squash);
            for (int i = 0; i < Sparks; i++)
            {
                var fx = DungeonFx.Spawn(DungeonFx.Pixel, world + Random.insideUnitCircle * 0.25f, Color.Lerp(HotColour, PaleColour, Random.value),
                                         Random.Range(0.25f, 0.45f), Random.Range(2.5f, 4f), 0.5f, 0f, 20460, true);
                fx.velocity = tangent.normalized * Random.Range(1.5f, 4f) + outward.normalized * Random.Range(1f, 3.5f);
                fx.drag = 4f;
            }
            if (Random.value < LeafChance)
            {
                var leaf = DungeonFx.Spawn(DungeonFx.Leaf, world, Color.Lerp(LightColour, DeepColour, Random.value), Random.Range(0.6f, 0.9f), 1.2f, 0.9f, Random.Range(0f, 360f), 20460, false);
                leaf.velocity = outward.normalized * Random.Range(1.5f, 3f) + tangent.normalized * 1.5f; leaf.drag = 3f;
                leaf.accel = new Vector2(0f, -1.5f); leaf.spin = Random.Range(-300f, 300f); leaf.sway = Random.Range(0.6f, 1f);
            }
        }

        static void PlaceBlade(SpriteRenderer sr, Vector2 root, float rot, float len, float thick, Color c, int order)
        {
            var t = sr.transform;
            t.localPosition = root;
            t.localRotation = Quaternion.Euler(0f, 0f, rot);
            t.localScale = new Vector3(len, thick / BladeH, 1f);   // the sprite is 1 unit long, BladeH units tall
            sr.color = c; sr.sortingOrder = order;
        }

        // the blade's sprite: 1 unit long (pivot at its root), bright down the middle, soft at the sides, coming to a point
        const int BladePxW = 64, BladePxH = 16;
        const float BladeH = BladePxH / (float)BladePxW;
        static Sprite _bladeShape;
        static Sprite BladeShape
        {
            get
            {
                if (_bladeShape != null) return _bladeShape;
                var tex = new Texture2D(BladePxW, BladePxH, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "fx_blade" };
                var px = new Color[BladePxW * BladePxH];
                for (int y = 0; y < BladePxH; y++)
                    for (int x = 0; x < BladePxW; x++)
                    {
                        float u = (x + 0.5f) / BladePxW, v = Mathf.Abs((y + 0.5f) / BladePxH - 0.5f) * 2f;   // v: 0 on the middle line, 1 at the sides
                        float half = Mathf.Lerp(1f, 0.15f, Mathf.SmoothStep(0.55f, 1f, u));                // full width, narrowing to a point at the tip
                        float across = Mathf.Pow(Mathf.Clamp01(1f - v / half), 0.8f);   // a full bright core, soft only at the very sides
                        float along = Mathf.SmoothStep(0f, 0.25f, u);                                      // fades in from the root
                        px[y * BladePxW + x] = new Color(1f, 1f, 1f, across * along);
                    }
                tex.SetPixels(px); tex.Apply();
                _bladeShape = Sprite.Create(tex, new Rect(0, 0, BladePxW, BladePxH), new Vector2(0f, 0.5f), BladePxW);
                return _bladeShape;
            }
        }

        void OnDestroy()
        {
            if (_light != null) Destroy(_light.gameObject);
            if (_back != null) Destroy(_back);
            if (_front != null) Destroy(_front);
        }

        // ── the material and its streak texture (made once) ──
        static Material _mat;
        static Material Material
        {
            get
            {
                if (_mat != null) return _mat;
                var src = FxMaterials.Trail;
                _mat = src != null ? new Material(src) : new Material(Shader.Find("HearthHollow/Glow Trail"));
                _mat.mainTexture = Streaks();
                return _mat;
            }
        }

        static Texture2D Streaks()
        {
            const int W = 128, H = 32;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapModeU = TextureWrapMode.Repeat, wrapModeV = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "fx_slash_streaks" };
            var rng = new System.Random(7);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;
                float row = 0.55f + 0.45f * (float)rng.NextDouble();               // each streak its own brightness
                int k = 1 + rng.Next(3); float ph = (float)rng.NextDouble() * 6.283f; // and its own breaks along the length
                float edge = Mathf.SmoothStep(0f, 1f, v / 0.55f) * (1f - Mathf.SmoothStep(0f, 1f, (v - 0.92f) / 0.08f));   // soft inside, crisp outside
                float rim = 0.75f + 0.55f * Mathf.SmoothStep(0f, 1f, (v - 0.6f) / 0.32f);                                  // the outer edge burns brighter
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)W;
                    float along = 0.72f + 0.28f * Mathf.Sin(u * 6.283f * k + ph);
                    float b = row * along * rim;
                    px[y * W + x] = new Color(Mathf.Min(1f, b), Mathf.Min(1f, b), Mathf.Min(1f, b), edge * (0.6f + 0.4f * row * along));
                }
            }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        // domain reload is off: the cached material and texture belong to the last play session
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetForPlay() { _mat = null; _bladeShape = null; }
    }
}
