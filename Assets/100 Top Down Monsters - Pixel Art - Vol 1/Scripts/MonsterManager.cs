using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdmurinsMonsters
{
    public class MonsterManager : MonoBehaviour
    {
        public Animator[] monsterAnimators;
        public bool facingUp, facingRight, facingLeft, facingDown;
        // Start is called before the first frame update

        private void Update()
        {
            ChangeDirection();
            ChangeAnimation();
        }

        private void ChangeDirection()
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
            {
                _FaceUp();
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
            {
                _FaceDown();
            }
            else if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
            {
                _FaceLeft();
            }
            else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
            {
                _FaceRight();
            }
        }

        private void ChangeAnimation()
        {
            if (Keyboard.current.numpad1Key.wasPressedThisFrame || Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                _AnimationIdle();
            }
            else if (Keyboard.current.numpad2Key.wasPressedThisFrame || Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                _AnimationMove();
            }
            else if (Keyboard.current.numpad3Key.wasPressedThisFrame || Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                _AnimationAttack();
            }
            else if (Keyboard.current.numpad4Key.wasPressedThisFrame || Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                _AnimationAttack_2();
            }
            else if (Keyboard.current.numpad5Key.wasPressedThisFrame || Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                _AnimationAbility();
            }
        }

        public void _FaceRight()
        {
            ResetDirection();
            facingRight = true;
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetFloat("Horizontal", 1);
                monster.SetFloat("Vertical", 0);
            }
        }
        public void _FaceLeft()
        {
            ResetDirection();
            facingLeft = true;
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetFloat("Horizontal", -1);
                monster.SetFloat("Vertical", 0);
            }
        }
        public void _FaceUp()
        {
            ResetDirection();
            facingUp = true;
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetFloat("Horizontal", 0);
                monster.SetFloat("Vertical", 1);
            }
        }
        public void _FaceDown()
        {
            ResetDirection();
            facingDown = true;
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetFloat("Horizontal", 0);
                monster.SetFloat("Vertical", -1);
            }
        }

        public void _AnimationIdle()
        {
            ResetAnimations();
        }

        public void _AnimationAttack()
        {
            ResetAnimations();
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetBool("Attack", true);
            }
        }

        public void _AnimationAttack_2()
        {
            ResetAnimations();
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetBool("Attack 2", true);
            }
        }

        public void _AnimationMove()
        {
            ResetAnimations();
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetBool("Move", true);
            }
        }

        public void _AnimationAbility()
        {
            ResetAnimations();
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetBool("Ability", true);
            }
        }
        private void ResetDirection()
        {
            facingRight = false;
            facingLeft = false;
            facingUp = false;
            facingDown = false;
        }
        private void ResetAnimations()
        {
            foreach (Animator monster in monsterAnimators)
            {
                monster.SetBool("Move", false);
                monster.SetBool("Attack", false);
                monster.SetBool("Attack 2", false);
                monster.SetBool("Ability", false);
            }
        }

        public void OpenLink(string link)
        {
            Application.OpenURL(link);
        }

    }
}
