using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;
using Invector.CharacterController;
using Invector.CharacterController.Actions;
using Invector.vCharacterController;

namespace Invector.CharacterController.Actions
{
//    [vClassHeader("Ladder Action", "Use the vTriggerLadderAction on your ladder mesh.", iconName = "ladderIcon")]
    public class vLadderAction : vActionListener
    {
        #region public variables

        [Tooltip("Tag of the object you want to access")]
        public string actionTag = "LadderTrigger";
        
        [Tooltip("Input to up/down the ladder")]
        public GenericInput verticallInput = new GenericInput("Vertical", "LeftAnalogVertical", "Vertical");
        [Tooltip("Input to enter the ladder")]
        public GenericInput enterInput = new GenericInput("E", "A", "A");
        [Tooltip("Input to exit the ladder")]
        public GenericInput exitInput = new GenericInput("Space", "B", "B");     
        public bool debugMode;

        #endregion

        #region protected variables    

        protected AInput tpInput;
        protected vTriggerLadderAction ladderAction;
        protected vTriggerLadderAction ladderActionTemp;
        protected float speed;
        protected bool isUsingLadder;
        protected bool isExitingLadder;
        protected bool triggerEnterOnce;
        protected bool triggerExitOnce;

        #endregion

        private void Awake()
        {
            actionStay = true;
            actionExit = true;
        }

        void Start()
        {
            tpInput = GetComponent<AInput>();           
        }

        void Update()
        {
            tpInput.LateUpdate();
            AutoEnterLadder();
            EnterLadderInput();
            UseLadder();
            ExitLadderInput();
        }

        void OnAnimatorMove()
        {
            if(tpInput.enabled) return;
            if (!isUsingLadder) return;
            // enable movement using root motion
            transform.rotation = tpInput.Animator.rootRotation;
            transform.position = tpInput.Animator.rootPosition;
        }

        void EnterLadderInput()
        {
            if (ladderAction == null || tpInput.customAction) return;

            if (enterInput.GetButtonDown() && !isUsingLadder && !ladderAction.autoAction)
                TriggerEnterLadder();
        }

        void TriggerEnterLadder()
        {
            if (debugMode) Debug.Log("Enter Ladder");

            triggerEnterOnce = true;
            isUsingLadder = true;
            tpInput.Animator.SetInteger("ActionState", 1);     // set actionState 1 to avoid falling transitions            
            tpInput.enabled = false;                              // disable vThirdPersonInput
            tpInput.DisableGravityAndCollision();              // disable gravity & turn collision trigger
            tpInput.Rigidbody.velocity = Vector3.zero;                       
            ladderAction.OnDoAction.Invoke();

            ladderActionTemp = ladderAction;

            if (!string.IsNullOrEmpty(ladderAction.playAnimation))
                tpInput.Animator.CrossFadeInFixedTime(ladderAction.playAnimation, 0.1f);     // trigger the action animation clip                           
        }

