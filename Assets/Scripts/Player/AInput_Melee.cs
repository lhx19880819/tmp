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
            bool bAttack = CrossPlatformInputManager.GetButtonDown("Attack");
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
    }//class end
}
