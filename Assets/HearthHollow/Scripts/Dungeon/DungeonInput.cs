// Hearth & Hollow — dungeon controls (Input System package; mouse + keyboard, keyboard only, or gamepad).
//   Move: WASD / arrows / left stick / d-pad          Aim: mouse cursor / right stick (else the way you move)
//   Attack:   left click · J or Z · (A)               Skill 1:  right click · K or X · (X)
//   Skill 2:  Q · L · (RB)                            Ultimate: C · I · (right-stick click)
//   Dash: Space · (B)      Use: E or Enter · (Y)      Bag: Tab · (Select)
//   Torch: R · (LB)        Flare: F · (LT)
using UnityEngine;
using UnityEngine.InputSystem;

namespace HearthHollow.Dungeon
{
    public static class DungeonInput
    {
        public static Vector2 Move
        {
            get
            {
                Vector2 v = Vector2.zero;
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
                    if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
                    if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
                }
                var g = Gamepad.current;
                if (g != null)
                {
                    Vector2 s = g.leftStick.ReadValue();
                    if (s.sqrMagnitude > 0.08f) v += s;
                    v += g.dpad.ReadValue();
                }
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        // ───────── aiming ─────────
        // The mouse aims once it's been moved or clicked more recently than the keyboard-only keys (J K L I) or a
        // gamepad were used, so all three styles work without a setting.
        static int _polled = -1;
        static Vector2 _lastMouse;
        static float _mouseAt = -99f, _padAt = -99f, _keysAt = -99f;

        static void Poll()
        {
            if (_polled == Time.frameCount) return;
            _polled = Time.frameCount;
            float now = Time.unscaledTime;
            var m = Mouse.current;
            if (m != null)
            {
                Vector2 p = m.position.ReadValue();
                if ((p - _lastMouse).sqrMagnitude > 9f || m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame) _mouseAt = now;
                _lastMouse = p;
            }
            var k = Keyboard.current;
            if (k != null && (k.jKey.wasPressedThisFrame || k.kKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame
                              || k.zKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame)) _keysAt = now;
            var g = Gamepad.current;
            if (g != null && (g.rightStick.ReadValue().sqrMagnitude > 0.1f || g.leftStick.ReadValue().sqrMagnitude > 0.1f
                              || g.buttonSouth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame)) _padAt = now;
        }

        /// True while the mouse is the thing aiming (HUD shows mouse key labels then).
        public static bool MouseAim { get { Poll(); return _mouseAt > _keysAt && _mouseAt > _padAt; } }

        /// The cursor's world position (false without a mouse / camera).
        public static bool Cursor(out Vector2 world)
        {
            world = Vector2.zero;
            var m = Mouse.current; var cam = Camera.main;
            if (m == null || cam == null) return false;
            Vector2 p = m.position.ReadValue();
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
            world = w;
            return true;
        }

        /// Where to aim from `from`: the right stick if it's pushed, else the mouse if it's aiming. False = use the move direction.
        public static bool AimFrom(Vector2 from, out Vector2 dir)
        {
            dir = Vector2.zero;
            var g = Gamepad.current;
            if (g != null)
            {
                Vector2 s = g.rightStick.ReadValue();
                if (s.sqrMagnitude > 0.12f) { dir = s.normalized; return true; }
            }
            Vector2 c;
            if (MouseAim && Cursor(out c))
            {
                Vector2 d = c - from;
                if (d.sqrMagnitude > 0.04f) { dir = d.normalized; return true; }
            }
            return false;
        }

        // ───────── is the mouse over the HUD / a menu? ─────────
        static UnityEngine.UIElements.UIDocument[] _docs;
        static int _uiFrame = -1; static bool _uiOver;

        /// True when the cursor is over a HUD panel, badge, ability button or any button, so a click there doesn't also attack.
        public static bool PointerOverUI()
        {
            if (_uiFrame == Time.frameCount) return _uiOver;
            _uiFrame = Time.frameCount; _uiOver = false;
            var m = Mouse.current; if (m == null) return false;
            if (_docs == null || _docs.Length == 0 || _docs[0] == null) _docs = Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>();
            Vector2 sp = m.position.ReadValue();
            foreach (var d in _docs)
            {
                if (d == null || d.rootVisualElement == null || d.rootVisualElement.panel == null) continue;
                var panel = d.rootVisualElement.panel;
                Vector2 pp = UnityEngine.UIElements.RuntimePanelUtils.ScreenToPanel(panel, new Vector2(sp.x, Screen.height - sp.y));
                for (var e = panel.Pick(pp); e != null; e = e.parent)
                    if (e is UnityEngine.UIElements.Button || e.ClassListContains("panel") || e.ClassListContains("dhud__ability") || e.ClassListContains("badge"))
                    { _uiOver = true; return true; }
            }
            return false;
        }

        // ───────── buttons ─────────
        static Mouse M { get { return Mouse.current; } }

        public static bool Attack
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current; var m = M;
                return (k != null && (k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame))
                    || (m != null && m.leftButton.wasPressedThisFrame && !PointerOverUI())
                    || (g != null && g.buttonSouth.wasPressedThisFrame);
            }
        }

        /// Held attack keeps swinging (Moonlighter style).
        public static bool AttackHeld
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current; var m = M;
                return (k != null && (k.jKey.isPressed || k.zKey.isPressed)) || (m != null && m.leftButton.isPressed && !PointerOverUI()) || (g != null && g.buttonSouth.isPressed);
            }
        }

        /// Skill 1: right click / K / X / (X).
        public static bool Special
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current; var m = M;
                return (k != null && (k.kKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame))
                    || (m != null && m.rightButton.wasPressedThisFrame && !PointerOverUI())
                    || (g != null && g.buttonWest.wasPressedThisFrame);
            }
        }

        /// Skill 2: Q / L / (RB).
        public static bool Special2
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.qKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame))
                    || (g != null && g.rightShoulder.wasPressedThisFrame);
            }
        }

        /// Ultimate: C / I / (right-stick click).
        public static bool Ultimate
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.cKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame))
                    || (g != null && g.rightStickButton.wasPressedThisFrame);
            }
        }

        public static bool Dash
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && k.spaceKey.wasPressedThisFrame)
                    || (g != null && g.buttonEast.wasPressedThisFrame);
            }
        }

        public static bool Interact
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.eKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                    || (g != null && g.buttonNorth.wasPressedThisFrame);
            }
        }

        /// Flare — a burst of light round you: F / (LT).
        public static bool Flare
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && k.fKey.wasPressedThisFrame) || (g != null && g.leftTrigger.wasPressedThisFrame);
            }
        }

        /// Throw a torch: R / (LB).
        public static bool Torch
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.leftShoulder.wasPressedThisFrame);
            }
        }


        public static bool Bag
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && k.tabKey.wasPressedThisFrame)
                    || (g != null && g.selectButton.wasPressedThisFrame);
            }
        }

        // key labels for the HUD (follow whichever style is aiming)
        public static string AttackKey { get { return MouseAim ? "LMB" : "J"; } }
        public static string Skill1Key { get { return MouseAim ? "RMB" : "K"; } }
        public static string Skill2Key { get { return MouseAim ? "Q" : "L"; } }
        public static string UltKey { get { return MouseAim ? "C" : "I"; } }
    }
}
