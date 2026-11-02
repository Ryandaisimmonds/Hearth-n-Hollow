// Hearth & Hollow — a lingering area on the floor (arrow rain, hot-pot puddle, picnic blanket, snare, butter trail).
// Ticks a callback every `interval` seconds while the dungeon is running; fades out and removes itself.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class DungeonZone : MonoBehaviour
    {
        public float Radius { get; private set; }
        public Vector2 Centre { get { return transform.position; } }

        DungeonController _dc;
        float _life, _t, _interval, _tickT;
        Action<DungeonZone> _onTick;
        SpriteRenderer _sr;
        Color _color;

        /// Spawns a zone. <paramref name="interval"/> ≤ 0 ticks every frame. The visual is a soft disc of <paramref name="color"/>.
        public static DungeonZone Spawn(DungeonController dc, Vector2 at, float radius, float life, float interval, Color color, Action<DungeonZone> onTick)
        {
            if (dc == null || dc.FloorRoot == null) return null;
            var go = new GameObject("Zone");
            go.transform.SetParent(dc.FloorRoot.transform, true);
            go.transform.position = at;
            var z = go.AddComponent<DungeonZone>();
            z._dc = dc; z.Radius = radius; z._life = life; z._interval = interval; z._onTick = onTick; z._color = color;
            z._sr = go.AddComponent<SpriteRenderer>();
            z._sr.sprite = DungeonFx.Circle;          // 16 px at 32 PPU = 0.5 units across
            z._sr.color = color;
            z._sr.sortingOrder = 1;                   // on the floor, under characters
            go.transform.localScale = Vector3.one * (radius * 4f);
            return z;
        }

        void Update()
        {
            if (_dc == null || !_dc.GameplayActive) return;
            float dt = Time.deltaTime;
            _t += dt;
            if (_onTick != null)
            {
                if (_interval <= 0f) _onTick(this);
                else
                {
                    _tickT -= dt;
                    while (_tickT <= 0f && _onTick != null) { _tickT += _interval; _onTick(this); }
                }
            }
            float fade = Mathf.Clamp01((_life - _t) / 0.4f);
            if (_sr != null) _sr.color = new Color(_color.r, _color.g, _color.b, _color.a * fade);
            if (_t >= _life) Destroy(gameObject);
        }

        /// Ends the zone now (e.g. a snare that has sprung).
        public void End() { _onTick = null; _life = Mathf.Min(_life, _t + 0.3f); }

        /// Living enemies whose feet or body are inside the zone.
        public List<EnemyController> Enemies() { return Combat2D.EnemiesIn(Centre, Radius); }
    }

    /// Shared hit-finding helpers for player moves.
    public static class Combat2D
    {
        // one shared buffer for the physics query (homing arrows and zones call this every frame); the result list is new each
        // call, so callers can loop over it while hitting things that call this again
        static readonly List<Collider2D> _hitBuf = new List<Collider2D>(64);

        public static List<EnemyController> EnemiesIn(Vector2 centre, float radius)
        {
            var list = new List<EnemyController>();
            var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };   // same colliders as OverlapCircleAll
            _hitBuf.Clear();
            Physics2D.OverlapCircle(centre, radius, filter, _hitBuf);
            foreach (var h in _hitBuf)
            {
                var e = h.GetComponentInParent<EnemyController>();
                if (e != null && !e.Dead && !e.Spawning && !list.Contains(e)) list.Add(e);
            }
            return list;
        }

        /// Enemies within <paramref name="range"/> of <paramref name="from"/> and within <paramref name="halfAngle"/>° of <paramref name="dir"/>.
        public static List<EnemyController> EnemiesInCone(Vector2 from, Vector2 dir, float range, float halfAngle)
        {
            var list = new List<EnemyController>();
            foreach (var e in EnemiesIn(from, range))
            {
                Vector2 to = (Vector2)e.transform.position - from;
                if (to.sqrMagnitude < 0.36f || Vector2.Angle(dir, to) <= halfAngle) list.Add(e);
            }
            return list;
        }

        public static EnemyController Nearest(Vector2 from, float range)
        {
            EnemyController best = null; float bd = range;
            foreach (var e in EnemiesIn(from, range))
            {
                float d = Vector2.Distance(from, e.transform.position);
                if (d < bd) { bd = d; best = e; }
            }
            return best;
        }
    }
}
