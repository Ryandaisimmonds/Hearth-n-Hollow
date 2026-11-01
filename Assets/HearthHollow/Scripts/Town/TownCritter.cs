// Hearth & Hollow — little creatures pottering around Hearth's Hollow (slimes, bees, piglets, frogs by the pond).
// Purely for life: they wander between random spots, stop for a sniff, and scurry off if you walk right up to them.
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public class TownCritter : MonoBehaviour
    {
        TownController _tc;
        CharacterAnimator _anim;
        Vector2 _home, _target;
        float _speed, _waitUntil, _range;
        Facing _facing = Facing.Down;
        System.Random _rng;
        // bopped by the player's swing: a hop through the air, a spin, a bump on landing
        float _flyT, _flyDur; Vector2 _flyFrom, _flyTo; float _spin; Vector3 _bodyLocal;
        public bool Flying { get { return _flyT > 0f; } }
        public Vector2 Centre { get { return (Vector2)transform.position + new Vector2(0, 0.3f); } }

        /// Sent flying away from the player's swing (`dir` = away), landing a couple of tiles off on walkable ground.
        public void Launch(Vector2 dir)
        {
            if (_anim == null) return;
            Vector2 pos = transform.position;
            float dist = 2.2f + (float)_rng.NextDouble() * 1.2f;
            Vector2 d = ((Vector2)(Quaternion.Euler(0, 0, (float)(_rng.NextDouble() * 40 - 20)) * dir)).normalized;
            Vector2 to = pos;
            for (float f = dist; f > 0.3f; f -= 0.3f) { Vector2 t = pos + d * f; if (TownLayout.IsWalkable(t.x, t.y)) { to = t; break; } }
            if (_flyT <= 0f) _bodyLocal = _anim.transform.localPosition;   // the sprite's own offset (feet on the ground)
            _flyFrom = pos; _flyTo = to;
            _flyDur = 0.55f + (float)_rng.NextDouble() * 0.2f; _flyT = _flyDur;
            _spin = (d.x >= 0f ? -1f : 1f) * (500f + (float)_rng.NextDouble() * 300f);
            DungeonFx.Puff(pos + new Vector2(0, 0.15f), new Color(0.85f, 0.8f, 0.7f, 0.6f), 6, 0.35f);
            DungeonFx.Sparks(Centre, d, new Color(1f, 0.9f, 0.6f), 6);
            _target = to; _waitUntil = Time.time + _flyDur + 0.4f;   // then a moment to shake it off
        }

        void FlyUpdate()
        {
            _flyT -= Time.deltaTime;
            float k = 1f - Mathf.Clamp01(_flyT / Mathf.Max(0.01f, _flyDur));
            transform.position = Vector2.Lerp(_flyFrom, _flyTo, k);
            _anim.transform.localPosition = _bodyLocal + new Vector3(0, Mathf.Sin(k * Mathf.PI) * 1.3f, 0);   // the arc
            _anim.transform.localRotation = Quaternion.Euler(0, 0, _spin * k * _flyDur);
            if (_flyT <= 0f)
            {
                _anim.transform.localPosition = _bodyLocal; _anim.transform.localRotation = Quaternion.identity;
                transform.position = _flyTo;
                DungeonFx.Puff(_flyTo + new Vector2(0, 0.1f), new Color(0.8f, 0.74f, 0.62f, 0.6f), 8, 0.45f);   // bump!
            }
        }

        public void Setup(TownController tc, HearthHollow.Data.ArtLibrary.Anim art, float size, Color tint, Vector2 home, float range, int seed)
        {
            _tc = tc; _home = home; _range = range;
            _rng = new System.Random(seed);
            _speed = 0.7f + (float)_rng.NextDouble() * 0.8f;
            transform.position = home;
            _target = home;
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.AddComponent<SpriteRenderer>();
            _anim = body.AddComponent<CharacterAnimator>();
            _anim.Setup(art, tc.Settings.characterPixelsPerUnit, size);
            if (_anim.Renderer != null) _anim.Renderer.color = tint;
            _waitUntil = Time.time + (float)_rng.NextDouble() * 2f;
        }

        void Update()
        {
            if (_tc == null || _anim == null) return;
            if (_flyT > 0f) { _anim.SetMoving(false); FlyUpdate(); return; }
            Vector2 pos = transform.position;
            // scurry away from the player
            if (_tc.Player != null)
            {
                Vector2 away = pos - (Vector2)_tc.Player.transform.position;
                if (away.sqrMagnitude < 1.6f * 1.6f && away.sqrMagnitude > 0.0001f)
                {
                    Vector2 t = pos + away.normalized * 2.2f;
                    if (TownLayout.IsWalkable(t.x, t.y)) { _target = t; _waitUntil = 0f; }
                }
            }
            if (Time.time < _waitUntil) { _anim.SetMoving(false); return; }
            Vector2 to = _target - pos;
            if (to.magnitude < 0.05f)
            {
                _anim.SetMoving(false);
                _waitUntil = Time.time + 0.8f + (float)_rng.NextDouble() * 2.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 t = _home + new Vector2((float)(_rng.NextDouble() * 2 - 1), (float)(_rng.NextDouble() * 2 - 1)) * _range;
                    if (TownLayout.IsWalkable(t.x, t.y)) { _target = t; break; }
                }
                return;
            }
            Vector2 step = to.normalized * _speed * Time.deltaTime;
            if (step.magnitude > to.magnitude) step = to;
            Vector2 next = pos + step;
            if (!TownLayout.IsWalkable(next.x, next.y)) { _target = pos; return; }
            transform.position = next;
            _facing = CharacterAnimator.FacingFrom(step, _facing);
            _anim.Dir = _facing;
            _anim.SetMoving(true);
        }

        void LateUpdate()
        {
            if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y);
        }
    }
}
