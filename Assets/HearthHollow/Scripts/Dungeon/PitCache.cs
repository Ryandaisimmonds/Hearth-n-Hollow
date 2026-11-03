// Hearth & Hollow — opening arc stage 3: the forgotten cache you land in after falling through the pit.
// A big iron-banded chest: walk up to it and it opens — the next weapon, armour and helmet for your class
// (OpeningArc.pitCache) plus a treasure chest's worth of coins and ingredients (GameSession.DungeonPitCache).
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HearthHollow.Dungeon
{
    public class PitCache : MonoBehaviour
    {
        DungeonController _dc; RoomInstance _room;
        bool _open; Transform _lid; Light2D _light; float _t;
        static readonly Color Gold = new Color(1f, 0.82f, 0.4f);

        public void Setup(DungeonController dc, RoomInstance room)
        {
            _dc = dc; _room = room;
            int o = DungeonController.SortOrder(transform.position.y);
            Color wood = new Color(0.36f, 0.22f, 0.13f), dark = new Color(0.24f, 0.15f, 0.09f), iron = new Color(0.55f, 0.55f, 0.6f);
            Box(transform, new Vector2(0, 0.22f), new Vector2(0.9f, 0.44f), wood, o);
            Box(transform, new Vector2(-0.3f, 0.22f), new Vector2(0.07f, 0.46f), iron, o + 1);
            Box(transform, new Vector2(0.3f, 0.22f), new Vector2(0.07f, 0.46f), iron, o + 1);
            var lid = new GameObject("Lid"); lid.transform.SetParent(transform, false); lid.transform.localPosition = new Vector3(-0.45f, 0.44f, 0); _lid = lid.transform;
            Box(_lid, new Vector2(0.45f, 0.1f), new Vector2(0.94f, 0.2f), dark, o + 2);
            Box(_lid, new Vector2(0.45f, -0.01f), new Vector2(0.14f, 0.14f), Gold, o + 3);   // the clasp
            _light = DungeonFx.Flash((Vector2)transform.position + new Vector2(0, 0.45f), Gold, 3.4f, 1.2f, 0f, transform);
            DungeonController.CastShadows(_light, 0.7f, 0.45f);
            DungeonFx.RingAt(transform.position + new Vector3(0, 0.25f), new Color(1f, 0.85f, 0.5f, 0.9f), 1.6f, 0.8f);
            var col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.32f; col.offset = new Vector2(0, 0.2f);
        }

        static void Box(Transform parent, Vector2 local, Vector2 size, Color c, int order)
        {
            var g = new GameObject("Part"); g.transform.SetParent(parent, false); g.transform.localPosition = local;
            var b = DungeonFx.Pixel.bounds.size;
            g.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1f);
            var sr = g.AddComponent<SpriteRenderer>(); sr.sprite = DungeonFx.Pixel; sr.color = c; sr.sortingOrder = order;
        }

        void Update()
        {
            if (_dc == null) return;
            _t += Time.deltaTime;
            if (_open)
            {
                if (_light != null) _light.intensity = Mathf.MoveTowards(_light.intensity, 0.35f, Time.deltaTime * 0.5f);
                return;
            }
            if (_light != null) _light.intensity = 1.2f * (1f + 0.15f * Mathf.Sin(_t * 3f));
            if (!_dc.GameplayActive || _dc.Player == null || _dc.CurrentRoom != _room) return;
            if (Vector2.Distance(_dc.Player.Feet, transform.position) > 1.05f) return;
            _open = true;
            _lid.localRotation = Quaternion.Euler(0, 0, 40f);
            _lid.localPosition += new Vector3(0, 0.12f, 0);
            Vector2 p = (Vector2)transform.position + new Vector2(0, 0.5f);
            DungeonFx.Flash(p, Gold, 6f, 2.4f, 0.9f);
            DungeonFx.Sparks(p, Vector2.up, Gold, 20);
            DungeonFx.Embers(p, Gold, 28, 0.5f);
            GameFeel.HitStop(0.06f);
            DungeonHud.Number(p + new Vector2(0, 0.8f), "A forgotten cache!", "dnum--info");
            _dc.Session.DungeonPitCache();
        }
    }
}
