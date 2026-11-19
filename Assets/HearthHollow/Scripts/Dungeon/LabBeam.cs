// Hearth & Hollow — a straight line of light (the Effects Lab's lances, cracks, crosses and pillars).
// A soft glow round a thin bright core, from one point to another. It flares, then thins and fades out; Grow
// stretches it out from its start as it appears (a thrust), and the whole thing runs on scaled time.
using UnityEngine;

namespace HearthHollow.Dungeon
{
    public class LabBeam : MonoBehaviour
    {
        SpriteRenderer _glow, _core;
        Vector2 _from, _to; Color _colour; float _width, _life, _t, _grow;

        /// A line from <paramref name="from"/> to <paramref name="to"/>. <paramref name="grow"/> > 0: it shoots out
        /// from <paramref name="from"/> over that many seconds instead of appearing all at once.
        public static LabBeam Line(Vector2 from, Vector2 to, Color colour, float width, float life, float grow = 0f, int order = 20480)
        {
            var go = new GameObject("fx_beam");
            var b = go.AddComponent<LabBeam>();
            b._from = from; b._to = to; b._colour = colour; b._width = width; b._life = Mathf.Max(0.02f, life); b._grow = grow;
            b._glow = Layer(go.transform, DungeonFx.Soft, order);
            b._core = Layer(go.transform, DungeonFx.Pixel, order + 1);
            b.Place(grow > 0f ? 0.02f : 1f, 1f);
            return b;
        }

        static SpriteRenderer Layer(Transform parent, Sprite sprite, int order)
        {
            var go = new GameObject("layer"); go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite; sr.sortingOrder = order;
            if (FxMaterials.Glow != null) sr.sharedMaterial = FxMaterials.Glow;
            return sr;
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_t >= _life) { Destroy(gameObject); return; }
            float reach = _grow > 0f ? Mathf.Clamp01(_t / _grow) : 1f;
            reach = 1f - (1f - reach) * (1f - reach);   // shoots out fast and settles
            float k = _t / _life;
            float flare = k < 0.12f ? 1f + 0.6f * (1f - k / 0.12f) : 1f;
            Place(reach, Mathf.Pow(1f - k, 0.7f) * flare);
        }

        void Place(float reach, float thick)
        {
            Vector2 to = Vector2.Lerp(_from, _to, reach);
            Vector2 d = to - _from; float len = Mathf.Max(0.01f, d.magnitude);
            transform.position = (_from + to) * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            float a = Mathf.Clamp01(thick);
            // the glow: the soft sprite stretched along the line (rounded, soft ends)
            Vector2 gs = _glow.sprite.bounds.size;
            _glow.transform.localScale = new Vector3((len + _width * 3f) / Mathf.Max(0.01f, gs.x), _width * 3.2f * thick / Mathf.Max(0.01f, gs.y), 1f);
            var gc = _colour; gc.a = 0.75f * a; _glow.color = gc;
            // the core: a thin white-hot line
            _core.transform.localScale = new Vector3(len * DungeonFx.PPU, Mathf.Max(0.01f, _width * 0.35f * thick) * DungeonFx.PPU, 1f);
            var cc = Color.Lerp(_colour, Color.white, 0.75f); cc.a = a; _core.color = cc;
        }
    }
}
