// Hearth & Hollow — game-feel helpers: floating numbers, screen shake, scale pops, toasts.
// Everything is animated by hand with schedule.Execute(...).Every(16) so it doesn't depend on
// USS transition timing (transitions are only used for hover/press polish in the USS).
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class Juice
    {
        readonly VisualElement _stage;      // shaken element (holds all screens)
        readonly VisualElement _toastLayer;
        IVisualElementScheduledItem _shakeItem;
        float _shakeEnd, _shakeDur, _shakeMag;

        public Juice(VisualElement stage, VisualElement toastLayer)
        {
            _stage = stage; _toastLayer = toastLayer;
        }

        // ───────── screen shake ─────────
        public void Shake(float magnitudePx = 8f, float seconds = 0.2f)
        {
            float now = UIKit.Now;
            if (now < _shakeEnd && magnitudePx < _shakeMag) return; // a bigger shake is already playing
            _shakeMag = magnitudePx; _shakeDur = seconds; _shakeEnd = now + seconds;
            if (_shakeItem == null) _shakeItem = _stage.schedule.Execute(ShakeTick).Every(16);
            else _shakeItem.Resume();
        }

        void ShakeTick()
        {
            float left = _shakeEnd - UIKit.Now;
            if (left <= 0f)
            {
                _stage.style.translate = new Translate(0, 0);
                _shakeItem.Pause();
                return;
            }
            float m = _shakeMag * (left / _shakeDur);
            _stage.style.translate = new Translate(UnityEngine.Random.Range(-m, m), UnityEngine.Random.Range(-m, m));
        }

        // ───────── scale pop (crit hit-pause) ─────────
        /// Jumps to `peak` scale, holds for `holdSeconds` (the "hit-pause"), then eases back to 1.
        public static void Pop(VisualElement e, float peak = 1.2f, float holdSeconds = 0.1f, float backSeconds = 0.2f)
        {
            float start = UIKit.Now;
            e.style.scale = new Scale(new Vector2(peak, peak));
            IVisualElementScheduledItem item = null;
            item = e.schedule.Execute(() =>
            {
                float t = UIKit.Now - start - holdSeconds;
                if (t < 0f) return;
                float k = Mathf.Clamp01(t / backSeconds);
                float s = Mathf.Lerp(peak, 1f, 1f - (1f - k) * (1f - k));
                e.style.scale = new Scale(new Vector2(s, s));
                if (k >= 1f) item.Pause();
            }).Every(16);
        }

        /// Adds a CSS class for a short time (e.g. red flash on the player panel).
        public static void Flash(VisualElement e, string cls, long ms = 180)
        {
            e.AddToClassList(cls);
            e.schedule.Execute(() => e.RemoveFromClassList(cls)).StartingIn(ms);
        }

        // ───────── floating text ─────────
        /// Spawns a label inside `host` (which should be position: relative) that drifts up and fades.
        public static void Float(VisualElement host, string text, string variant, float delaySeconds = 0f)
        {
            if (host == null) return;
            var l = UIKit.L(text, "float-text");
            if (!string.IsNullOrEmpty(variant)) l.AddToClassList(variant);
            l.pickingMode = PickingMode.Ignore;
            // small random horizontal jitter so stacked numbers don't overlap exactly
            float jitter = UnityEngine.Random.Range(-24f, 24f);
            l.style.left = Length.Percent(50);
            l.style.top = Length.Percent(35);
            l.style.opacity = 0f;
            host.Add(l);

            float start = UIKit.Now + delaySeconds;
            const float life = 0.9f;
            IVisualElementScheduledItem item = null;
            item = l.schedule.Execute(() =>
            {
                float t = (UIKit.Now - start) / life;
                if (t < 0f) return;
                if (t >= 1f) { item.Pause(); l.RemoveFromHierarchy(); return; }
                float rise = 70f * (1f - (1f - t) * (1f - t));
                l.style.translate = new Translate(jitter - 30f, -rise);
                l.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                float s = t < 0.12f ? Mathf.Lerp(0.6f, 1.15f, t / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.12f) / 0.2f));
                l.style.scale = new Scale(new Vector2(s, s));
            }).Every(16);
        }

        // ───────── toasts ─────────
        public void Toast(string text, string variant = null, float seconds = 2.4f)
        {
            if (string.IsNullOrEmpty(text)) return;
            while (_toastLayer.childCount >= 5) _toastLayer.RemoveAt(0);
            var t = UIKit.L(text, "toast", "toast--enter");
            if (!string.IsNullOrEmpty(variant)) t.AddToClassList(variant);
            t.pickingMode = PickingMode.Ignore;
            _toastLayer.Add(t);
            // next frame: drop the enter class so the USS transition fades/slides it in
            t.schedule.Execute(() => t.RemoveFromClassList("toast--enter")).StartingIn(30);
            t.schedule.Execute(() => t.AddToClassList("toast--leave")).StartingIn((long)(seconds * 1000));
            t.schedule.Execute(() => t.RemoveFromHierarchy()).StartingIn((long)(seconds * 1000) + 400);
        }
    }
}
