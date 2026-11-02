// Hearth & Hollow — keeps a shadow-casting light working when its centre ends up inside a wall, pillar or rock.
// A Light2D whose centre sits inside a ShadowCaster2D's shape treats everything as 'behind the wall' and goes dark.
//   • Lights that stay put (braziers, lanterns, fires): if one was placed inside a shadow caster, it's nudged to the
//     nearest clear spot (down / in front of the wall first) during its first second, so it keeps its shadows.
//   • Anything that moves (your carried light, enemy lanterns, thrown torches): its shadows fade out while its centre
//     is inside a caster, and back in once it's clear.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    [RequireComponent(typeof(Light2D))]
    public class ShadowSafeLight : MonoBehaviour
    {
        const float SettleTime = 1f;      // how long a still light may be nudged after it appears
        const float MaxNudge = 1.2f;      // never moved further than this
        const float Clearance = 0.08f;    // how far from the caster's edge a nudged light ends up

        Light2D _light;
        float _base;   // the shadow strength it was set up with
        float _settleUntil;
        Vector3 _anchorStart;   // where the thing carrying the light started (to tell still lights from moving ones)
        bool _settled;

        static readonly List<Collider2D> _buf = new List<Collider2D>(8);
        static readonly Vector2[] Dirs =
        {
            new Vector2(0, -1), new Vector2(-0.7071f, -0.7071f), new Vector2(0.7071f, -0.7071f),
            new Vector2(-1, 0), new Vector2(1, 0),
            new Vector2(-0.7071f, 0.7071f), new Vector2(0.7071f, 0.7071f), new Vector2(0, 1)
        };

        Transform Anchor { get { return transform.parent != null ? transform.parent : transform; } }

        void Start()
        {
            _light = GetComponent<Light2D>();
            _base = _light != null ? _light.shadowIntensity : 0f;
            _settleUntil = Time.time + SettleTime;
            _anchorStart = Anchor.position;
            // carried by something with physics (the player, enemies): it moves, so never nudge it
            _settled = GetComponentInParent<Rigidbody2D>() != null;
        }

        void LateUpdate()
        {
            if (_light == null || !_light.shadowsEnabled) return;
            Vector2 p = transform.position;
            bool inside = InsideCaster(p, 0f);

            if (!_settled)
            {
                bool still = (Anchor.position - _anchorStart).sqrMagnitude < 0.0001f;
                if (!still || Time.time > _settleUntil) _settled = true;
                else if (inside)
                {
                    Vector2 free;
                    if (FindClear(p, out free)) { transform.position = new Vector3(free.x, free.y, transform.position.z); inside = false; }
                }
            }

            float want = inside ? 0f : _base;
            _light.shadowIntensity = Mathf.MoveTowards(_light.shadowIntensity, want, Time.unscaledDeltaTime * 4f);
        }

        /// The point (with a little clearance) overlaps a solid collider that casts shadows: walls, pillars, rocks.
        public static bool InsideCaster(Vector2 p, float clearance)
        {
            _buf.Clear();
            var filter = new ContactFilter2D { useTriggers = false };
            if (clearance > 0f) Physics2D.OverlapCircle(p, clearance, filter, _buf);
            else Physics2D.OverlapPoint(p, filter, _buf);
            foreach (var c in _buf)
                if (c != null && !c.isTrigger && c.attachedRigidbody == null && c.GetComponent<ShadowCaster2D>() != null) return true;
            return false;
        }

        /// The nearest spot clear of every shadow caster, trying in front of (below) the wall first.
        public static bool FindClear(Vector2 from, out Vector2 free)
        {
            for (float r = 0.1f; r <= MaxNudge + 0.001f; r += 0.1f)
                foreach (var d in Dirs)
                {
                    Vector2 q = from + d * r;
                    if (!InsideCaster(q, Clearance)) { free = q; return true; }
                }
            free = from;
            return false;
        }
    }
}
