// Hearth & Hollow — rings of light orbiting a character: comets of light racing round tilted ellipses (like an atom's
// orbits), each a bright head with a long arc of trail fading behind it, plus sparkles drifting off. For a powered-up,
// glowing body (Radiant Surge). Parented to the character, it follows them; it fades in, holds, then fades out.
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class LightRings : MonoBehaviour
    {
        public Color Head = new Color(1f, 1f, 1f);
        public Color Trail = new Color(0.45f, 0.75f, 1f);
        public float Heat = 3.6f;
        public float SparklesPerSecond = 10f;

        class Ring { public float tilt, rx, ry, speed, phase, span, width; public Mesh mesh; public MeshRenderer mr; }
        readonly List<Ring> _rings = new List<Ring>();
        float _t, _life, _size, _sparkT;

        /// Rings round <paramref name="parent"/>, centred at <paramref name="local"/> (e.g. the body's height), for a while.
        public static LightRings Play(Transform parent, Vector2 local, float size, int rings, float seconds)
        {
            var go = new GameObject("Light rings");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var r = go.AddComponent<LightRings>();
            r._size = size; r._life = seconds;
            float[] tilts = { -32f, 28f, 72f, -68f, 5f };
            for (int i = 0; i < Mathf.Clamp(rings, 1, tilts.Length); i++)
            {
                var ring = new Ring
                {
                    tilt = tilts[i] * Mathf.Deg2Rad,
                    rx = size * (1.5f + 0.15f * i), ry = size * (0.42f + 0.06f * (i % 2)),
                    speed = (i % 2 == 0 ? 1f : -1f) * (6f + 1.3f * i),   // neighbouring rings run opposite ways
                    phase = i * 2.1f, span = (190f + 15f * (i % 3)) * Mathf.Deg2Rad,
                    width = size * 0.12f,
                };
                var m = new GameObject("Ring " + i); m.transform.SetParent(go.transform, false);
                m.AddComponent<MeshFilter>().sharedMesh = ring.mesh = new Mesh { name = "light ring" };
                ring.mr = m.AddComponent<MeshRenderer>();
                ring.mr.sharedMaterial = SlashRing.TrailMaterial;
                ring.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; ring.mr.receiveShadows = false;
                ring.mr.sortingOrder = 20420;
                r._rings.Add(ring);
            }
            return r;
        }

        static readonly List<Vector3> V = new List<Vector3>();
        static readonly List<Color> C = new List<Color>();
        static readonly List<Vector2> U = new List<Vector2>(), H = new List<Vector2>();
        static readonly List<int> T = new List<int>();
        const int Steps = 40;

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            if (_t >= _life) { Destroy(gameObject); return; }
            float fade = Mathf.Clamp01(_t / 0.2f) * Mathf.Clamp01((_life - _t) / 0.4f);
            foreach (var r in _rings) Build(r, fade);

            // sparkles drifting off the rings
            _sparkT -= dt;
            while (_sparkT <= 0f && SparklesPerSecond > 0f)
            {
                _sparkT += 1f / SparklesPerSecond;
                var r = _rings[Random.Range(0, _rings.Count)];
                Vector2 p = (Vector2)transform.position + Point(r, Random.value * Mathf.PI * 2f);
                var fx = DungeonFx.Spawn(DungeonFx.Twinkle, p, Color.Lerp(Head, Trail, Random.value), Random.Range(0.4f, 0.8f), Random.Range(2f, 3.5f), 0.5f, 0f, 20430, true);
                fx.velocity = Random.insideUnitCircle * 0.6f + Vector2.up * 0.4f; fx.drag = 1.5f;
            }
        }

        Vector2 Point(Ring r, float a)
        {
            Vector2 p = new Vector2(Mathf.Cos(a) * r.rx, Mathf.Sin(a) * r.ry);
            float c = Mathf.Cos(r.tilt), s = Mathf.Sin(r.tilt);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);   // the orbit, tilted
        }

        void Build(Ring r, float fade)
        {
            V.Clear(); C.Clear(); U.Clear(); H.Clear(); T.Clear();
            float head = r.phase + _t * r.speed;
            float dir = Mathf.Sign(r.speed);
            for (int i = 0; i <= Steps; i++)
            {
                float k = i / (float)Steps;                                   // 0 = the tail … 1 = the head
                float a = head - dir * r.span * (1f - k);
                Vector2 p = Point(r, a);
                Vector2 tan = Point(r, a + 0.01f * dir) - Point(r, a - 0.01f * dir);
                Vector2 nrm = new Vector2(-tan.y, tan.x).normalized;
                float half = r.width * 0.5f * Mathf.Lerp(0.15f, 1f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.05f) * 0.5f)) * (k > 0.95f ? Mathf.Lerp(1f, 0.4f, (k - 0.95f) / 0.05f) : 1f);
                Color c = Color.Lerp(Trail, Head, k * k); c.a = (0.15f + 0.85f * Mathf.Pow(k, 1.2f)) * Mathf.SmoothStep(0f, 1f, k * 6f) * fade;   // the tail end fades to nothing
                V.Add(p - nrm * half); V.Add(p + nrm * half);
                C.Add(c); C.Add(c);
                U.Add(new Vector2(k, 0.2f)); U.Add(new Vector2(k, 0.95f));
                float heat = Mathf.Lerp(1f, Heat, k * k);
                H.Add(new Vector2(heat, 0f)); H.Add(new Vector2(heat, 0f));
                if (i > 0) { int b = (i - 1) * 2; T.Add(b); T.Add(b + 1); T.Add(b + 3); T.Add(b); T.Add(b + 3); T.Add(b + 2); }
            }
            r.mesh.Clear();
            r.mesh.SetVertices(V); r.mesh.SetColors(C); r.mesh.SetUVs(0, U); r.mesh.SetUVs(1, H); r.mesh.SetTriangles(T, 0);
            r.mesh.RecalculateBounds();
        }

        void OnDestroy() { foreach (var r in _rings) if (r.mesh != null) Destroy(r.mesh); }
    }
}
