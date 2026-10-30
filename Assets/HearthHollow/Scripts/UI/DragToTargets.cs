// Hearth & Hollow — drag something from a set of SOURCE elements onto a set of TARGET elements
// (e.g. a recipe card onto a tray slot). Press on a source, move past a small threshold → a ghost
// follows the pointer; release over a target → onDrop(sourceKey, targetIndex). A press without
// movement is left alone, so buttons inside the source still click normally.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class DragToTargets
    {
        const float DragThreshold = 8f;

        readonly VisualElement _dragLayer;
        readonly Action<string, int> _onDrop;
        readonly List<VisualElement> _targets = new List<VisualElement>();
        /// Called with the source key when a source is pressed and released without dragging.
        public Action<string> onClick;

        VisualElement _src;
        string _key, _ghostText;
        int _pointerId = -1;
        Vector2 _downPos;
        bool _dragging;
        Label _ghost;
        VisualElement _hover;

        public DragToTargets(VisualElement dragLayer, Action<string, int> onDrop)
        {
            _dragLayer = dragLayer; _onDrop = onDrop;
        }

        public void ClearTargets() { _targets.Clear(); }
        public void AddTarget(VisualElement t) { _targets.Add(t); }

        /// `canDrag` is checked on press (e.g. "can this recipe be cooked right now?").
        public void AddSource(VisualElement src, string key, string ghostText, Func<bool> canDrag)
        {
            src.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || UIKit.IsInsideButton(e.target, src)) return;
                if (canDrag != null && !canDrag()) return;
                _src = src; _key = key; _ghostText = ghostText;
                _pointerId = e.pointerId; _downPos = e.position; _dragging = false;
                src.CapturePointer(e.pointerId);
            });
            src.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (_src != src || e.pointerId != _pointerId || !src.HasPointerCapture(e.pointerId)) return;
                Vector2 p = e.position;
                if (!_dragging)
                {
                    if ((p - _downPos).magnitude < DragThreshold) return;
                    _dragging = true;
                    _ghost = UIKit.L(_ghostText, "drag-ghost");
                    _ghost.pickingMode = PickingMode.Ignore;
                    _dragLayer.Add(_ghost);
                    src.AddToClassList("slot--lifted");
                    for (int i = 0; i < _targets.Count; i++) _targets[i].AddToClassList("drop-target");
                }
                Vector2 local = _dragLayer.WorldToLocal(p);
                _ghost.style.left = local.x - 28f;
                _ghost.style.top = local.y - 28f;
                int over = TargetAt(p);
                var el = over >= 0 ? _targets[over] : null;
                if (el != _hover)
                {
                    if (_hover != null) _hover.RemoveFromClassList("slot--drop");
                    _hover = el;
                    if (_hover != null) _hover.AddToClassList("slot--drop");
                }
                e.StopPropagation();
            });
            src.RegisterCallback<PointerUpEvent>(e =>
            {
                if (_src != src || e.pointerId != _pointerId) return;
                bool was = _dragging; string key = _key;
                int to = TargetAt(e.position);
                if (src.HasPointerCapture(e.pointerId)) src.ReleasePointer(e.pointerId);
                Cancel();
                if (was) { e.StopPropagation(); if (to >= 0 && _onDrop != null) _onDrop(key, to); }
                else if (onClick != null && src.worldBound.Contains(e.position)) onClick(key);
            });
            src.RegisterCallback<PointerCaptureOutEvent>(e => { if (_src == src) Cancel(); });
        }

        int TargetAt(Vector2 worldPos)
        {
            for (int i = 0; i < _targets.Count; i++)
                if (_targets[i].worldBound.Contains(worldPos)) return i;
            return -1;
        }

        void Cancel()
        {
            if (_ghost != null) { _ghost.RemoveFromHierarchy(); _ghost = null; }
            if (_hover != null) { _hover.RemoveFromClassList("slot--drop"); _hover = null; }
            if (_src != null) _src.RemoveFromClassList("slot--lifted");
            for (int i = 0; i < _targets.Count; i++) _targets[i].RemoveFromClassList("drop-target");
            _src = null; _pointerId = -1; _dragging = false;
        }
    }
}
