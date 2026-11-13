// Hearth & Hollow — ticket 63: red ground markers before a boss's big attacks: a lane for a charge, a fan for a
// breath, a ring for a slam. The marker fills up over the windup and flashes as the attack lands, so you can see
// it coming and step out of the way.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class Telegraph : MonoBehaviour
    {
        public const int Order = 1;   // on the floor: above the floor tiles (0), under walls (2) and everything that stands
        static readonly Color Edge = new Color(1f, 0.15f, 0.1f, 0.22f), Fill = new Color(1f, 0.2f, 0.12f, 0.42f);

        SpriteRenderer _edge, _fill;
        float _life, _t, _len, _width;
        bool _ring;

        static SpriteRenderer Part(Transform parent, Sprite s, Color c, int order)
        {
            var go = new GameObject("part");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s; sr.color = c; sr.sortingOrder = order;
            var m = FxMaterials.Unlit; if (m != null) sr.sharedMaterial = m;   // readable even in a dark boss room
            return sr;
        }

        /// A straight lane from `from` along `dir` (a charge, a beam).
        public static Telegraph Lane(Vector2 from, Vector2 dir, float length, float width, float life)
        {
            var go = new GameObject("Telegraph · lane");
            go.transform.position = from;
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            var t = go.AddComponent<Telegraph>();
            t._life = Mathf.Max(0.1f, life); t._len = length; t._width = width;
            t._edge = Part(go.transform, DungeonFx.BarPixel, Edge, Order);
            t._edge.transform.localScale = new Vector3(length * DungeonFx.PPU, width * DungeonFx.PPU, 1f);
            t._fill = Part(go.transform, DungeonFx.BarPixel, Fill, Order + 0);
            t._fill.transform.localScale = new Vector3(0f, width * DungeonFx.PPU, 1f);
            return t;
        }

        /// Rays in a fan (a breath of shots).
        public static void Fan(Vector2 from, Vector2 dir, float length, float stepDeg, int each, float life)
        {
            for (int i = -each; i <= each; i++) Lane(from, Quaternion.Euler(0, 0, i * stepDeg) * dir, length, 0.45f, life);
        }

        /// A disc around a point (a slam, a bite all round).
        public static Telegraph Ring(Vector2 at, float radius, float life)
        {
            var go = new GameObject("Telegraph · ring");
            go.transform.position = at;
            var t = go.AddComponent<Telegraph>();
            t._life = Mathf.Max(0.1f, life); t._len = radius; t._ring = true;
            float k = radius * 2f / DungeonFx.Circle.bounds.size.x;
            t._edge = Part(go.transform, DungeonFx.Circle, Edge, Order);
            t._edge.transform.localScale = new Vector3(k, k, 1f);
            t._fill = Part(go.transform, DungeonFx.Circle, Fill, Order);
            t._fill.transform.localScale = Vector3.zero;
            return t;
        }

        /// Cancel early (the monster was stunned / staggered / killed).
        public void Stop() { if (this != null) Destroy(gameObject); }

        void Update()
        {
            _t += Time.deltaTime;   // scaled: it pauses with the game and hit-stop
            float k = Mathf.Clamp01(_t / _life);
            float pulse = 0.75f + 0.25f * Mathf.Sin(_t * 18f);
            if (_edge != null) _edge.color = new Color(Edge.r, Edge.g, Edge.b, Edge.a * pulse + 0.15f * k);
            if (_fill != null)
            {
                if (_ring) { float s = _len * 2f / DungeonFx.Circle.bounds.size.x * k; _fill.transform.localScale = new Vector3(s, s, 1f); }
                else _fill.transform.localScale = new Vector3(_len * k * DungeonFx.PPU, _width * DungeonFx.PPU, 1f);
                _fill.color = k >= 1f ? new Color(1f, 0.5f, 0.35f, 0.7f) : Fill;   // flashes as it lands
            }
            if (_t >= _life + 0.08f) Destroy(gameObject);
        }
    }
}
