// Hearth & Hollow — the Effects Lab's quick ring (R): the lab's actions as round buttons round the mouse, for a
// keyboard with no F keys. Time holds still while it's open; click one, or click anywhere else / press R to close.
using System.Collections.Generic;
using HearthHollow.Dungeon;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HearthHollow.Lab
{
    public partial class EffectsLab
    {
        VisualElement _ring;
        Vector2? _ringWorld;   // the room spot under the mouse when the ring opened (dummy / spawn go there)
        bool RingOpen { get { return _ring != null; } }

        void OpenRing()
        {
            var m = Mouse.current;
            if (m != null) OpenRingAt(m.position.ReadValue());
        }

        /// The ring centred on a screen point (pixels, origin bottom-left).
        void OpenRingAt(Vector2 screen)
        {
            var panel = _ui.Root.panel;
            if (panel == null) return;
            Vector2 c = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            var cam = Camera.main;
            _ringWorld = cam != null && !DungeonInput.PointerOverUI() ? (Vector2?)(Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z)) : null;

            // a see-through cover over the whole screen: a click on it closes the ring (and never swings)
            _ring = new VisualElement { name = "lab-ring" };
            _ring.AddToClassList("panel");
            var rs = _ring.style;
            rs.position = Position.Absolute; rs.left = 0; rs.top = 0; rs.right = 0; rs.bottom = 0;
            rs.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
            rs.backgroundImage = new StyleBackground(StyleKeyword.None);   // .panel draws a framed box: not here
            rs.marginLeft = rs.marginRight = rs.marginTop = rs.marginBottom = 0;
            rs.paddingLeft = rs.paddingRight = rs.paddingTop = rs.paddingBottom = 0;
            _ring.RegisterCallback<PointerDownEvent>(e => { if (e.target == _ring) { CloseRing(); e.StopPropagation(); } });

            var items = new List<KeyValuePair<string, System.Action>>
            {
                new KeyValuePair<string, System.Action>(_paused ? "▶\nPlay  P" : "⏸\nPause  P", () => SetPaused(!_paused)),
                new KeyValuePair<string, System.Action>("⏭\nFrame  .", StepFrame),
                new KeyValuePair<string, System.Action>(_p.autoOn ? "🔁\nAuto off  T" : "🔁\nAuto on  T", () => SetAuto(!_p.autoOn)),
                new KeyValuePair<string, System.Action>("🐢\nSlower  −", () => StepSpeed(-1)),
                new KeyValuePair<string, System.Action>("🐇\nFaster  =", () => StepSpeed(1)),
                new KeyValuePair<string, System.Action>("🎯\nDummy", () => { if (Ready) SpawnDummy(SpawnPoint(_ringWorld)); }),
                new KeyValuePair<string, System.Action>("👾\nMonster", () => { if (Ready) SpawnMonster(SpawnPoint(_ringWorld), false); }),
                new KeyValuePair<string, System.Action>("🌪️\nSwirl boss", SpawnSwirlBoss),
                new KeyValuePair<string, System.Action>("💀\nKill all", KillAll),
                new KeyValuePair<string, System.Action>("🔄\nRestart", Restart),
                new KeyValuePair<string, System.Action>("☰\nPanel", () => ShowPanel(!_panelShown)),
            };
            const float Radius = 128f, Size = 80f;
            // near a screen edge, slide the ring in so every button shows (the spawn spot stays where the mouse was)
            float reach = Radius + Size / 2f + 16f;
            var full = _ui.Root.layout;
            if (full.width > reach * 2f) c.x = Mathf.Clamp(c.x, reach, full.width - reach);
            if (full.height > reach * 2f) c.y = Mathf.Clamp(c.y, reach, full.height - reach);
            for (int i = 0; i < items.Count; i++)
            {
                var act = items[i].Value;
                float a = Mathf.PI / 2f - i * Mathf.PI * 2f / items.Count;   // clockwise from the top
                var b = new Button(() => { CloseRing(); act(); _ui.Root.Focus(); }) { text = items[i].Key };
                b.focusable = false;
                var bs = b.style;
                bs.position = Position.Absolute; bs.width = Size; bs.height = Size;
                bs.left = c.x + Mathf.Cos(a) * Radius - Size / 2f; bs.top = c.y - Mathf.Sin(a) * Radius - Size / 2f;
                bs.borderTopLeftRadius = bs.borderTopRightRadius = bs.borderBottomLeftRadius = bs.borderBottomRightRadius = Size / 2f;
                bs.backgroundColor = new Color(0.16f, 0.11f, 0.08f, 0.94f);
                bs.borderLeftWidth = bs.borderRightWidth = bs.borderTopWidth = bs.borderBottomWidth = 2;
                bs.borderLeftColor = bs.borderRightColor = bs.borderTopColor = bs.borderBottomColor = new Color(1f, 0.75f, 0.4f, 0.7f);
                bs.color = new Color(1f, 0.93f, 0.8f); bs.fontSize = 12; bs.whiteSpace = WhiteSpace.Normal;
                bs.unityTextAlign = TextAnchor.MiddleCenter;
                bs.paddingLeft = bs.paddingRight = bs.paddingTop = bs.paddingBottom = 0;
                bs.marginLeft = bs.marginRight = bs.marginTop = bs.marginBottom = 0;
                _ring.Add(b);
            }
            var hub = Text("R", 13, new Color(1f, 0.85f, 0.55f));
            var hs = hub.style;
            hs.position = Position.Absolute; hs.width = 30; hs.height = 30; hs.left = c.x - 15f; hs.top = c.y - 15f;
            hs.unityTextAlign = TextAnchor.MiddleCenter; hs.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            hs.borderTopLeftRadius = hs.borderTopRightRadius = hs.borderBottomLeftRadius = hs.borderBottomRightRadius = 15;
            _ring.Add(hub);

            _ui.Root.Add(_ring);
            GameFeel.LabPaused = true;
        }

        void CloseRing()
        {
            if (_ring != null) _ring.RemoveFromHierarchy();
            _ring = null;
            GameFeel.LabPaused = _paused;
        }
    }
}
