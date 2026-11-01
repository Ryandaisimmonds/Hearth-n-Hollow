// Hearth & Hollow — light sources in a dungeon room (light plan step 4).
//   • Braziers by the walls: cold in a fight, lit by your fire hits (swings, skills, arrows passing through) or a torch;
//     every brazier flares on in a wave from the last kill when the room is cleared, and in calm rooms when the room lights up.
//   • The route's own glow: fireflies (meadows, glades), glowing mushrooms (caves), ice crystals (frost), lava cracks (ember, chili).
//   • Thrown torches (T / LB, unlimited on a cooldown) — see ThrownTorch.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public enum RoomGlow { None, Fireflies, Mushrooms, Crystals, LavaCracks }

    public class RoomLights : MonoBehaviour
    {
        public RoomInstance Room { get; private set; }
        public readonly List<Brazier> Braziers = new List<Brazier>();
        System.Random _rng;
        readonly List<Vector2> _used = new List<Vector2>();

        public void Setup(RoomInstance room, DungeonTheme theme, int seed)
        {
            Room = room;
            _rng = new System.Random(seed * 31 + 7);
            foreach (var p in room.Props) if (p != null) _used.Add(p.transform.position);
            _used.Add(room.Origin + RoomTemplate.Centre);
            var edge = EdgeCells();

            // braziers: 2–4 by the walls
            int nb = 2 + _rng.Next(3);
            for (int i = 0; i < nb; i++)
            {
                Vector2 at; if (!Take(edge, 4.5f, out at)) break;
                var go = new GameObject("Brazier");
                go.transform.SetParent(transform, false);
                go.transform.position = at;
                var b = go.AddComponent<Brazier>();
                b.Setup();
                Braziers.Add(b);
            }

            // the route's own glow
            if (theme != null && theme.ambient != RoomGlow.None)
            {
                if (theme.ambient == RoomGlow.Fireflies) Fireflies(theme.ambientColor);
                else
                {
                    int n = 2 + _rng.Next(2);
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 at; if (!Take(edge, 3.5f, out at)) break;
                        if (theme.ambient == RoomGlow.Mushrooms) Mushrooms(at, theme.ambientColor);
                        else if (theme.ambient == RoomGlow.Crystals) Crystals(at, theme.ambientColor);
                        else LavaCrack(at, theme.ambientColor);
                    }
                }
            }
        }

        // ───────── lighting the braziers ─────────
        /// Fire reached <paramref name="p"/>: light any cold brazier within <paramref name="radius"/>.
        public void IgniteNear(Vector2 p, float radius, bool puzzle = true)
        {
            foreach (var b in Braziers) if (b != null && !b.Lit && (puzzle || !b.Puzzle) && Vector2.Distance(b.Base, p) <= radius + 0.35f) b.Ignite(0f);
        }

        /// Every cold brazier flares on in turn, nearest to <paramref name="from"/> first, after <paramref name="delay"/> seconds.
        public void IgniteWave(Vector2 from, float delay = 0f)
        {
            foreach (var b in Braziers) if (b != null && !b.Lit && !b.Puzzle) b.Ignite(delay + 0.15f + Vector2.Distance(b.Base, from) * 0.09f);   // puzzle braziers are yours to light
        }

        /// One more brazier by a wall (brazier puzzles want at least three). Null if there's no room for it.
        public Brazier AddBrazier(float minGap)
        {
            Vector2 at; if (!Take(EdgeCells(), minGap, out at)) return null;
            var go = new GameObject("Brazier");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var b = go.AddComponent<Brazier>();
            b.Setup();
            Braziers.Add(b);
            return b;
        }

        // ───────── placement ─────────
        /// Walkable cells next to a wall, away from the doors.
        List<Vector2> EdgeCells()
        {
            var list = new List<Vector2>();
            for (int y = 1; y <= RoomTemplate.Height - 3; y++)
                for (int x = 1; x <= RoomTemplate.Width - 2; x++)
                {
                    var c = new Vector3Int(x, y, 0);
                    if (!Room.IsWalkable(c) || Room.NearDoor(c)) continue;
                    bool wall = !Room.IsWalkable(new Vector3Int(x - 1, y, 0)) || !Room.IsWalkable(new Vector3Int(x + 1, y, 0))
                             || !Room.IsWalkable(new Vector3Int(x, y - 1, 0)) || !Room.IsWalkable(new Vector3Int(x, y + 1, 0));
                    if (wall) list.Add(Room.Origin + new Vector2(x + 0.5f, y + 0.35f));
                }
            for (int i = list.Count - 1; i > 0; i--) { int j = _rng.Next(i + 1); var t = list[i]; list[i] = list[j]; list[j] = t; }
            return list;
        }

        bool Take(List<Vector2> cells, float minGap, out Vector2 at)
        {
            foreach (var c in cells)
            {
                bool ok = true;
                foreach (var u in _used) if (Vector2.Distance(u, c) < minGap) { ok = false; break; }
                if (!ok) continue;
                _used.Add(c); at = c; return true;
            }
            at = Vector2.zero; return false;
        }

        // ───────── the route's own glow ─────────
        static SpriteRenderer Glow(Transform parent, Sprite s, Vector2 local, Vector2 size, Color c, int order, float rot = 0f)
        {
            var fx = new GameObject("Glow");
            fx.transform.SetParent(parent, false);
            fx.transform.localPosition = local;
            fx.transform.localRotation = Quaternion.Euler(0, 0, rot);
            var b = s.bounds.size;
            fx.transform.localScale = new Vector3(size.x / Mathf.Max(0.0001f, b.x), size.y / Mathf.Max(0.0001f, b.y), 1f);
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
            return sr;
        }

        void Mushrooms(Vector2 at, Color col)
        {
            var root = new GameObject("Glowing mushrooms").transform;
            root.SetParent(transform, false); root.position = at;
            int o = DungeonController.SortOrder(at.y);
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector2(-0.22f + i * 0.2f + (float)_rng.NextDouble() * 0.08f, (float)_rng.NextDouble() * 0.12f);
                float s = 0.7f + (float)_rng.NextDouble() * 0.5f;
                var stem = new GameObject("Stem"); stem.transform.SetParent(root, false); stem.transform.localPosition = p + new Vector2(0, 0.06f * s);
                stem.transform.localScale = new Vector3(0.05f * s / DungeonFx.Pixel.bounds.size.x, 0.13f * s / DungeonFx.Pixel.bounds.size.y, 1f);
                var ss = stem.AddComponent<SpriteRenderer>(); ss.sprite = DungeonFx.Pixel; ss.color = new Color(0.85f, 0.82f, 0.72f); ss.sortingOrder = o;
                Glow(root, DungeonFx.Circle, p + new Vector2(0, 0.14f * s), new Vector2(0.2f * s, 0.12f * s), col, o + 1);
            }
            var l = DungeonFx.Flash(at + new Vector2(0, 0.2f), col, 2.6f, 1f, 0f, root);
            root.gameObject.AddComponent<Pulse>().Setup(l, 0.8f, 0.2f, (float)_rng.NextDouble() * 6f);
        }

        void Crystals(Vector2 at, Color col)
        {
            var root = new GameObject("Ice crystals").transform;
            root.SetParent(transform, false); root.position = at;
            int o = DungeonController.SortOrder(at.y);
            for (int i = 0; i < 3; i++)
            {
                float h = 0.3f + (float)_rng.NextDouble() * 0.3f;
                Glow(root, DungeonFx.Pixel, new Vector2(-0.18f + i * 0.18f, h * 0.5f), new Vector2(0.11f, h), col, o + 1, -18f + i * 18f + (float)_rng.NextDouble() * 8f);
            }
            var l = DungeonFx.Flash(at + new Vector2(0, 0.3f), col, 3f, 1.05f, 0f, root);
            root.gameObject.AddComponent<Pulse>().Setup(l, 1.4f, 0.12f, (float)_rng.NextDouble() * 6f);
        }

        void LavaCrack(Vector2 at, Color col)
        {
            var root = new GameObject("Lava crack").transform;
            root.SetParent(transform, false); root.position = at;
            Vector2 p = Vector2.zero; float ang = (float)_rng.NextDouble() * 360f;
            for (int i = 0; i < 4; i++)
            {
                float len = 0.35f + (float)_rng.NextDouble() * 0.35f;
                ang += -40f + (float)_rng.NextDouble() * 80f;
                Vector2 d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                Glow(root, DungeonFx.Pixel, p + d * len * 0.5f, new Vector2(len, 0.05f), col, 6, ang);
                p += d * len;
            }
            var l = DungeonFx.Flash(at + new Vector2(0, 0.1f), col, 2.8f, 1.15f, 0f, root);
            root.gameObject.AddComponent<Pulse>().Setup(l, 2.2f, 0.25f, (float)_rng.NextDouble() * 6f);
        }

        void Fireflies(Color col)
        {
            int n = 6 + _rng.Next(4);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Firefly");
                go.transform.SetParent(transform, false);
                var f = go.AddComponent<Firefly>();
                f.Setup(Room, col, i < 3, _rng.Next());
            }
        }
    }

    /// A brazier by the wall: cold until fire reaches it, then a flickering flame that lights (and shadows) the room.
    public class Brazier : MonoBehaviour
    {
        public bool Lit { get; private set; }
        public Vector2 Base { get { return transform.position; } }
        /// Part of a brazier puzzle (step 7): never lit by the room, only by you; may burn out after _burn seconds.
        public bool Puzzle { get; private set; }
        float _burn, _litAt; SpriteRenderer _rune;
        static readonly Color Rune = new Color(0.45f, 0.7f, 1f, 0.55f);

        public void MakePuzzle(float burnSeconds)
        {
            Puzzle = true; _burn = burnSeconds;
            var g = new GameObject("Rune"); g.transform.SetParent(transform, false); g.transform.localPosition = new Vector3(0, 0.02f, 0);
            var b = DungeonFx.Circle.bounds.size;
            g.transform.localScale = new Vector3(0.85f / b.x, 0.32f / b.y, 1f);
            _rune = g.AddComponent<SpriteRenderer>(); _rune.sprite = DungeonFx.Circle; _rune.color = Rune;
            _rune.sortingOrder = DungeonController.SortOrder(transform.position.y) - 1;
            if (DungeonFx.GlowMat != null) _rune.sharedMaterial = DungeonFx.GlowMat;
        }

        /// The puzzle is solved: stay lit for good.
        public void Keep() { _burn = 0f; if (_rune != null) _rune.color = new Color(1f, 0.8f, 0.4f, 0.6f); }

        void Extinguish()
        {
            Lit = false; _igniteAt = -1f;
            if (_flame != null) Destroy(_flame.gameObject);
            if (_core != null) Destroy(_core.gameObject);
            if (_light != null) Destroy(_light.gameObject);
            _flame = _core = null; _light = null;
            DungeonFx.Puff((Vector2)transform.position + new Vector2(0, 0.55f), new Color(0.35f, 0.35f, 0.38f, 0.8f), 8, 0.4f);
        }
        SpriteRenderer _flame, _core; Light2D _light;
        float _igniteAt = -1f, _seed, _emberT;
        static readonly Color Fire = new Color(1f, 0.62f, 0.28f);

        public void Setup()
        {
            _seed = Random.value * 10f;
            int o = DungeonController.SortOrder(transform.position.y);
            Color iron = new Color(0.2f, 0.18f, 0.17f), iron2 = new Color(0.32f, 0.29f, 0.27f);
            Box(new Vector2(-0.16f, 0.15f), new Vector2(0.06f, 0.3f), iron, o);
            Box(new Vector2(0.16f, 0.15f), new Vector2(0.06f, 0.3f), iron, o);
            Box(new Vector2(0f, 0.34f), new Vector2(0.5f, 0.14f), iron2, o + 1);
            Box(new Vector2(0f, 0.42f), new Vector2(0.58f, 0.05f), new Color(0.42f, 0.38f, 0.34f), o + 1);
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.22f; col.offset = new Vector2(0, 0.15f);
        }

        void Box(Vector2 local, Vector2 size, Color c, int order)
        {
            var g = new GameObject("Part"); g.transform.SetParent(transform, false); g.transform.localPosition = local;
            var b = DungeonFx.Pixel.bounds.size;
            g.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Pixel; sr.color = c; sr.sortingOrder = order;
        }

        public void Ignite(float delay)
        {
            if (Lit || (_igniteAt >= 0f && _igniteAt <= Time.time + delay)) return;
            _igniteAt = Time.time + delay;
        }

        void LightUp()
        {
            Lit = true; _litAt = Time.time;
            int o = DungeonController.SortOrder(transform.position.y) + 2;
            _flame = Flame(new Vector2(0, 0.42f), Fire, o, 1f);
            _core = Flame(new Vector2(0, 0.42f), Color.Lerp(Fire, Color.white, 0.6f), o + 1, 0.55f);
            _light = DungeonFx.Flash((Vector2)transform.position + new Vector2(0, 0.7f), Fire, 4.8f, 1.35f, 0f, transform);
            DungeonController.CastShadows(_light, 0.75f, 0.45f);
            // whoomph
            DungeonFx.Flash((Vector2)transform.position + new Vector2(0, 0.6f), Fire, 6f, 2f, 0.5f);
            DungeonFx.Embers((Vector2)transform.position + new Vector2(0, 0.5f), Fire, 12, 0.2f);
            DungeonFx.Sparks((Vector2)transform.position + new Vector2(0, 0.5f), Vector2.up, Fire, 6);
        }

        SpriteRenderer Flame(Vector2 local, Color c, int order, float k)
        {
            var g = new GameObject("Flame"); g.transform.SetParent(transform, false); g.transform.localPosition = local;
            g.transform.localScale = new Vector3(0.9f * k, 0.9f * k, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Flame; sr.color = c; sr.sortingOrder = order;
            if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
            return sr;
        }

        void Update()
        {
            if (!Lit && _igniteAt >= 0f && Time.time >= _igniteAt) LightUp();
            if (!Lit) return;
            float t = Time.time + _seed;
            float f = 1f + 0.07f * Mathf.Sin(t * 9f) + 0.05f * Mathf.Sin(t * 23f);
            if (_flame != null) _flame.transform.localScale = new Vector3(0.9f * (2f - f), 0.9f * f, 1f);
            if (_core != null) _core.transform.localScale = new Vector3(0.5f * (2f - f), 0.5f * f, 1f);
            float life = 1f;
            if (Puzzle && _burn > 0f)   // a timed puzzle brazier: gutters over its last 4 seconds, then goes out
            {
                float left = _burn - (Time.time - _litAt);
                if (left <= 0f) { Extinguish(); return; }
                life = Mathf.Clamp01(left / 4f) * 0.7f + 0.3f;
                if (left < 4f) f *= 0.85f + 0.15f * Mathf.Sin(Time.time * 30f);
                if (_rune != null) _rune.color = new Color(Rune.r, Rune.g, Rune.b, Rune.a * (0.4f + 0.6f * Mathf.Clamp01(left / _burn)));
            }
            if (_flame != null) _flame.transform.localScale *= life;
            if (_core != null) _core.transform.localScale *= life;
            if (_light != null) _light.intensity = 1.35f * f * life;
            _emberT -= Time.deltaTime;
            if (_emberT <= 0f) { _emberT = Random.Range(0.25f, 0.6f); DungeonFx.Embers((Vector2)transform.position + new Vector2(0, 0.6f), Fire, 1, 0.12f); }
        }
    }

    /// Slowly breathes a light's intensity (mushrooms, crystals, lava).
    public class Pulse : MonoBehaviour
    {
        Light2D _l; float _base, _speed, _amount, _seed;
        public void Setup(Light2D l, float speed, float amount, float seed) { _l = l; _base = l != null ? l.intensity : 1f; _speed = speed; _amount = amount; _seed = seed; }
        void Update() { if (_l != null) _l.intensity = _base * (1f + _amount * Mathf.Sin(Time.time * _speed + _seed)); }
    }

    /// A firefly drifting round the room; some carry a little light.
    public class Firefly : MonoBehaviour
    {
        RoomInstance _room; SpriteRenderer _sr; Light2D _l; Vector2 _target; System.Random _rng; float _seed; Color _c;
        public void Setup(RoomInstance room, Color c, bool light, int seed)
        {
            _room = room; _rng = new System.Random(seed); _seed = (float)_rng.NextDouble() * 10f; _c = c;
            transform.position = Pick();
            _target = Pick();
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = DungeonFx.Pixel; _sr.color = c; _sr.sortingOrder = 21000;
            transform.localScale = new Vector3(3f, 3f, 1f);
            if (DungeonFx.GlowMat != null) _sr.sharedMaterial = DungeonFx.GlowMat;
            if (light) _l = DungeonFx.Flash(transform.position, c, 1.4f, 0.8f, 0f, transform);
        }
        Vector2 Pick() { return _room.Origin + new Vector2(1.5f + (float)_rng.NextDouble() * (RoomTemplate.Width - 3f), 1.5f + (float)_rng.NextDouble() * (RoomTemplate.Height - 5f)); }
        void Update()
        {
            Vector2 p = transform.position;
            if (Vector2.Distance(p, _target) < 0.2f) _target = Pick();
            p = Vector2.MoveTowards(p, _target, Time.deltaTime * 0.6f);
            p.y += Mathf.Sin(Time.time * 3f + _seed) * 0.003f;
            transform.position = p;
            float a = 0.35f + 0.65f * Mathf.Max(0f, Mathf.Sin(Time.time * 1.7f + _seed));
            _sr.color = new Color(_c.r, _c.g, _c.b, a);
            if (_l != null) _l.intensity = 0.8f * a;
        }
    }

    /// A thrown torch: arcs to where you aim (stopping short of walls), lands and burns for a while,
    /// lighting that corner (and any brazier it lands next to).
    public class ThrownTorch : MonoBehaviour
    {
        Vector2 _from, _to; float _t, _flight = 0.35f, _burn, _left; bool _landed;
        SpriteRenderer _flame, _stick; Light2D _light; RoomInstance _room; float _emberT;
        static readonly Color Fire = new Color(1f, 0.6f, 0.25f);

        public static void Throw(DungeonController dc, Vector2 from, Vector2 dir, float range, float seconds)
        {
            var room = dc.CurrentRoom;
            if (room == null) return;
            Vector2 d = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.down;
            Vector2 to = from;
            var hazard = dc.Theme != null ? dc.Theme.hazard : null;
            for (float s = 0.25f; s <= range; s += 0.25f)   // flies over pits and water, stops at walls; lands on the last open floor
            {
                Vector2 p = from + d * s;
                var c = new Vector3Int(Mathf.FloorToInt(p.x - room.Origin.x), Mathf.FloorToInt(p.y - room.Origin.y), 0);
                if (room.IsWalkable(c)) { to = p; continue; }
                var wt = room.Template.walls != null ? room.Template.walls.GetTile(c) : null;
                if (wt == null || wt != hazard) break;   // a wall
            }
            var go = new GameObject("Thrown torch");
            go.transform.SetParent(room.transform, true);
            go.transform.position = from;
            var t = go.AddComponent<ThrownTorch>();
            t._from = from; t._to = to; t._burn = seconds; t._left = seconds; t._room = room;
            t.Build();
        }

        void Build()
        {
            var b = DungeonFx.Pixel.bounds.size;
            var stick = new GameObject("Stick"); stick.transform.SetParent(transform, false);
            stick.transform.localScale = new Vector3(0.07f / b.x, 0.42f / b.y, 1f);
            _stick = stick.AddComponent<SpriteRenderer>(); _stick.sprite = DungeonFx.Pixel; _stick.color = new Color(0.5f, 0.33f, 0.18f);
            var fl = new GameObject("Flame"); fl.transform.SetParent(transform, false); fl.transform.localPosition = new Vector3(0, 0.22f, 0);
            fl.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            _flame = fl.AddComponent<SpriteRenderer>(); _flame.sprite = DungeonFx.Flame; _flame.color = Fire;
            if (DungeonFx.GlowMat != null) _flame.sharedMaterial = DungeonFx.GlowMat;
            _light = DungeonFx.Flash((Vector2)transform.position + new Vector2(0, 0.4f), Fire, 5.5f, 1.45f, 0f, transform);
            DungeonController.CastShadows(_light, 0.75f, 0.45f);
        }

        void Update()
        {
            var dc = DungeonController.Instance;
            if (dc == null || !dc.GameplayActive) return;
            float dt = Time.deltaTime;
            if (!_landed)
            {
                _t += dt;
                float k = Mathf.Clamp01(_t / _flight);
                Vector2 p = Vector2.Lerp(_from, _to, k) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * 0.8f);
                transform.position = p;
                transform.rotation = Quaternion.Euler(0, 0, -k * 540f);
                if (k >= 1f)
                {
                    _landed = true;
                    transform.rotation = Quaternion.Euler(0, 0, -20f);
                    DungeonFx.Sparks(transform.position, Vector2.up, Fire, 8);
                    DungeonFx.Embers(transform.position, Fire, 10, 0.25f);
                    if (_room != null && _room.Lights != null) _room.Lights.IgniteNear(transform.position, 1.6f);
                    foreach (var e in Combat2D.EnemiesIn(transform.position, 1.6f)) if (e != null) e.ShieldHit(HitType.Fire, 1);   // a fire hit for Break (no damage)
                }
            }
            else
            {
                _left -= dt;
                float fade = Mathf.Clamp01(_left / 2f);   // dies down over the last two seconds
                float t = Time.time;
                float f = 1f + 0.08f * Mathf.Sin(t * 11f) + 0.05f * Mathf.Sin(t * 27f);
                if (_light != null) _light.intensity = 1.45f * f * fade;
                if (_flame != null) { _flame.transform.localScale = new Vector3(0.55f * (2f - f), 0.55f * f, 1f) * Mathf.Max(0.2f, fade); }
                _emberT -= dt;
                if (_emberT <= 0f && fade > 0.3f) { _emberT = Random.Range(0.2f, 0.45f); DungeonFx.Embers((Vector2)transform.position + new Vector2(0, 0.3f), Fire, 1, 0.1f); }
                if (_left <= 0f) Destroy(gameObject);
            }
            int o = DungeonController.SortOrder(transform.position.y);
            if (_stick != null) _stick.sortingOrder = o; if (_flame != null) _flame.sortingOrder = o + 1;
        }
    }
}
