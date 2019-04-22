using UnityEngine;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        Animator animator;
        public Animator mAnimator
        {
            get { return animator; }
        }

        [HideInInspector]
        public AnimatorStateInfo baseLayerInfo, underBodyInfo, rightArmInfo, leftArmInfo, fullBodyInfo, upperBodyInfo;

        int baseLayer { get { return mAnimator.GetLayerIndex("Base Layer"); } }
        int underBodyLayer { get { return mAnimator.GetLayerIndex("UnderBody"); } }
        int rightArmLayer { get { return mAnimator.GetLayerIndex("RightArm"); } }
        int leftArmLayer { get { return mAnimator.GetLayerIndex("LeftArm"); } }
        int upperBodyLayer { get { return mAnimator.GetLayerIndex("UpperBody"); } }
        int fullbodyLayer { get { return mAnimator.GetLayerIndex("FullBody"); } }


        private void InitAnimator()
        {
            animator = GetComponent<Animator>();
        }

        public void LayerControl()
        {
            baseLayerInfo = mAnimator.GetCurrentAnimatorStateInfo(baseLayer);
            underBodyInfo = mAnimator.GetCurrentAnimatorStateInfo(underBodyLayer);
            rightArmInfo = mAnimator.GetCurrentAnimatorStateInfo(rightArmLayer);
            leftArmInfo = mAnimator.GetCurrentAnimatorStateInfo(leftArmLayer);
            upperBodyInfo = mAnimator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            fullBodyInfo = mAnimator.GetCurrentAnimatorStateInfo(fullbodyLayer);
        }
        public void ActionsControl()
        {
            // to have better control of your actions, you can filter the animations state using bools 
            // this way you can know exactly what animation state the character is playing

            landHigh = baseLayerInfo.IsName("LandHigh");
            quickStop = baseLayerInfo.IsName("QuickStop");

            isRolling = baseLayerInfo.IsName("Roll");
            inTurn = baseLayerInfo.IsName("TurnOnSpot");

            // locks player movement while a animation with tag 'LockMovement' is playing
            lockMovement = IsAnimatorTag("LockMovement");
            // ! -- you can add the Tag "CustomAction" into a AnimatonState and the character will not perform any Melee action -- !            
            customAction = IsAnimatorTag("CustomAction");
        }

        public bool IsAnimatorTag(string tag)
        {
            if (animator == null) return false;
            if (baseLayerInfo.IsTag(tag)) return true;
            if (underBodyInfo.IsTag(tag)) return true;
            if (rightArmInfo.IsTag(tag)) return true;
            if (leftArmInfo.IsTag(tag)) return true;
            if (upperBodyInfo.IsTag(tag)) return true;
            if (fullBodyInfo.IsTag(tag)) return true;
            return false;
        }
        public void DisableGravityAndCollision()
        {
            animator.SetFloat("InputHorizontal", 0f);
            animator.SetFloat("InputVertical", 0f);
            animator.SetFloat("VerticalVelocity", 0f);
            Rigidbody.useGravity = false;
            _capsuleCollider.isTrigger = true;
        }

        private void UpdateAnimator()
        {
            mAnimator.SetBool("IsStrafing", isStrafing);
            mAnimator.SetBool("IsCrouching", isCrouching);
            mAnimator.SetBool("IsGrounded", isGrounded);
//            mAnimator.SetBool("isDead", isDead);

            mAnimator.SetFloat("GroundDistance", groundDistance);
            mAnimator.SetFloat("VerticalVelocity", verticalVelocity);

            if (isStrafing)
            {
                mAnimator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
            }
            mAnimator.SetFloat("InputVertical", speed, dampTIme, Time.deltaTime);
        }

        public void MatchTarget(Vector3 matchPosition, Quaternion matchRotation, AvatarTarget target, MatchTargetWeightMask weightMask, float normalisedStartTime, float normalisedEndTime)
        {
            if (animator.isMatchingTarget || animator.IsInTransition(0))
                return;

            float normalizeTime = Mathf.Repeat(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1f);

            if (normalizeTime > normalisedEndTime)
                return;

            animator.MatchTarget(matchPosition, matchRotation, target, weightMask, normalisedStartTime, normalisedEndTime);
        }
    }
}//class end
