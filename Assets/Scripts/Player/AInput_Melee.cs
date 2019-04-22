using Invector.vCharacterController;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        private int attackId = 0;
        private bool isAttack = false;
        private int combo = 0;

        private void UpdateMelee()
        {
            if (isGrounded)
            {
                bool bAttack = CrossPlatformInputManager.GetButtonDown("Attack");
                Attack(bAttack);
//                if (CrossPlatformInputManager.GetButtonDown("Strafe"))
//                {
//                    SwitchStrafe();
//                }
            }
        }

        public void SwitchStrafe()
        {
            if (customAction || isAttack) return;
            isStrafing = !isStrafing;
            mAnimator.SetBool("IsStrafing", isStrafing);
        }

        public void OnEnableAttack()
        {
            combo++;
            isAttack = true;


            mAnimator.SetFloat("InputMagnitude", 0);
            direction = 0;
            speed = 0;
            Rigidbody.velocity = Vector3.zero;
        }

        public void OnDisableAttack()
        {
            combo--;
            if (combo == 0)
            {
                isAttack = false;
            }
        }

        public void ResetAttackTriggers()
        {
            mAnimator.ResetTrigger("Attack");
        }

        private void Attack(bool bAttack = true)
        {
            if (!isGrounded)
            {
                return;
            }
            if (isStrafing && bAttack)
            {
                mAnimator.SetInteger("AttackID", attackId);
                mAnimator.SetTrigger("Attack");
            }
            else if (bAttack)
            {
                SwitchStrafe();
                mAnimator.SetInteger("AttackID", attackId);
                mAnimator.SetTrigger("Attack");
            }
        }
    }//class end
}
