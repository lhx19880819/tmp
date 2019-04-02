using UnityEngine;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        private Animator mAnimator;

        private void InitAnimator()
        {
            mAnimator = GetComponent<Animator>();
        }

        private void UpdateAnimator()
        {
            mAnimator.SetBool("IsStrafing", isStrafing);
            mAnimator.SetBool("IsCrouching", isCrouching);
            mAnimator.SetBool("IsGrounded", isGrounded);
            mAnimator.SetBool("isDead", isDead);

            mAnimator.SetFloat("GroundDistance", groundDistance);
            mAnimator.SetFloat("VerticalVelocity", verticalVelocity);

            if (isStrafing)
            {
                mAnimator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
            }
            mAnimator.SetFloat("InputVertical", speed, dampTIme, Time.deltaTime);
        }
    }
}//class end
