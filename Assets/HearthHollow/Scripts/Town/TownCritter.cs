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
