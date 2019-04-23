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
//        public GenericInput AttackInput = new GenericInput("Attack", "RT", "RT");
//        public GenericInput StrafeInput = new GenericInput("q", "RB", "RB");

        private void UpdateMelee()
        {
            if (isGrounded)
            {
                bool bAttack = CrossPlatformInputManager.GetButtonDown("Attack");
                Attack(bAttack);
                if (CrossPlatformInputManager.GetButtonDown("Strafe"))
                {
                    SwitchStrafe();
                }
            }
        }

        public void SwitchStrafe()
        {
            if (customAction || isAttack) return;
            isStrafing = !isStrafing;
            Animator.SetBool("IsStrafing", isStrafing);
        }

        public void OnEnableAttack()
        {
            combo++;
            isAttack = true;


            Animator.SetFloat("InputMagnitude", 0);
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
            Animator.ResetTrigger("Attack");
        }

        private void Attack(bool bAttack = true)
        {
            if (!isGrounded)
            {
                return;
            }
            if (isStrafing && bAttack)
            {
                Animator.SetInteger("AttackID", attackId);
                Animator.SetTrigger("Attack");
            }
            else if (bAttack)
            {
                SwitchStrafe();
                Animator.SetInteger("AttackID", attackId);
                Animator.SetTrigger("Attack");
            }
        }
    }//class end
}
