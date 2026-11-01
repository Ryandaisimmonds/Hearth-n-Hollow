// Hearth & Hollow — arrows (player) and orbs / fire (monsters). Stops on walls; player arrows can pierce.
using System.Collections.Generic;
using HearthHollow.Core;
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class Projectile : MonoBehaviour
    {
        DungeonController _dc;
        bool _fromPlayer;
        float _mult;        // player: damage multiplier (rolled on hit, with crits)
        float _raw;         // monster: raw damage
        bool _pierce;
        float _life = 2.5f;
        Vector2 _vel;
        readonly HashSet<EnemyController> _hit = new HashSet<EnemyController>();
        Rigidbody2D _rb;
        // boomerang (Rolling Pin): flies out, then homes back to the player, hitting everything on each pass
        float _outRange, _travelled; bool _boomerang, _returning; float _speed;
        float _spin;
        Color? _trail; float _trailT;   // glowing projectiles leave embers
        HitType _type = HitType.Pierce; int _pips = 1;   // Break (step 8)

        public Projectile SetHit(HitType type, int pips) { _type = type; _pips = pips; return this; }

        float _homing, _seekRange;
        public System.Action<Vector2> OnStop;   // called once where it stops (an enemy, a wall or the end of its flight)
        public bool FromPlayer { get { return _fromPlayer; } }
        public Projectile SetLife(float seconds) { _life = seconds; return this; }
        public Projectile Tint(Color c) { var sr = GetComponent<SpriteRenderer>(); if (sr != null) sr.color = c; _trail = c; return this; }

        /// Sunshield: a monster's shot bounces back as the player's (a Light hit).
        public void Reflect(float mult)
        {
            _fromPlayer = true; _mult = Mathf.Max(0.5f, mult); _vel = -_vel; _hit.Clear(); _type = HitType.Light; _pips = 1; _life = 2f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_vel.y, _vel.x) * Mathf.Rad2Deg);
        }

        void Stopped() { if (OnStop == null) return; var a = OnStop; OnStop = null; a(transform.position); }
        /// Turns towards the nearest enemy within <paramref name="seekRange"/> at <paramref name="degPerSec"/>.
        public Projectile Homing(float degPerSec, float seekRange) { _homing = degPerSec; _seekRange = seekRange; return this; }

        public static Projectile Spawn(DungeonController dc, bool fromPlayer, Vector2 pos, Vector2 dir, float speed,
                                       float mult, float raw, Sprite sprite, Color color, bool pierce, float scale)
        {
            if (dc == null || dc.FloorRoot == null) return null;   // floor already gone (run just ended / changing floor)
            var go = new GameObject(fromPlayer ? "Arrow" : "Orb");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            go.transform.localScale = Vector3.one * scale;
            if (dc != null && dc.FloorRoot != null) go.transform.SetParent(dc.FloorRoot.transform, true);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.color = color; sr.sortingOrder = 20000;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = fromPlayer ? 0.12f / scale : 0.16f / scale;
            var p = go.AddComponent<Projectile>();
            if (sprite == DungeonFx.Orb)
            {
                // orbs / fire: glowing, lit and trailing embers
                if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
                DungeonFx.Flash(pos, color, 2.8f, 1.6f, 0f, go.transform);
                p._trail = color;
            }
            else if (fromPlayer) DungeonFx.Flash(pos, new Color(1f, 0.95f, 0.85f), 1.2f, 0.45f, 0f, go.transform);   // a faint glint on arrows
            p._dc = dc; p._fromPlayer = fromPlayer; p._mult = mult; p._raw = raw; p._pierce = pierce;
            p._vel = dir.normalized * speed; p._rb = rb; p._speed = speed;
            rb.linearVelocity = p._vel;
            return p;
        }

        /// Makes this a boomerang that turns back after <paramref name="range"/> units.
        public Projectile Boomerang(float range, float spinDegPerSec)
        {
            _boomerang = true; _outRange = range; _pierce = true; _life = 6f; _spin = spinDegPerSec;
            return this;
        }

        void Update()
        {
            if (_dc == null || !_dc.GameplayActive) { if (_rb != null) _rb.linearVelocity = Vector2.zero; return; }
            if (_boomerang)
            {
                _travelled += _vel.magnitude * Time.deltaTime;
                if (!_returning && _travelled >= _outRange) TurnBack();
                if (_returning)
                {
                    var pl = _dc.Player;
                    if (pl == null) { Destroy(gameObject); return; }
                    Vector2 to = pl.Body - (Vector2)transform.position;
                    if (to.magnitude < 0.5f) { Destroy(gameObject); return; }
                    _vel = to.normalized * _speed * 1.15f;
                }
                if (_spin != 0f) transform.Rotate(0, 0, _spin * Time.deltaTime);
            }
            if (_homing > 0f && _fromPlayer && !_boomerang)
            {
                EnemyController best = null; float bd = _seekRange;
                foreach (var e in Combat2D.EnemiesIn(transform.position, _seekRange))
                {
                    if (e == null || e.Dead || _hit.Contains(e)) continue;
                    float d = Vector2.Distance(e.Body, transform.position);
                    if (d < bd) { bd = d; best = e; }
                }
                if (best != null)
                {
                    Vector2 want = (best.Body - (Vector2)transform.position).normalized;
                    float spd = _vel.magnitude;
                    _vel = (Vector2)Vector3.RotateTowards(_vel.normalized, want, _homing * Mathf.Deg2Rad * Time.deltaTime, 0f) * spd;
                    transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(_vel.y, _vel.x) * Mathf.Rad2Deg);
                }
            }
            if (_rb != null) _rb.linearVelocity = _vel;
            if (_fromPlayer && _dc.CurrentRoom != null && _dc.CurrentRoom.Lights != null) _dc.CurrentRoom.Lights.IgniteNear(transform.position, 0.45f);
            if (_fromPlayer && _dc.CurrentRoom != null && _dc.CurrentRoom.Secrets != null) _dc.CurrentRoom.Secrets.Hit(transform.position, 0.2f);
            if (_trail.HasValue)
            {
                _trailT -= Time.deltaTime;
                if (_trailT <= 0f) { _trailT = 0.04f; DungeonFx.Embers(transform.position, _trail.Value, 1, 0.08f); }
            }
            _life -= Time.deltaTime;
            if (_life <= 0f) { Stopped(); Destroy(gameObject); }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_dc == null || !_dc.GameplayActive) return;
            // characters are hit on their body hurt-box (a trigger) as well as their feet; anything else that's a trigger is ignored
            if (_fromPlayer)
            {
                var e = other.GetComponentInParent<EnemyController>();
                if (e != null)
                {
                    if (e.Dead || !_hit.Add(e)) return;
                    bool crit;
                    float dmg = _dc.Session.DungeonRollDamage(_mult, out crit);
                    e.TakeHit(dmg, crit, _vel.normalized);
                    if (!e.Dead) e.ShieldHit(_type, _pips);
                    if (!_pierce) Die();
                    return;
                }
                if (other.GetComponentInParent<PlayerController>() != null) return;
            }
            else
            {
                var pl = other.GetComponentInParent<PlayerController>();
                if (pl != null)
                {
                    if (!pl.CanBeHit) return;   // dashing through it
                    pl.TakeHit(_raw, transform.position);
                    Die();
                    return;
                }
                if (other.GetComponentInParent<EnemyController>() != null) return;
            }
            if (other.isTrigger) return;
            if (other.attachedRigidbody == null)
            {
                if (_boomerang) { if (!_returning) TurnBack(); return; }   // bounces off walls and comes home
                Die();
            }   // walls / pillars / hazards
        }

        void TurnBack() { _returning = true; _hit.Clear(); }

        void Die()
        {
            Stopped();
            DungeonFx.Spark(transform.position, _fromPlayer ? new Color(1, 1, 1, 0.8f) : new Color(1f, 0.6f, 0.4f, 0.8f));
            if (_trail.HasValue) { DungeonFx.Flash(transform.position, _trail.Value, 1.8f, 1f, 0.2f); DungeonFx.Embers(transform.position, _trail.Value, 5, 0.15f); }
            Destroy(gameObject);
        }
    }
}
