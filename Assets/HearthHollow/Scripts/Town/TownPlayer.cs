// Hearth & Hollow — you, walking around Hearth's Hollow. Same art and controls as in the dungeon, a bit quicker on your feet
// (DungeonSettings.townSpeedMult): WASD / arrows / left stick to walk, Space / (B) to dash, left click / J / (A) to swing
// (harmless — it just sends the town's critters flying), E / Enter / (Y) to use whatever you're next to.
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
        ClassId _class;
        Vector2 _move, _aim = Vector2.down;
        Facing _facing = Facing.Down;
        float _dashT, _dashCd, _ghostT, _attackCd;
        Vector2 _dashDir;

        public void Setup(TownController tc, ClassId cls)
        {
            _tc = tc; _class = cls;
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
            _dashT = 0f;
        }

        Vector2 Body { get { return (Vector2)transform.position + new Vector2(0, _anim != null ? _anim.FigureHeight * 0.45f : 0.4f); } }
        Color Colour { get { return PlayerController.ClassColour(_class); } }

        void Update()
        {
            float dt = Time.deltaTime;
            _dashCd -= dt; _attackCd -= dt;
            if (_dashT > 0f) _dashT -= dt;
            if (_tc == null || !_tc.Walking) { _move = Vector2.zero; _dashT = 0f; if (_anim != null) _anim.SetMoving(false); return; }
            _move = DungeonInput.Move;
            Vector2 a;
            if (DungeonInput.AimFrom(Body, out a)) _aim = a;           // mouse / right stick
            else if (_move.sqrMagnitude > 0.01f) _aim = _move.normalized;
            if (_dashT <= 0f) _facing = CharacterAnimator.FacingFrom(_aim, _facing);

            if (DungeonInput.Dash && _dashCd <= 0f) StartDash();
            if ((DungeonInput.Attack || DungeonInput.AttackHeld) && _attackCd <= 0f && _dashT <= 0f) Swing();

            if (_dashT > 0f)
            {
                _ghostT -= dt;
                if (_ghostT <= 0f)
                {
                    _ghostT = 0.04f;
                    DungeonFx.Ghost(_anim.Renderer, new Color(Colour.r, Colour.g, Colour.b, 0.45f), 0.22f, true, false);
                    DungeonFx.Embers(transform.position, Colour, 2, 0.2f);
                }
            }
            _anim.Dir = _facing;
            _anim.SetMoving(_move.sqrMagnitude > 0.01f || _dashT > 0f);
        }

        void StartDash()
        {
            var set = _tc.Settings;
            _dashDir = _move.sqrMagnitude > 0.01f ? _move.normalized : _aim;
            _dashT = set.dashSeconds * 1.3f;   // a touch longer than in the dungeon: it's for getting about
            _dashCd = Mathf.Max(0.25f, set.dashSeconds + 0.25f);
            DungeonFx.Puff(transform.position + new Vector3(0, 0.1f), new Color(0.8f, 0.74f, 0.62f, 0.55f), 6, 0.5f);
        }

        /// A friendly swing: no damage in town, but any critter in reach gets bopped into the air and flies off a little.
        void Swing()
        {
            _attackCd = 0.38f;
            Vector2 dir = _aim.sqrMagnitude > 0.01f ? _aim.normalized : CharacterAnimator.Vector(_facing);
            _facing = CharacterAnimator.FacingFrom(dir, _facing);
            _anim.Dir = _facing;
            _anim.PlayAttack(0.3f);
            Vector2 at = Body + dir * 0.85f;
            DungeonFx.SlashAt(Body + dir * 0.35f, dir, new Color(Colour.r, Colour.g, Colour.b, 0.95f), 1.3f);
            DungeonFx.Flash(at, Colour, 2.6f, 1.1f, 0.3f);
            foreach (var d in Object.FindObjectsByType<TownDummy>(FindObjectsSortMode.None))   // training dummies rock on their posts
                if (Vector2.Distance((Vector2)d.transform.position + new Vector2(0, 0.8f), at) < 1.2f) d.Hit(dir, true);
            foreach (var c in Object.FindObjectsByType<TownCritter>(FindObjectsSortMode.None))
            {
                if (c == null || c.Flying) continue;
                Vector2 to = c.Centre - at;
                if (to.magnitude > 1.15f) continue;
                Vector2 away = (c.Centre - (Vector2)transform.position);
                c.Launch(away.sqrMagnitude > 0.01f ? away.normalized : dir);
            }
        }

        void FixedUpdate()
        {
            if (_rb == null) return;
            var set = _tc != null ? _tc.Settings : null;
            float speed = set != null ? set.playerSpeed * set.townSpeedMult : 5f;
            if (_tc == null || !_tc.Walking) { _rb.linearVelocity = Vector2.zero; return; }
            _rb.linearVelocity = _dashT > 0f && set != null ? _dashDir * set.dashSpeed : _move * speed;
        }

        void LateUpdate()
        {
            if (_anim != null && _anim.Renderer != null) _anim.Renderer.sortingOrder = DungeonController.SortOrder(transform.position.y);
        }
    }
}
