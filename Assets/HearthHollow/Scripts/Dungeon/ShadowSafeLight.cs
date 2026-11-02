// Hearth & Hollow — keeps a shadow-casting light working when its centre ends up inside a wall, pillar or rock.
// A Light2D whose centre sits inside a ShadowCaster2D's shape treats everything as 'behind the wall' and goes dark. This
// fades the light's shadows out while its centre overlaps a solid static collider, and back in once it's clear.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    [RequireComponent(typeof(Light2D))]
    public class ShadowSafeLight : MonoBehaviour
    {
        Light2D _light;
        float _base;   // the shadow strength it was set up with
        static readonly List<Collider2D> _buf = new List<Collider2D>(8);

        void Start()
        {
            _light = GetComponent<Light2D>();
            _base = _light != null ? _light.shadowIntensity : 0f;
        }

        void LateUpdate()
        {
            if (_light == null || !_light.shadowsEnabled) return;
            bool inside = false;
            _buf.Clear();
            Physics2D.OverlapPoint(transform.position, new ContactFilter2D { useTriggers = false }, _buf);
            foreach (var c in _buf)
                if (c != null && !c.isTrigger && c.attachedRigidbody == null) { inside = true; break; }   // walls, pillars, rocks (not characters)
            float want = inside ? 0f : _base;
            _light.shadowIntensity = Mathf.MoveTowards(_light.shadowIntensity, want, Time.unscaledDeltaTime * 4f);
        }
    }
}
