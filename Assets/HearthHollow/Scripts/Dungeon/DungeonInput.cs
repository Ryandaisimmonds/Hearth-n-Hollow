// Hearth & Hollow — dungeon controls (Input System package; keyboard + gamepad).
//   Move: WASD / arrows / left stick / d-pad      Attack: J or Z / (A)
//   Skill 1: K or X / (X)    Skill 2: U or C / (RB)   Dash: Space or L / (B)
//   Use: E or Enter / (Y)                         Bag: Tab or I / (Select)
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

        public static bool Attack
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.jKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame))
                    || (g != null && g.buttonSouth.wasPressedThisFrame);
            }
        }

        /// Held attack keeps swinging (Moonlighter style).
        public static bool AttackHeld
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.jKey.isPressed || k.zKey.isPressed)) || (g != null && g.buttonSouth.isPressed);
            }
        }

        public static bool Special
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.kKey.wasPressedThisFrame || k.xKey.wasPressedThisFrame))
                    || (g != null && g.buttonWest.wasPressedThisFrame);
            }
        }

        /// Second skill slot (U).
        public static bool Special2
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.uKey.wasPressedThisFrame || k.cKey.wasPressedThisFrame))
                    || (g != null && g.rightShoulder.wasPressedThisFrame);
            }
        }

        public static bool Dash
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.spaceKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame))
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

        public static bool Bag
        {
            get
            {
                var k = Keyboard.current; var g = Gamepad.current;
                return (k != null && (k.tabKey.wasPressedThisFrame || k.iKey.wasPressedThisFrame))
                    || (g != null && g.selectButton.wasPressedThisFrame);
            }
        }
    }
}
