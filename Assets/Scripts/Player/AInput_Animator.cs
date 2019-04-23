using UnityEngine;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        Animator animator;
        public Animator Animator
        {
            get { return animator; }
        }

        [HideInInspector]
        public AnimatorStateInfo baseLayerInfo, underBodyInfo, rightArmInfo, leftArmInfo, fullBodyInfo, upperBodyInfo;

        int baseLayer { get { return Animator.GetLayerIndex("Base Layer"); } }
        int underBodyLayer { get { return Animator.GetLayerIndex("UnderBody"); } }
        int rightArmLayer { get { return Animator.GetLayerIndex("RightArm"); } }
        int leftArmLayer { get { return Animator.GetLayerIndex("LeftArm"); } }
        int upperBodyLayer { get { return Animator.GetLayerIndex("UpperBody"); } }
        int fullbodyLayer { get { return Animator.GetLayerIndex("FullBody"); } }


        private void InitAnimator()
        {
            animator = GetComponent<Animator>();
        }

        public void LayerControl()
        {
            baseLayerInfo = Animator.GetCurrentAnimatorStateInfo(baseLayer);
            underBodyInfo = Animator.GetCurrentAnimatorStateInfo(underBodyLayer);
            rightArmInfo = Animator.GetCurrentAnimatorStateInfo(rightArmLayer);
            leftArmInfo = Animator.GetCurrentAnimatorStateInfo(leftArmLayer);
            upperBodyInfo = Animator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            fullBodyInfo = Animator.GetCurrentAnimatorStateInfo(fullbodyLayer);
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
            LayerControl();
            ActionsControl();

            Animator.SetBool("IsStrafing", isStrafing);
            Animator.SetBool("IsCrouching", isCrouching);
            Animator.SetBool("IsGrounded", isGrounded);
//            mAnimator.SetBool("isDead", isDead);

            Animator.SetFloat("GroundDistance", groundDistance);
            Animator.SetFloat("VerticalVelocity", verticalVelocity);

            if (isStrafing)
            {
                Animator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
            }
            Animator.SetFloat("InputVertical", speed, dampTIme, Time.deltaTime);
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
