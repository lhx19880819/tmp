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
        }

        private void Update()
        {
            UpdateMelee();

            if (isAttack) return;

            UpdateMotor();
            UpdateAnimator();

            UpdateCamera();

            UpdateJump();
#if UNITY_EDITOR
            PCInput();
#endif
        }

        private void PCInput()
        {
            if (Input.GetKeyDown("space"))
            {
                Jump();
            }
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
                if (Input.GetMouseButtonDown(0))
                {
                    OnDisableAttack();
                }
            }
        }

        void LateUpdate()
        {
            CameraInput();
        }
    }//class end
}