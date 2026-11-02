// Hearth & Hollow — weather in Hearth's Hollow, from today's camp event: rain on a storm day, fireflies on a
// harvest-moon night (the lighting itself is set by the TownController). Calm days have neither.
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public class TownWeather : MonoBehaviour
    {
        public enum Kind { Calm, Rain, Night }

        Kind _kind;
        Transform[] _bits;
        SpriteRenderer[] _srs;   // cached (fireflies blink every frame)
        Vector2[] _vel;
        float[] _phase;
        Camera _cam;
        System.Random _rng = new System.Random(5);

        public void Setup(Kind kind, Camera cam)
        {
            _kind = kind; _cam = cam;
            if (kind == Kind.Calm) return;
            int n = kind == Kind.Rain ? 160 : 26;
            _bits = new Transform[n]; _srs = new SpriteRenderer[n]; _vel = new Vector2[n]; _phase = new float[n];
            float px = DungeonFx.Pixel.bounds.size.x;
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject(kind == Kind.Rain ? "Rain" : "Firefly");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                if (kind == Kind.Rain)
                {
                    sr.sprite = DungeonFx.Pixel;
                    sr.color = new Color(0.75f, 0.85f, 1f, 0.45f);
                    go.transform.localScale = new Vector3(0.05f / px, 0.55f / px, 1);
                    go.transform.rotation = Quaternion.Euler(0, 0, 12f);
                    _vel[i] = new Vector2(-3f, -16f) * (0.85f + (float)_rng.NextDouble() * 0.3f);
                }
                else
                {
                    sr.sprite = DungeonFx.Circle;
                    sr.color = new Color(1f, 0.95f, 0.5f, 0.9f);
                    go.transform.localScale = new Vector3(0.14f, 0.14f, 1);
                    _phase[i] = (float)_rng.NextDouble() * 10f;
                }
                sr.sortingOrder = 30000;
                go.transform.position = RandomInView();
                _bits[i] = go.transform; _srs[i] = sr;
            }
        }

        Vector3 RandomInView()
        {
            Vector3 c = _cam != null ? _cam.transform.position : Vector3.zero;
            float h = _cam != null ? _cam.orthographicSize : 8f, w = h * (_cam != null ? _cam.aspect : 1.8f);
            return new Vector3(c.x + ((float)_rng.NextDouble() * 2 - 1) * w * 1.1f, c.y + ((float)_rng.NextDouble() * 2 - 1) * h * 1.2f, 0);
        }

        void Update()
        {
            if (_bits == null || _cam == null) return;
            Vector3 c = _cam.transform.position;
            float h = _cam.orthographicSize, w = h * _cam.aspect;
            float dt = Time.unscaledDeltaTime, t = Time.unscaledTime;
            for (int i = 0; i < _bits.Length; i++)
            {
                var b = _bits[i];
                Vector3 p = b.position;
                if (_kind == Kind.Rain)
                {
                    p += (Vector3)(_vel[i] * dt);
                    if (p.y < c.y - h - 1f || p.x < c.x - w - 2f || p.y > c.y + h + 4f || p.x > c.x + w + 3f) p = new Vector3(c.x + ((float)_rng.NextDouble() * 2.4f - 1f) * w, c.y + h + (float)_rng.NextDouble() * 2f, 0);
                }
                else
                {
                    // fireflies drift lazily and blink
                    p += new Vector3(Mathf.Sin(t * 0.7f + _phase[i]) * 0.4f, Mathf.Cos(t * 0.5f + _phase[i] * 1.3f) * 0.3f, 0) * dt;
                    var sr = _srs[i];
                    float a = 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(t * 2.2f + _phase[i]));
                    sr.color = new Color(1f, 0.95f, 0.5f, a);
                    if (Mathf.Abs(p.x - c.x) > w + 1f || Mathf.Abs(p.y - c.y) > h + 1f) p = RandomInView();
                }
                b.position = p;
            }
        }
    }
}
