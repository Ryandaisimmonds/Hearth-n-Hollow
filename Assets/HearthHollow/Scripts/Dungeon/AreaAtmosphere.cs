// Hearth & Hollow — ticket 55: each area's air. Meadow: drifting pollen and fireflies. Chili Ridge: ember sparks
// and heat shimmer. Frost Hollow: falling snow and the cook's frosty breath. The fall: rolling world-space fog
// that thickens room by room (DungeonController.Hud.Fog01), under a lighter screen veil.
// A small pool of sprites, each fading in and out over its life; nothing here touches gameplay.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public enum AirKind { None, Meadow, Chili, Frost, Fall }

    public class AreaAtmosphere : MonoBehaviour
    {
        public const int AirOrder = 24000, FogOrder = 26000;   // above the characters (SortOrder ~10000), below the UI
        const int MaxMotes = 160, FogBlobs = 16;

        class Mote
        {
            public SpriteRenderer sr; public Vector2 pos, vel; public float life, t, size, alpha, wobble, seed; public bool blink;
        }

        readonly List<Mote> _motes = new List<Mote>();
        readonly Stack<SpriteRenderer> _free = new Stack<SpriteRenderer>();
        readonly List<Mote> _fog = new List<Mote>();
        Transform _root;
        float _spawnT, _breathT;
        static Material _unlit;

        /// Which air an area has (the fall has its own; Cold hazard routes get snow).
        public static AirKind KindFor(GameContent c, string routeId)
        {
            if (routeId == GameSession.FallRoute) return AirKind.Fall;
            var r = c != null && !string.IsNullOrEmpty(routeId) ? c.routes.Find(x => x.id == routeId) : null;
            if (routeId == Ids.Area.FrostHollow || (r != null && r.hazard == Hazard.Cold)) return AirKind.Frost;
            if (routeId == Ids.Area.Chili || routeId == Ids.Area.Ember || (r != null && r.hazard == Hazard.Heat)) return AirKind.Chili;
            if (routeId == Ids.Area.Meadow || routeId == Ids.Area.Berry) return AirKind.Meadow;
            return AirKind.None;
        }

        static Material Unlit
        {
            get
            {
                if (_unlit == null) _unlit = Resources.Load<Material>("HHFx/Unlit Sprite");
                return _unlit;
            }
        }

        SpriteRenderer Take(Sprite s, int order, bool glow)
        {
            SpriteRenderer sr = _free.Count > 0 ? _free.Pop() : null;
            if (sr == null)
            {
                if (_root == null) _root = new GameObject("Area air").transform;
                var go = new GameObject("mote"); go.transform.SetParent(_root, false);
                sr = go.AddComponent<SpriteRenderer>();
            }
            sr.gameObject.SetActive(true);
            sr.sprite = s; sr.sortingOrder = order;
            var m = glow ? DungeonFx.GlowMat : Unlit;
            if (m != null) sr.sharedMaterial = m;
            return sr;
        }

        void Give(SpriteRenderer sr) { if (sr == null) return; sr.gameObject.SetActive(false); _free.Push(sr); }

        Mote Add(Sprite s, Vector2 pos, Vector2 vel, float life, float size, Color c, bool glow, int order = AirOrder)
        {
            if (_motes.Count >= MaxMotes) return null;
            var m = new Mote { sr = Take(s, order, glow), pos = pos, vel = vel, life = life, size = size, alpha = c.a, seed = Random.value * 10f };
            m.sr.color = new Color(c.r, c.g, c.b, 0f);
            m.sr.transform.position = pos; m.sr.transform.localScale = Vector3.one * size;
            _motes.Add(m);
            return m;
        }

        void Clear()
        {
            foreach (var m in _motes) Give(m.sr);
            _motes.Clear();
            foreach (var f in _fog) Give(f.sr);
            _fog.Clear();
        }

        void OnDestroy() { if (_root != null) Destroy(_root.gameObject); }

        void Update()
        {
            var dc = DungeonController.Instance;
            var cam = Camera.main;
            var s = dc != null ? dc.Session : null;
            bool on = dc != null && cam != null && s != null && s.InDungeon && s.Run != null && dc.Player != null && dc.CurrentRoom != null;
            AirKind kind = on ? KindFor(s.Content, s.Run.routeId) : AirKind.None;
            if (kind == AirKind.None) { if (_motes.Count > 0 || _fog.Count > 0) Clear(); return; }

            float dt = Time.deltaTime;   // scaled: the air freezes with pause and hit-stop
            float hh = cam.orthographicSize, hw = hh * cam.aspect;
            Vector2 c = cam.transform.position;
            Rect view = new Rect(c.x - hw, c.y - hh, hw * 2f, hh * 2f);

            if (dt > 0f) Spawn(kind, view, dc, dt);
            Step(dt, view);
            Fog(kind == AirKind.Fall && dc != null ? dc.Hud.Fog01 : 0f, view, dt);
        }

        void Spawn(AirKind kind, Rect v, DungeonController dc, float dt)
        {
            _spawnT -= dt;
            Vector2 rnd = new Vector2(Random.Range(v.xMin, v.xMax), Random.Range(v.yMin, v.yMax));
            switch (kind)
            {
                case AirKind.Meadow:
                    if (_spawnT > 0f) break;
                    _spawnT = 0.18f;
                    if (Random.value < 0.75f)   // pollen: tiny pale motes drifting on the breeze
                    {
                        var m = Add(DungeonFx.Circle, rnd, new Vector2(Random.Range(0.15f, 0.35f), Random.Range(-0.05f, 0.08f)), Random.Range(4f, 7f), Random.Range(0.12f, 0.2f), new Color(1f, 0.96f, 0.7f, 0.6f), false);
                        if (m != null) m.wobble = 0.25f;
                    }
                    else   // a firefly: a glowing speck that wanders and blinks
                    {
                        var m = Add(DungeonFx.Orb, rnd, Random.insideUnitCircle * 0.25f, Random.Range(4f, 8f), Random.Range(0.22f, 0.3f), new Color(0.85f, 1f, 0.45f, 0.95f), true);
                        if (m != null) { m.wobble = 0.6f; m.blink = true; }
                    }
                    break;
                case AirKind.Chili:
                    if (_spawnT > 0f) break;
                    _spawnT = 0.12f;
                    if (Random.value < 0.7f)   // ember sparks rising from below
                    {
                        var p = new Vector2(Random.Range(v.xMin, v.xMax), Random.Range(v.yMin - 0.5f, v.center.y));
                        var m = Add(DungeonFx.Circle, p, new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(0.6f, 1.3f)), Random.Range(1.6f, 3f), Random.Range(0.1f, 0.17f), new Color(1f, Random.Range(0.4f, 0.75f), 0.15f, 0.95f), true);
                        if (m != null) { m.wobble = 0.5f; m.blink = true; }
                    }
                    else   // heat shimmer: big faint warm swells drifting up
                    {
                        var m = Add(DungeonFx.Soft, rnd, new Vector2(0f, Random.Range(0.2f, 0.4f)), Random.Range(3f, 5f), Random.Range(2.5f, 4f), new Color(1f, 0.55f, 0.25f, 0.07f), false);
                        if (m != null) m.wobble = 0.3f;
                    }
                    break;
                case AirKind.Frost:
                    if (_spawnT <= 0f)   // snow, falling across the whole view
                    {
                        _spawnT = 0.05f;
                        var p = new Vector2(Random.Range(v.xMin - 1f, v.xMax), v.yMax + 0.3f);
                        if (Random.value < 0.35f) p = rnd;   // some start mid-air so the screen isn't empty on arrival
                        var m = Add(DungeonFx.Circle, p, new Vector2(Random.Range(0.1f, 0.4f), -Random.Range(0.6f, 1.2f)), Random.Range(5f, 8f), Random.Range(0.12f, 0.24f), new Color(0.92f, 0.97f, 1f, 0.9f), false);
                        if (m != null) m.wobble = 0.35f;
                    }
                    _breathT -= dt;
                    if (_breathT <= 0f && dc.Player != null)   // the cook's frosty breath
                    {
                        _breathT = Random.Range(1.4f, 2f);
                        Breath(dc.Player);
                    }
                    break;
            }
        }

        /// A little puff of breath in front of the face.
        void Breath(PlayerController p)
        {
            Vector2 d = p.FacingDir;
            Vector2 at = p.Body + new Vector2(0f, 0.25f) + d * 0.3f;
            for (int i = 0; i < 3; i++)
            {
                var m = Add(DungeonFx.Soft, at + Random.insideUnitCircle * 0.05f, d * Random.Range(0.4f, 0.7f) + new Vector2(0f, 0.15f), Random.Range(0.8f, 1.2f), 0.35f + i * 0.1f, new Color(0.9f, 0.96f, 1f, 0.6f), false, FogOrder - 1);
                if (m != null) m.wobble = -1f;   // grows as it fades
            }
        }

        void Step(float dt, Rect v)
        {
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                var m = _motes[i];
                m.t += dt;
                float k = m.t / m.life;
                if (k >= 1f || m.sr == null) { Give(m.sr); _motes.RemoveAt(i); continue; }
                Vector2 sway = m.wobble > 0f ? new Vector2(Mathf.Sin(m.t * 1.7f + m.seed), Mathf.Cos(m.t * 1.3f + m.seed)) * m.wobble : Vector2.zero;
                m.pos += (m.vel + sway) * dt;
                if (m.wobble < 0f) m.vel *= 1f - Mathf.Clamp01(dt * 2f);
                float a = m.alpha * Mathf.Sin(k * Mathf.PI);   // fade in, fade out
                if (m.blink) a *= 0.55f + 0.45f * Mathf.Sin(m.t * 5f + m.seed * 3f);
                var col = m.sr.color; col.a = a; m.sr.color = col;
                m.sr.transform.position = m.pos;
                if (m.wobble < 0f) m.sr.transform.localScale = Vector3.one * m.size * (1f + k * 1.5f);
            }
        }

        /// The fall's rolling fog: big soft grey-violet banks drifting across the view, as thick as Fog01.
        void Fog(float fog01, Rect v, float dt)
        {
            if (fog01 <= 0.01f && _fog.Count == 0) return;
            while (_fog.Count < FogBlobs)
            {
                var f = new Mote { sr = Take(DungeonFx.Soft, FogOrder, false), seed = Random.value * 10f };
                f.pos = new Vector2(Random.Range(v.xMin, v.xMax), Random.Range(v.yMin, v.yMax));
                f.vel = new Vector2(Random.Range(0.15f, 0.4f) * (Random.value < 0.5f ? -1f : 1f), Random.Range(-0.05f, 0.05f));
                f.size = Random.Range(5f, 9f);
                f.alpha = Random.Range(0.35f, 0.6f);
                f.sr.transform.localScale = Vector3.one * f.size;
                _fog.Add(f);
            }
            float pad = 4f;
            foreach (var f in _fog)
            {
                f.t += dt;
                f.pos += f.vel * dt * (1f + fog01);
                // wrap around the view so the banks keep rolling past
                if (f.pos.x < v.xMin - pad) f.pos.x = v.xMax + pad; else if (f.pos.x > v.xMax + pad) f.pos.x = v.xMin - pad;
                if (f.pos.y < v.yMin - pad) f.pos.y = v.yMax + pad; else if (f.pos.y > v.yMax + pad) f.pos.y = v.yMin - pad;
                f.sr.transform.position = f.pos + new Vector2(Mathf.Sin(f.t * 0.3f + f.seed), Mathf.Cos(f.t * 0.25f + f.seed)) * 0.6f;
                float breathe = 0.85f + 0.15f * Mathf.Sin(f.t * 0.5f + f.seed);
                f.sr.color = new Color(0.62f, 0.56f, 0.72f, f.alpha * fog01 * breathe);
            }
            if (fog01 <= 0.01f) { foreach (var f in _fog) Give(f.sr); _fog.Clear(); }
        }
    }
}
