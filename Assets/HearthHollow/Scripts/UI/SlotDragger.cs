// Hearth & Hollow — pointer-driven drag & drop between a row/grid of slots.
// Used by the hearth tray (reorder) and the hollow bag (merge / swap).
// Pointer down on a slot → capture pointer; once the pointer moves past a small threshold a ghost
// label follows it on the drag layer; pointer up over another slot → onDrop(from, to).
// A press without movement counts as a click → onClick(index). Right-click → onRightClick(index).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace HearthHollow.UI
{
    public class SlotDragger
    {
        const float DragThreshold = 8f;

        readonly VisualElement _dragLayer;
        readonly List<VisualElement> _slots = new List<VisualElement>();
        readonly Func<int, string> _ghostText;     // null/empty → slot not draggable
        readonly Action<int, int> _onDrop;
        readonly Action<int> _onClick;
        readonly Action<int> _onRightClick;

        int _from = -1;
        int _pointerId = -1;
        Vector2 _downPos;
        bool _dragging;
        Label _ghost;
        VisualElement _hover;

        public SlotDragger(VisualElement dragLayer, Func<int, string> ghostText, Action<int, int> onDrop,
                           Action<int> onClick, Action<int> onRightClick)
        {
            _dragLayer = dragLayer; _ghostText = ghostText; _onDrop = onDrop;
            _onClick = onClick; _onRightClick = onRightClick;
        }

        public bool IsDragging { get { return _dragging; } }

        public void Clear() { Cancel(); _slots.Clear(); }

        /// Register a slot element. Its index is its registration order.
        public void Register(VisualElement slot)
        {
            int index = _slots.Count;
            _slots.Add(slot);
            slot.RegisterCallback<PointerDownEvent>(e => Down(slot, index, e));
            slot.RegisterCallback<PointerMoveEvent>(e => Move(slot, e));
            slot.RegisterCallback<PointerUpEvent>(e => Up(slot, e));
            slot.RegisterCallback<PointerCaptureOutEvent>(e => Cancel());
        }

        void Down(VisualElement slot, int index, PointerDownEvent e)
        {
            // Let real buttons inside the slot (e.g. the × discard) handle their own clicks.
            if (UIKit.IsInsideButton(e.target, slot)) return;
            if (e.button == 1) { if (_onRightClick != null) _onRightClick(index); e.StopPropagation(); return; }
            if (e.button != 0) return;
            _from = index;
            _pointerId = e.pointerId;
            _downPos = e.position;
            _dragging = false;
            slot.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        void Move(VisualElement slot, PointerMoveEvent e)
        {
            if (_from < 0 || e.pointerId != _pointerId || !slot.HasPointerCapture(e.pointerId)) return;
            Vector2 p = e.position;
            if (!_dragging)
            {
                if ((p - _downPos).magnitude < DragThreshold) return;
                string text = _ghostText != null ? _ghostText(_from) : null;
                if (string.IsNullOrEmpty(text)) return;
                _dragging = true;
                _ghost = UIKit.L(text, "drag-ghost");
                _ghost.pickingMode = PickingMode.Ignore;
                _dragLayer.Add(_ghost);
                _slots[_from].AddToClassList("slot--lifted");
            }
            Vector2 local = _dragLayer.WorldToLocal(p);
            _ghost.style.left = local.x - 28f;
            _ghost.style.top = local.y - 28f;

            var over = SlotAt(p);
            var overEl = over >= 0 && over != _from ? _slots[over] : null;
            if (overEl != _hover)
            {
                if (_hover != null) _hover.RemoveFromClassList("slot--drop");
                _hover = overEl;
                if (_hover != null) _hover.AddToClassList("slot--drop");
            }
            e.StopPropagation();
        }

        void Up(VisualElement slot, PointerUpEvent e)
        {
            if (_from < 0 || e.pointerId != _pointerId) return;
            int from = _from;
            bool wasDragging = _dragging;
            int to = SlotAt(e.position);
            Vector2 upPos = e.position;
            if (slot.HasPointerCapture(e.pointerId)) slot.ReleasePointer(e.pointerId);
            Cancel();
            e.StopPropagation();
            if (wasDragging)
            {
                if (to >= 0 && to != from && _onDrop != null) _onDrop(from, to);
            }
            else if (_onClick != null && slot.worldBound.Contains(upPos))
            {
                _onClick(from);
            }
        }

        int SlotAt(Vector2 worldPos)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].worldBound.Contains(worldPos)) return i;
            return -1;
        }

        void Cancel()
        {
            if (_ghost != null) { _ghost.RemoveFromHierarchy(); _ghost = null; }
            if (_hover != null) { _hover.RemoveFromClassList("slot--drop"); _hover = null; }
            if (_from >= 0 && _from < _slots.Count) _slots[_from].RemoveFromClassList("slot--lifted");
            _from = -1; _pointerId = -1; _dragging = false;
        }
    }
}
