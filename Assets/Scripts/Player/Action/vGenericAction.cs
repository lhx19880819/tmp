using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;

namespace Invector.vCharacterController.vActions
{
    using UnityEngine.Events;
    using vCharacterController;
    //    [vClassHeader("GENERIC ACTION", "Use the vTriggerGenericAction to trigger a simple animation.", iconName = "triggerIcon")]
    public class vGenericAction : vActionListener
    {
        #region Variables
        [Tooltip("Input to make the action")]
        public GenericInput actionInput = new GenericInput("E", "A", "A");
        [Tooltip("Tag of the object you want to access")]
        public string actionTag = "Action";
        public int animatorActionState = 1;
        [Tooltip("Use root motion of the animation")]
        public bool useRootMotion = true;

        [Header("--- Debug Only ---")]
        public vTriggerGenericAction triggerAction;
        [Tooltip("Check this to enter the debug mode")]
        public bool debugMode;
        public bool canTriggerAction;
        public bool isPlayingAnimation;
        public bool triggerActionOnce;

        public UnityEvent OnStartAction;
        public UnityEvent OnEndAction;

        public bool lockInput = false;
        public float lockTime = 0;

        protected AInput tpInput;

        #endregion

        private void Awake()
        {
            actionStay = true;
            actionExit = true;
        }

        protected virtual void Start()
        {
            tpInput = GetComponent<AInput>();
        }

        void Update()
        {
            TriggerActionInput();
        }

        void OnAnimatorMove()
        {
            if (tpInput.enabled) return;
            tpInput.LayerControl();                              // update the verification of the layers 

            AnimationBehaviour();

            if (!playingAnimation) return;

            if (!tpInput.customAction)
            {
                // enable movement using root motion
                transform.rotation = tpInput.Animator.rootRotation;
            }
            transform.position = tpInput.Animator.rootPosition;
        }

        protected virtual void TriggerActionInput()
        {
            if (!tpInput.enabled)
            {
                return;
            }
            if (triggerAction == null) return;

            if (canTriggerAction)
            {
                if ((triggerAction.autoAction && actionConditions) || (actionInput.GetButtonDown() && actionConditions))
                {
                    if (!triggerActionOnce)
                    {
                        OnDoAction.Invoke(triggerAction);
                        TriggerAnimation();
                        if (lockInput)
                        {
                            StartCoroutine(UnlockInput());
                        }
                    }
                }
            }
        }

        private IEnumerator UnlockInput()
        {
            tpInput.SetLockMove(true);
            yield return new WaitForSecondsRealtime(lockTime);
            tpInput.SetLockMove(false);
        }

        public virtual bool actionConditions
        {
            get
            {
                return !(tpInput.isJumping || tpInput.actions || !canTriggerAction || isPlayingAnimation) && !tpInput.Animator.IsInTransition(0);
            }
        }

        protected virtual void TriggerAnimation()
        {
            if (debugMode) Debug.Log("TriggerAnimation");

            // trigger the animation behaviour & match target
            if (!string.IsNullOrEmpty(triggerAction.playAnimation))
            {
                isPlayingAnimation = true;
                tpInput.Animator.CrossFadeInFixedTime(triggerAction.playAnimation, 0.1f);    // trigger the action animation clip
                                                                                             //                tpInput.ChangeCameraState(triggerAction.customCameraState);                     // change current camera state to a custom
            }

            // trigger OnDoAction Event, you can add a delay in the inspector   
            StartCoroutine(triggerAction.OnDoActionDelay(gameObject));

            // bool to limit the autoAction run just once
            if (triggerAction.autoAction || triggerAction.destroyAfter) triggerActionOnce = true;

            // destroy the triggerAction if checked with destroyAfter
            if (triggerAction.destroyAfter)
                StartCoroutine(DestroyDelay(triggerAction));
        }

        public virtual IEnumerator DestroyDelay(vTriggerGenericAction triggerAction)
        {
            var _triggerAction = triggerAction;
            yield return new WaitForSeconds(_triggerAction.destroyDelay);
            OnEndAction.Invoke();
            ResetPlayerSettings();
            Destroy(_triggerAction.gameObject);
        }