        void UseLadder()
        {
            if (!isUsingLadder) return;

            // update the base layer to know what animations are being played
            tpInput.LayerControl();
            tpInput.ActionsControl();
            // update camera movement
            tpInput.CameraInput();

            // go up or down 
            tpInput.input.y = verticallInput.GetAxis();
            speed = Mathf.Clamp(tpInput.input.y, -1f, 1f);
            tpInput.Animator.SetFloat("InputVertical", speed, 0.25f, Time.deltaTime);

            // enter ladder behaviour           
            if (tpInput.baseLayerInfo.IsName("EnterLadderTop") || tpInput.baseLayerInfo.IsName("EnterLadderBottom"))
            {
                // disable ingame hud
                if (ladderActionTemp != null) ladderActionTemp.OnPlayerExit.Invoke();

                if (ladderActionTemp.useTriggerRotation)
                {
                    // smoothly rotate the character to the target
                    transform.rotation = Quaternion.Lerp(transform.rotation, ladderActionTemp.matchTarget.transform.rotation, tpInput.Animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }

                if(ladderActionTemp.matchTarget != null)
                {
                    if (debugMode) Debug.Log("Match Target...");
                    // use match target to match the Y and Z target 
                    tpInput.MatchTarget(ladderActionTemp.matchTarget.transform.position, ladderActionTemp.matchTarget.transform.rotation, AvatarTarget.Root, new MatchTargetWeightMask(new Vector3(1, 1, 1), 0), ladderActionTemp.startMatchTarget, ladderActionTemp.endMatchTarget);
                }                    
            }            

            // exit ladder behaviour
            isExitingLadder = tpInput.baseLayerInfo.IsName("ExitLadderTop") || tpInput.baseLayerInfo.IsName("ExitLadderBottom");

            if (isExitingLadder && tpInput.baseLayerInfo.normalizedTime >= 0.8f)
            {
                // after playing the animation we reset some values
                ResetPlayerSettings();
            }
        }

        void ExitLadderInput()
        {
            if (!isUsingLadder) return;
            if (tpInput.baseLayerInfo.IsName("EnterLadderTop") || tpInput.baseLayerInfo.IsName("EnterLadderBottom")) return;

            if (ladderAction == null)
            {
                // exit ladder at any moment by pressing the cancelInput
                if (tpInput.baseLayerInfo.IsName("ClimbLadder") && exitInput.GetButtonDown())
                {
                    if (debugMode) Debug.Log("Quick Exit");
                    ResetPlayerSettings();
                }
            }
            else
            {
                var animationClip = ladderAction.exitAnimation;
                if (animationClip == "ExitLadderBottom")
                {
                    // exit ladder when reach the bottom by pressing the cancelInput or pressing down at
                    if (exitInput.GetButtonDown() || (speed <= -0.05f && !triggerExitOnce))
                    {
                        if (debugMode) Debug.Log("Exit Bottom");
                        triggerExitOnce = true;
                        tpInput.Animator.CrossFadeInFixedTime(ladderAction.exitAnimation, 0.1f);             // trigger the animation clip        
                    }
                }
                else if (animationClip == "ExitLadderTop" && tpInput.baseLayerInfo.IsName("ClimbLadder"))    // exit the ladder from the top
                {
                    if ((speed >= 0.05f) && !triggerExitOnce && !tpInput.Animator.IsInTransition(0))         // trigger the exit animation by pressing up
                    {
                        if (debugMode) Debug.Log("Exit Top");
                        triggerExitOnce = true;
                        tpInput.Animator.CrossFadeInFixedTime(ladderAction.exitAnimation, 0.1f);             // trigger the animation clip
                    }
                }
            }
        }

        void AutoEnterLadder()
        {
            if (ladderAction == null || !ladderAction.autoAction) return;
            if (isUsingLadder || tpInput.Animator.IsInTransition(0)) return;

            // enter the ladder automatically if checked with autoAction
            if (ladderAction.autoAction && tpInput.input != Vector2.zero && !tpInput.actions)
            {
                var inputDir = Camera.main.transform.TransformDirection(new Vector3(tpInput.input.x, 0f, tpInput.input.y));
                inputDir.y = 0f;
                var dist = Vector3.Distance(inputDir.normalized, ladderAction.transform.forward);
                if (dist < 0.8f)
                    TriggerEnterLadder();
            }
        }

        void ResetPlayerSettings()
        {
            if (debugMode) Debug.Log("Reset Player Settings");
            speed = 0f;
            ladderAction = null;
            isUsingLadder = false;
            triggerExitOnce = false;
            triggerEnterOnce = false;
            tpInput._capsuleCollider.isTrigger = false;
            tpInput.Rigidbody.useGravity = true;
            tpInput.Animator.SetInteger("ActionState", 0);            
            tpInput.enabled = true;
            tpInput.gameObject.transform.eulerAngles = new Vector3(0f, tpInput.gameObject.transform.localEulerAngles.y, 0f);
        }

        public override void OnActionStay(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                CheckForTriggerAction(other);
            }
        }

        public override void OnActionExit(Collider other)
        {
            if (other.gameObject.CompareTag(actionTag))
            {
                // disable ingame hud
                if (ladderAction != null) ladderAction.OnPlayerExit.Invoke();
                ladderAction = null;
            }
        }

        void CheckForTriggerAction(Collider other)
        {
            // assign the component - it will be null when he exit the trigger area
            var _ladderAction = other.GetComponent<vTriggerLadderAction>();
            if (!_ladderAction)
            {
                return;
            }
            // check the maxAngle too see if the character can do the action
            var dist = Vector3.Distance(transform.forward, _ladderAction.transform.forward);

            if (isUsingLadder && _ladderAction != null)
                ladderAction = _ladderAction;
            else if (dist <= 0.8f && !isUsingLadder)
            {
                ladderAction = _ladderAction;
                ladderAction.OnPlayerEnter.Invoke();
            }
            else
            {
                if (ladderAction != null) ladderAction.OnPlayerExit.Invoke();
                ladderAction = null;
            }
        }
    }
}