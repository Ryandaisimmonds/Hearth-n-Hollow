// Hearth & Hollow — things class moves leave in the world (class rework, 31 Oct).
//   HoverLantern: the Lamplighter's Lantern. A floating light source that looses a homing wisp at the nearest enemy
//   every so often, then gutters out.
using HearthHollow.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class HoverLantern : MonoBehaviour
    {
        DungeonController _dc; float _mult, _interval, _seek, _left, _life, _fireT, _seed; int _pips; Color _col;
        SpriteRenderer _glow, _core; Light2D _light; float _lightI; Vector3 _base;

        public static HoverLantern Place(DungeonController dc, Vector2 at, MoveData m, float mult, int pips, Color col, float lightRadius)
        {
            if (dc == null || dc.FloorRoot == null) return null;
            var go = new GameObject("Lantern");
            go.transform.SetParent(dc.FloorRoot.transform, true);
            go.transform.position = at;
            var h = go.AddComponent<HoverLantern>();
            h._dc = dc; h._mult = mult; h._pips = pips; h._col = col;
            h._interval = Mathf.Max(0.25f, m.value); h._seek = m.range; h._left = h._life = m.duration; h._fireT = 0.4f;
            h._seed = Random.value * 6f; h._base = at;
            h.Build(lightRadius, m.lightIntensity > 0f ? m.lightIntensity : 1.6f);
            return h;
        }

        void Build(float radius, float intensity)
        {
            _glow = Part(DungeonFx.Circle, 0.75f, new Color(_col.r, _col.g, _col.b, 0.45f), 21000);
            _core = Part(DungeonFx.Orb != null ? DungeonFx.Orb : DungeonFx.Circle, 0.35f, Color.Lerp(_col, Color.white, 0.5f), 21001);
            _lightI = intensity * 1.2f;
            _light = DungeonFx.Flash(transform.position, _col, radius, _lightI, 0f, transform);   // a real light source (lit weak spots, light-shy monsters keep away)
            DungeonController.CastShadows(_light, 0.7f, 0.45f);
            DungeonFx.RingAt(transform.position, _col, 1.2f, 0.4f);
            DungeonFx.Embers(transform.position, _col, 10, 0.3f);
        }

        SpriteRenderer Part(Sprite s, float size, Color c, int order)
        {
            var g = new GameObject("Glow"); g.transform.SetParent(transform, false);
            var b = s.bounds.size;
            g.transform.localScale = new Vector3(size / Mathf.Max(0.0001f, b.x), size / Mathf.Max(0.0001f, b.y), 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            if (DungeonFx.GlowMat != null) sr.sharedMaterial = DungeonFx.GlowMat;
            return sr;
        }

        void Update()
        {
            if (_dc == null || _dc.FloorRoot == null) { Destroy(gameObject); return; }
            if (!_dc.GameplayActive) return;
            float dt = Time.deltaTime;
            _left -= dt;
            float t = Time.time + _seed;
            transform.position = _base + new Vector3(Mathf.Sin(t * 1.3f) * 0.08f, Mathf.Sin(t * 2.1f) * 0.12f, 0f);
            float fade = Mathf.Clamp01(_left / 1.2f);   // gutters over the last second
            float f = (1f + 0.08f * Mathf.Sin(t * 9f)) * fade;
            if (_light != null) _light.intensity = _lightI * f;
            if (_glow != null) _glow.color = new Color(_col.r, _col.g, _col.b, 0.45f * f);
            if (_core != null) { var c = _core.color; c.a = fade; _core.color = c; }
            if (_left <= 0f) { DungeonFx.Puff(transform.position, new Color(_col.r, _col.g, _col.b, 0.6f), 6, 0.4f); Destroy(gameObject); return; }

            _fireT -= dt;
            if (_fireT > 0f) return;
            var target = Combat2D.Nearest(transform.position, _seek);
            if (target == null) { _fireT = 0.2f; return; }
            _fireT = _interval;
            Vector2 dir = (target.Body - (Vector2)transform.position).normalized;
            var p = Projectile.Spawn(_dc, true, (Vector2)transform.position + dir * 0.2f, dir, 9f, _mult, 0f, DungeonFx.Orb, _col, false, 0.8f);
            if (p != null) { p.SetHit(HitType.Light, _pips); p.Homing(320f, _seek); }
            DungeonFx.Flash(transform.position, _col, 2.5f, 1.4f, 0.15f);
        }
    }
}
