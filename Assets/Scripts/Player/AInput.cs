using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput : MonoBehaviour
    {
        public float currentHealth = 10;

        private bool lockMovement;
        private bool lockInStrafe;

        private bool _isStrafing = false;
        public bool isStrafing
        {
            get
            {
                return _isStrafing || lockInStrafe;
            }
            set
            {
                _isStrafing = value;
            }
        }

        private bool isDead = false;

        void Start()
        {
            InitCharacter();
            InitAnimator();
            InitJump();
            InitCamera();
#if UNITY_EDITOR
            m_TurnSpeed = 2;
#else
            m_TurnSpeed = 0.2f;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            PCInput();
            if (Cursor.visible)
            {
                mAnimator.SetFloat("InputMagnitude", 0);
                direction = 0;
                speed = 0;
                return;
            }
#endif
            UpdateMelee();

            if (isAttack) return;

            UpdateMotor();
            UpdateAnimator();

            UpdateJump();
        }

#if UNITY_EDITOR
        public void PCInput()
        {
            //
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Cursor.visible = !Cursor.visible;
                if (Cursor.visible)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
            if (Cursor.visible)
            {
                mAnimator.SetFloat("InputMagnitude", 0);
                direction = 0;
                speed = 0;
                return;
            }
            //
            if (JumpInput.GetButtonDown())
            {
                Jump();
            }

            //
            if (Input.GetMouseButtonDown(0))
            {
                Attack();
            }

            //
            if (Input.GetMouseButtonDown(1))
            {
                SwitchStrafe();
            }
        }
#endif

        public void LateUpdate()
        {
#if UNITY_EDITOR
            if (Cursor.visible)
            {
                mAnimator.SetFloat("InputMagnitude", 0);
                direction = 0;
                speed = 0;
                return;
            }
#endif
            CameraInput();
            UpdateCamera();
        }

        public void SetLockMove(bool b, float f = 0)
        {
            if (f == 0)
            {
                lockMovement = b;
            }
            else
            {
                StartCoroutine(LockMoveDelay(b, f));
            }
        }

        private IEnumerator LockMoveDelay(bool b, float f)
        {
            yield return new WaitForSecondsRealtime(f);
            lockMovement = b;
        }
    }//class end
}