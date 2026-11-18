// Hearth & Hollow — a boss wrapped in a swirling wind: thin bright ribbons spiralling up and round it, a couple sweeping
// round its feet, and glinting motes drifting up through it, all in green (the colours are fields). A vortex: each wisp
// rises as it circles, narrowing towards the top, then fades and starts again at the bottom. Ribbons on the far side
// draw behind the monster. It's a child of the monster, so it follows it and goes when it dies.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class SwirlAura : MonoBehaviour
    {
        public Color Head = new Color(0.8f, 1f, 0.5f);      // the bright tip of each wisp
        public Color Mid = new Color(0.32f, 1f, 0.12f);     // a strong lime…
        public Color Tail = new Color(0.04f, 0.72f, 0.1f);  // …into a deep green
        public float Heat = 2.8f;                           // how bright the tips burn (HDR: blooms)
        public int Wisps = 7;                               // ribbons spiralling up
        public int GroundWisps = 2;                         // ribbons sweeping round the feet
        public float MotesPerSecond = 22f;
        public float Squash = 0.62f;                        // the ellipse (ground seen at an angle)

        float _w, _h;            // the monster's width-ish radius and height
        int _baseOrder;
        Light2D _light;
        float _moteT, _t;

        class Wisp
        {
            public bool ground;
            public float phase, speed, radius, period, offset, width, trail;
            public readonly List<Vector3> pts = new List<Vector3>();    // x, y local; z = time laid
            public float lastU = -1f;
            public Mesh mesh; public MeshRenderer mr;
        }
        readonly List<Wisp> _wisps = new List<Wisp>();

        /// Wrap a monster in the swirl (once; calling again just returns it).
        public static SwirlAura On(EnemyController e)
        {
            var a = e.GetComponentInChildren<SwirlAura>();
            if (a != null) return a;
            var go = new GameObject("Swirl aura");
            go.transform.SetParent(e.transform, false);
            a = go.AddComponent<SwirlAura>();
            float h = Mathf.Max(0.6f, (e.Body.y - e.transform.position.y) / 0.45f);   // Body sits at 45% of the figure's height
            a._h = h; a._w = Mathf.Max(0.6f, h * 0.55f);
            return a;
        }

        public static void Off(EnemyController e)
        {
            var a = e.GetComponentInChildren<SwirlAura>();
            if (a != null) Destroy(a.gameObject);
        }

        void Start()
        {
            var rng = new System.Random(Random.Range(0, int.MaxValue));   // every aura swirls a little differently
            for (int i = 0; i < Wisps + GroundWisps; i++)
            {
                bool ground = i >= Wisps;
                var w = new Wisp
                {
                    ground = ground,
                    phase = (float)(i * Mathf.PI * 2.0 / Mathf.Max(1, ground ? GroundWisps : Wisps) + rng.NextDouble() * 0.6),
                    speed = (ground ? 2.0f : 2.8f) + (float)rng.NextDouble() * 1.2f,
                    radius = _w * (ground ? 1.7f : 1.1f + (float)rng.NextDouble() * 0.35f),
                    period = ground ? 2.6f + (float)rng.NextDouble() : 1.5f + (float)rng.NextDouble() * 0.9f,
                    offset = (float)rng.NextDouble(),
                    width = _h * (ground ? 0.2f : 0.13f) * (0.8f + 0.4f * (float)rng.NextDouble()),
                    trail = ground ? 0.9f : 0.55f,
                };
                var go = new GameObject(ground ? "Ground wisp" : "Wisp");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = w.mesh = new Mesh { name = "swirl wisp" };
                w.mr = go.AddComponent<MeshRenderer>();
                w.mr.sharedMaterial = SlashRing.TrailMaterial;
                w.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; w.mr.receiveShadows = false;
                _wisps.Add(w);
            }
            _light = DungeonFx.Flash(transform.position + new Vector3(0, _h * 0.5f), Mid, _w * 4f, 0.55f, 0f, transform);   // it lights the floor round it green
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            _baseOrder = DungeonController.SortOrder(transform.position.y);
            foreach (var w in _wisps) Step(w);
            Motes(dt);
            if (_light != null) _light.intensity = 0.5f + 0.1f * Mathf.Sin(_t * 5f);
        }

        // ── one wisp: where its tip is now, its trail, and its ribbon ──
        void Step(Wisp w)
        {
            float u = Mathf.Repeat(_t / w.period + w.offset, 1f);   // 0 → 1 through one rise
            if (u < w.lastU) w.pts.Clear();                          // back to the bottom: a new wisp
            w.lastU = u;
            float a = w.phase + _t * w.speed;
            float r, y;
            if (w.ground) { r = w.radius * (0.9f + 0.15f * Mathf.Sin(_t * 1.7f + w.phase)); y = 0.05f; }
            else
            {
                r = w.radius * (1.15f - 0.45f * u) * (1f + 0.08f * Mathf.Sin(_t * 6f + w.phase));   // narrows as it climbs
                y = Mathf.Lerp(0.05f, 1.15f, u) * _h;
            }
            w.pts.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * Squash + y, _t));
            while (w.pts.Count > 0 && _t - w.pts[0].z > w.trail) w.pts.RemoveAt(0);

            float env = Mathf.Sin(Mathf.PI * u);                     // fades in at the bottom, out at the top
            if (w.ground) env = 0.55f + 0.45f * Mathf.Sin(Mathf.PI * 2f * u);
            Build(w, env);
            bool back = Mathf.Sin(a - w.speed * w.trail * 0.5f) > 0f;   // the wisp's middle: on the far side, behind the monster
            w.mr.sortingOrder = back ? _baseOrder - 2 : _baseOrder + 2;
        }

        static readonly List<Vector3> V = new List<Vector3>();
        static readonly List<Color> C = new List<Color>();
        static readonly List<Vector2> U = new List<Vector2>(), H = new List<Vector2>();
        static readonly List<int> T = new List<int>();

        void Build(Wisp w, float env)
        {
            V.Clear(); C.Clear(); U.Clear(); H.Clear(); T.Clear();
            int n = w.pts.Count;
            w.mesh.Clear();
            if (n < 2 || env <= 0.01f) return;
            for (int i = 0; i < n; i++)
            {
                Vector3 p = w.pts[i];
                Vector2 prev = w.pts[Mathf.Max(0, i - 1)], next = w.pts[Mathf.Min(n - 1, i + 1)];
                Vector2 tan = next - prev;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector2.right;
                Vector2 nrm = new Vector2(-tan.y, tan.x).normalized;
                float k = i / (float)(n - 1);                         // 0 = the tail … 1 = the tip
                float taper = Mathf.Sin(Mathf.PI * Mathf.Clamp01(k * 0.92f + 0.04f));   // thin at both ends, full in the middle
                float half = w.width * 0.5f * Mathf.Lerp(0.3f, 1f, taper);
                Color c = k > 0.6f ? Color.Lerp(Mid, Head, (k - 0.6f) / 0.4f) : Color.Lerp(Tail, Mid, k / 0.6f);
                c.a = Mathf.SmoothStep(0f, 1f, k * 1.6f) * env;       // the tail fades away, the tip stays bright
                float heat = Mathf.Lerp(0.9f, Heat, k * k);
                V.Add((Vector2)p - nrm * half); V.Add((Vector2)p + nrm * half);
                C.Add(c); C.Add(c);
                U.Add(new Vector2(k, 0.15f)); U.Add(new Vector2(k, 0.95f));   // the streaky middle of the trail texture
                H.Add(new Vector2(heat, 0f)); H.Add(new Vector2(heat, 0f));
                if (i > 0) { int b = (i - 1) * 2; T.Add(b); T.Add(b + 1); T.Add(b + 3); T.Add(b); T.Add(b + 3); T.Add(b + 2); }
            }
            w.mesh.SetVertices(V); w.mesh.SetColors(C); w.mesh.SetUVs(0, U); w.mesh.SetUVs(1, H); w.mesh.SetTriangles(T, 0);
            w.mesh.RecalculateBounds();
        }

        // ── glinting motes drifting up through the swirl ──
        void Motes(float dt)
        {
            if (MotesPerSecond <= 0f) return;
            _moteT -= dt;
            while (_moteT <= 0f)
            {
                _moteT += 1f / MotesPerSecond;
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(0.2f, 1.5f) * _w;
                Vector3 at = transform.position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * Squash + Random.Range(0f, _h), 0f);
                Color c = Random.value < 0.35f ? new Color(1f, 1f, 0.9f) : Color.Lerp(Head, Mid, Random.value);
                bool front = Mathf.Sin(a) <= 0f;
                var fx = DungeonFx.Spawn(DungeonFx.Pixel, at, c, Random.Range(0.8f, 1.6f), Random.Range(4f, 8f), 1f, 0f, front ? _baseOrder + 3 : _baseOrder - 3, true);
                fx.velocity = new Vector2(-Mathf.Sin(a), Mathf.Cos(a) * Squash) * Random.Range(0.3f, 0.9f) + Vector2.up * Random.Range(0.4f, 1.1f);
                fx.drag = 0.8f;
            }
        }

        void OnDestroy()
        {
            foreach (var w in _wisps) if (w.mesh != null) Destroy(w.mesh);
            if (_light != null) Destroy(_light.gameObject);
        }
    }
}
