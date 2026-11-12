// Hearth & Hollow — ticket 59: tiny particles for UI Toolkit screens (the stove's steam, sizzle, sparkles and bursts).
// A layer over a panel; each particle is a small absolutely-positioned element moved by a scheduled tick.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class UiParticles
    {
        class P { public VisualElement el; public Vector2 pos, vel, acc; public float life, t, size, grow, alpha; public float spin, rot; }

        public readonly VisualElement Layer;
        readonly List<P> _live = new List<P>();
        float _last;
        const int Max = 140;

        public UiParticles()
        {
            Layer = new VisualElement { name = "ui-particles", pickingMode = PickingMode.Ignore };
            Layer.style.position = Position.Absolute;
            Layer.style.left = 0; Layer.style.top = 0; Layer.style.right = 0; Layer.style.bottom = 0;
            Layer.style.overflow = Overflow.Visible;
            Layer.schedule.Execute(Step).Every(16);
            _last = UIKit.Now;
        }

        public int Count { get { return _live.Count; } }

        /// A point inside the layer for an element's centre (plus an offset in its own size: (0.5, 0) = middle of the top edge).
        public Vector2 At(VisualElement e, Vector2 k)
        {
            var b = e.worldBound; var l = Layer.worldBound;
            return new Vector2(b.x + b.width * k.x - l.x, b.y + b.height * k.y - l.y);
        }

        /// A soft round blob (steam, puffs).
        public void Blob(Vector2 pos, Vector2 vel, Color c, float size, float life, float grow = 1.8f, Vector2 acc = default(Vector2))
        {
            var el = Make(size);
            el.style.backgroundColor = c;
            float r = size * 0.5f;
            el.style.borderTopLeftRadius = r; el.style.borderTopRightRadius = r; el.style.borderBottomLeftRadius = r; el.style.borderBottomRightRadius = r;
            Add(el, pos, vel, acc, size, grow, life, c.a, 0f);
        }

        /// A little glyph (✦ sparkles, ✧ glints) in a colour.
        public void Glyph(string g, Vector2 pos, Vector2 vel, Color c, float size, float life, float spin = 0f, Vector2 acc = default(Vector2))
        {
            var el = new Label(g) { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute;
            el.style.fontSize = size; el.style.color = c;
            el.style.unityTextAlign = TextAnchor.MiddleCenter;
            el.style.paddingLeft = 0; el.style.paddingRight = 0; el.style.paddingTop = 0; el.style.paddingBottom = 0;
            el.style.marginLeft = 0; el.style.marginTop = 0;
            el.style.width = size * 1.2f; el.style.height = size * 1.2f;
            Add(el, pos, vel, acc, size * 1.2f, 0.4f, life, c.a, spin);
        }

        /// A burst of sparkles and blobs out of a point (a Prime meal).
        public void Burst(Vector2 at, Color c, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 d = Random.insideUnitCircle.normalized;
                if (i % 3 == 0) Glyph(Random.value < 0.5f ? "✦" : "✧", at, d * speed * Random.Range(0.6f, 1.2f), c, Random.Range(14f, 24f), Random.Range(0.6f, 1.1f), Random.Range(-240f, 240f), new Vector2(0f, 220f));
                else Blob(at, d * speed * Random.Range(0.4f, 1.3f), new Color(c.r, c.g, c.b, 0.95f), Random.Range(4f, 8f), Random.Range(0.5f, 0.9f), 0.3f, new Vector2(0f, 260f));
            }
        }

        VisualElement Make(float size)
        {
            var el = new VisualElement { pickingMode = PickingMode.Ignore };
            el.style.position = Position.Absolute;
            el.style.width = size; el.style.height = size;
            return el;
        }

        void Add(VisualElement el, Vector2 pos, Vector2 vel, Vector2 acc, float size, float grow, float life, float alpha, float spin)
        {
            if (_live.Count >= Max) { Kill(0); }
            Layer.Add(el);
            var p = new P { el = el, pos = pos, vel = vel, acc = acc, size = size, grow = grow, life = Mathf.Max(0.05f, life), alpha = alpha, spin = spin };
            Place(p, 0f);
            _live.Add(p);
        }

        void Kill(int i) { _live[i].el.RemoveFromHierarchy(); _live.RemoveAt(i); }

        public void Clear() { while (_live.Count > 0) Kill(_live.Count - 1); }

        void Place(P p, float k)
        {
            float s = Mathf.Lerp(1f, p.grow, k);
            p.el.style.left = p.pos.x - p.size * 0.5f;
            p.el.style.top = p.pos.y - p.size * 0.5f;
            p.el.style.scale = new Scale(new Vector3(s, s, 1f));
            p.el.style.opacity = p.alpha * (k < 0.15f ? k / 0.15f : 1f - Mathf.Clamp01((k - 0.4f) / 0.6f));
            if (p.spin != 0f) p.el.style.rotate = new Rotate(p.rot);
        }

        void Step()
        {
            float now = UIKit.Now, dt = Mathf.Min(0.05f, now - _last); _last = now;
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var p = _live[i];
                p.t += dt;
                float k = p.t / p.life;
                if (k >= 1f) { Kill(i); continue; }
                p.vel += p.acc * dt;
                p.vel *= 1f - Mathf.Clamp01(dt * 1.5f);
                p.pos += p.vel * dt;
                p.rot += p.spin * dt;
                Place(p, k);
            }
        }
    }
}
