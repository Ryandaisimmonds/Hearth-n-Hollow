// Hearth & Hollow — you, walking around Hearth's Hollow. Same art, speed and controls as in the dungeon,
// but no combat: WASD / arrows / left stick to walk, E / Enter / (Y) to use whatever you're next to.
using HearthHollow.Core;
using HearthHollow.Dungeon;
using UnityEngine;

namespace HearthHollow.Town
{
    public class TownPlayer : MonoBehaviour
    {
        TownController _tc;
        Rigidbody2D _rb;
        CharacterAnimator _anim;
        Vector2 _move;
        Facing _facing = Facing.Down;

        public void Setup(TownController tc, ClassId cls)
        {
            _tc = tc;
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale = 0f; _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.28f; col.offset = new Vector2(0, 0.2f);

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.AddComponent<SpriteRenderer>();
            _anim = body.AddComponent<CharacterAnimator>();
            _anim.Setup(HearthHollow.Data.ArtLibrary.GetClass(cls.ToString()), tc.Settings.characterPixelsPerUnit, 1f);
        }

        public void Teleport(Vector2 pos)
        {
            transform.position = pos;
            if (_rb != null) { _rb.position = pos; _rb.linearVelocity = Vector2.zero; }
        }

        void Update()
        {
            if (_tc == null || !_tc.Walking) { _move = Vector2.zero; if (_anim != null) _anim.SetMoving(false); return; }
            _move = DungeonInput.Move;
            if (_move.sqrMagnitude > 0.01f) _facing = CharacterAnimator.FacingFrom(_move, _facing);
            _anim.Dir = _facing;
            _anim.SetMoving(_move.sqrMagnitude > 0.01f);
        }

        void FixedUpdate()
        {
            if (_rb == null) return;
            float speed = _tc != null && _tc.Settings != null ? _tc.Settings.playerSpeed : 5f;
            _rb.linearVelocity = _tc != null && _tc.Walking ? _move * speed : Vector2.zero;
        }

        void LateUpdate()
        {
            if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y);
        }
    }
}
