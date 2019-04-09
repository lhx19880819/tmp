using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput : MonoBehaviour
    {
        private bool lockMovement;
        private bool lockInStrafe;

        private bool _isStrafing = false;
        private bool isStrafing
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
            InitAnimator();
            InitJump();
            InitCamera();
#if UNITY_EDITOR
            m_TurnSpeed = 2;
#else
            m_TurnSpeed = 0.1f;
#endif
        }

        private void Update()
        {
#if UNITY_EDITOR
            PCInput();
#endif
            UpdateMelee();

            if (isAttack) return;

            UpdateMotor();
            UpdateAnimator();

            UpdateCamera();

            UpdateJump();
        }

        private void PCInput()
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
            //
            if (Input.GetKeyDown("space"))
            {
                Jump();
            }

            //
            if (Input.GetMouseButtonDown(0))
            {
                if (isStrafing)
                {
                    mAnimator.SetInteger("AttackID", attackId);
                    mAnimator.SetTrigger("Attack");
                }
                else
                {
                    SwitchStrafe();
                    mAnimator.SetInteger("AttackID", attackId);
                    mAnimator.SetTrigger("Attack");
                }
            }

            //
            if (Input.GetMouseButtonDown(1))
            {
                SwitchStrafe();
            }
        }

        void LateUpdate()
        {
            CameraInput();
        }
    }//class end
}