        protected virtual void AnimationBehaviour()
        {
            if (playingAnimation)
            {
                OnStartAction.Invoke();

                if (triggerAction.matchTarget != null)
                {
                    if (debugMode) Debug.Log("Match Target...");
                    // use match target to match the Y and Z target 
                    tpInput.MatchTarget(triggerAction.matchTarget.transform.position, triggerAction.matchTarget.transform.rotation, triggerAction.avatarTarget,
                        new MatchTargetWeightMask(triggerAction.matchTargetMask, 0), triggerAction.startMatchTarget, triggerAction.endMatchTarget);
                }

                if (triggerAction.useTriggerRotation)
                {
                    if (debugMode) Debug.Log("Rotate to Target...");
                    // smoothly rotate the character to the target
                    transform.rotation = Quaternion.Lerp(transform.rotation, triggerAction.transform.rotation, tpInput.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }

                if (triggerAction.resetPlayerSettings && tpInput.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= triggerAction.endExitTimeAnimation)
                {
                    if (debugMode) Debug.Log("Finish Animation");
                    // after playing the animation we reset some values
                    if (!triggerAction.destroyAfter) OnEndAction.Invoke();

                    ResetPlayerSettings();
                }
            }
        }

        protected virtual bool playingAnimation
        {
            get
            {
                if (triggerAction == null)
                {
                    isPlayingAnimation = false;
                    return false;
                }

                if (!isPlayingAnimation && !string.IsNullOrEmpty(triggerAction.playAnimation) && tpInput.baseLayerInfo.IsName(triggerAction.playAnimation))
                {
                    isPlayingAnimation = true;
                    if (triggerAction != null) triggerAction.OnPlayerExit.Invoke();
                    ApplyPlayerSettings();
                }
                else if (isPlayingAnimation && !string.IsNullOrEmpty(triggerAction.playAnimation) && !tpInput.baseLayerInfo.IsName(triggerAction.playAnimation))
                    isPlayingAnimation = false;

                return isPlayingAnimation;
            }
        }

        public override void OnActionEnter(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                if (triggerAction != null) triggerAction.OnPlayerEnter.Invoke();
            }
        }

        public override void OnActionStay(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag) && !isPlayingAnimation)
            {
                CheckForTriggerAction(other);
            }
        }

        public override void OnActionExit(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                if (debugMode) Debug.Log("Exit vTriggerAction");
                if (triggerAction != null) triggerAction.OnPlayerExit.Invoke();
                ResetPlayerSettings();
            }
        }

        protected virtual void CheckForTriggerAction(Collider other)
        {
            var _triggerAction = other.GetComponent<vTriggerGenericAction>();
            if (!_triggerAction) return;

            var dist = Vector3.Distance(transform.forward, _triggerAction.transform.forward);

            if (!_triggerAction.activeFromForward || dist <= 0.8f)
            {
                triggerAction = _triggerAction;
                canTriggerAction = true;
                triggerAction.OnPlayerEnter.Invoke();
            }
            else
            {
                if (triggerAction != null) triggerAction.OnPlayerExit.Invoke();
                canTriggerAction = false;
            }
        }

        protected virtual void ApplyPlayerSettings()
        {
            if (debugMode) Debug.Log("ApplyPlayerSettings");

            if (triggerAction.disableGravity)
            {
                tpInput.Rigidbody.useGravity = false;               // disable gravity of the player
                tpInput.Rigidbody.velocity = Vector3.zero;
                tpInput.isGrounded = true;                           // ground the character so that we can run the root motion without any issues
                tpInput.Animator.SetBool("IsGrounded", true);        // also ground the character on the animator so that he won't float after finishes the climb animation
                tpInput.Animator.SetInteger("ActionState", animatorActionState > 1 ? animatorActionState : 1);       // set actionState 1 or higher to avoid falling transitions     
            }
            if (triggerAction.disableCollision)
                tpInput._capsuleCollider.isTrigger = true;           // disable the collision of the player if necessary 
        }

        protected virtual void ResetPlayerSettings()
        {
            if (debugMode) Debug.Log("Reset Player Settings");
            if (!playingAnimation || tpInput.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= triggerAction.endExitTimeAnimation)
            {
                tpInput.EnableGravityAndCollision(0f);             // enable again the gravity and collision
                tpInput.Animator.SetInteger("ActionState", 0);     // Reset actions State to 0
            }

            // reset camera state to default
            //            tpInput.ResetCameraState();
            canTriggerAction = false;
            triggerActionOnce = false;
        }
    }
}