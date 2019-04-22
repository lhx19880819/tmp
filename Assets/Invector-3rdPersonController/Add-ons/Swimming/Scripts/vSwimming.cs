using UnityEngine;
using System.Collections;
using Assets.Scripts.Player;
using UnityEngine.Events;
using UnityStandardAssets.CrossPlatformInput;

namespace Invector.vCharacterController.vActions
{
    /// <summary>
    /// vSwimming Add-on
    /// On this Add-on we're locking the tpInput along with the tpMotor, tpAnimator & tpController methods to handle the Swimming behaviour.
    /// We can still access those scripts and methods, and call just what we need to use for example the FreeMovement, CameraInput, StaminaRecovery and UpdateHUD methods    
    /// This way the add-on become modular and plug&play easy to modify without changing the core of the controller. 
    /// </summary>

//    [vClassHeader("Swimming Action")]
    public class vSwimming : vActionListener
    {
        #region Swimming Variables      

        [Header("Animations Clips & Tags")]
        [Tooltip("Name of the tag assign into the Water object")]
        public string waterTag = "Water";
        [Tooltip("Name of the animation clip that will play when you enter the Water")]
        public string swimmingClip = "Swimming";
        [Tooltip("Name of the animation clip that will play when you enter the Water")]
        public string diveClip = "Dive";
        [Tooltip("Name of the tag assign into the Water object")]
        public string exitWaterTag = "Action";
        [Tooltip("Name of the animation clip that will play when you exit the Water")]
        public string exitWaterClip = "QuickClimb";

        [Header("Speed & Extra Options")]
        [Tooltip("Uncheck if you don't want to go under water")]
        public bool swimUpAndDown = true;
        [Tooltip("Speed to swim forward")]
        public float swimForwardSpeed = 3f;
        [Tooltip("Speed to rotate the character")]
        public float swimRotationSpeed = 3f;
        [Tooltip("Speed to swim up")]
        public float swimUpSpeed = 1.5f;
        [Tooltip("Increase the radius of the capsule collider to avoid enter walls")]
        public float colliderRadius = .5f;
        [Tooltip("Height offset to match the character Y position")]
        public float heightOffset = .3f;
        [Tooltip("Create a limit for the camera before affects the rotation Y of the character")]
        public float cameraRotationLimit = .65f;

        [Header("Health/Stamina Consuption")]
        [Tooltip("Leave with 0 if you don't want to use stamina consuption")]
        public float stamina = 15f;
        [Tooltip("How much health will drain after all the stamina were consumed")]
        public int healthConsumption = 1;

        [Header("Particle Effects")]
        public GameObject impactEffect;
        [Tooltip("Check the Rigibody.Y of the character to trigger the ImpactEffect Particle")]
        public float velocityToImpact = -4f;
        public GameObject waterRingEffect;
        [Tooltip("Frequency to instantiate the WaterRing effect while standing still")]
        public float waterRingFrequencyIdle = .8f;
        [Tooltip("Frequency to instantiate the WaterRing effect while swimming")]
        public float waterRingFrequencySwim = .15f;
        [Tooltip("Instantiate a prefab when exit the water")]
        public GameObject waterDrops;
        [Tooltip("Y Offset based at the capsule collider")]
        public float waterDropsYOffset = 1.6f;

        [Tooltip("Debug Mode will show the current behaviour at the console window")]
        public bool debugMode;

//        [Header("Inputs")]
//        [Tooltip("Input to make the character go up")]
//        public GenericInput swimUpInput = new GenericInput("Space", "X", "X");
//        [Tooltip("Input to make the character go down")]
//        public GenericInput swimDownInput = new GenericInput("LeftShift", "Y", "Y");
//        [Tooltip("Input to exit the water by triggering a climb animation")]
//        public GenericInput exitWaterInput = new GenericInput("E", "A", "A");

        private AInput tpInput;
        private vGetTransform exitWaterTrigger;
        private vGetTransform _tempExitWaterTrigger;
        private float originalColliderRadius;
        private float speed;
        private float timer;
        private float waterHeightLevel;
        private float originalRotationSpeed;
        private float waterRingSpawnFrequency;
        private bool inTheWater;
        private bool isExitingWater;

        // bools to trigger a method once on a update
        private bool triggerSwimState;
        private bool triggerExitSwim;
        private bool triggerUnderWater;
        private bool triggerAboveWater;

        #endregion

        public UnityEvent OnAboveWater;
        public UnityEvent OnUnderWater;
        public GenericInput JumpInput = new GenericInput("Space", "X", "X");

        private void Start()
        {
            tpInput = GetComponent<AInput>();
        }

        protected virtual void Update()
        {
            if (!inTheWater) return;

            ExitWaterAnimation();

            if (isExitingWater) return;

            UnderWaterBehaviour();
            SwimmingBehaviour();
        }

        protected virtual void LateUpdate()
        {
            if (!inTheWater) return;
            tpInput.LateUpdate();
#if UNITY_EDITOR
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
#endif
        }

        private void SwimmingBehaviour()
        {
            // trigger swim behaviour only if the water level matches the player height + offset
            if (tpInput._capsuleCollider.bounds.center.y + heightOffset < waterHeightLevel)
            {
                if (tpInput.currentHealth > 0)
                {
                    if (!triggerSwimState) EnterSwimState();        // call once the swim behaviour
                    SwimForwardInput();                             // input to swin forward
                    SwimUpOrDownInput();                            // input to swin up or down
                    ExitWaterInput();                               // input to exit the water if inside the exitTrigger
                    tpInput.FreeMovement();                      // update the free movement so we can rotate the character
//                    tpInput.StaminaRecovery();
                }
                else
                {
                    ExitSwimState();
                }
                tpInput.CameraInput();                          // update the camera input
//                tpInput.UpdateHUD();                            // update hud graphics                
            }
            else
            {
                ExitSwimState();
            }
        }

        private void UnderWaterBehaviour()
        {
            if (isUnderWater)
            {
                StaminaConsumption();

                if (!triggerUnderWater)
                {
                    tpInput._capsuleCollider.radius = colliderRadius;
                    triggerUnderWater = true;
                    triggerAboveWater = false;
                    OnUnderWater.Invoke();
                    tpInput.mAnimator.CrossFadeInFixedTime(diveClip, 0.25f);
                    tpInput.mAnimator.SetInteger("ActionState", 2);
                }
            }
            else
            {
                WaterRingEffect();
                if (!triggerAboveWater && triggerSwimState)
                {
                    triggerUnderWater = false;
                    triggerAboveWater = true;
                    OnAboveWater.Invoke();
                    tpInput.mAnimator.CrossFadeInFixedTime(swimmingClip, 0.25f);
                    tpInput.mAnimator.SetInteger("ActionState", 1);
                }
            }
        }

        private void StaminaConsumption()
        {
//            if (tpInput.currentStamina <= 0)
//            {
//                tpInput.ChangeHealth(-healthConsumption);
//            }
//            else
//            {
//                tpInput.ReduceStamina(stamina, true);        // call the ReduceStamina method from the player
//                tpInput.currentStaminaRecoveryDelay = 0.25f;    // delay to start recovery stamina           
//            }
        }

        public override void OnActionEnter(Collider other)
        {
            if (other.gameObject.CompareTag(waterTag))
            {
                if (debugMode) Debug.Log("Player enter the Water");
                inTheWater = true;
                waterHeightLevel = other.transform.position.y;
                originalColliderRadius = tpInput._capsuleCollider.radius;
                originalRotationSpeed = tpInput.freeSpeed.rotationSpeed;

                if (tpInput.verticalVelocity <= velocityToImpact)
                {
                    var newPos = new Vector3(transform.position.x, other.transform.position.y, transform.position.z);
                    Instantiate(impactEffect, newPos, tpInput.transform.rotation);
                }
            }

            if (other.gameObject.CompareTag(exitWaterTag))
            {
                exitWaterTrigger = other.GetComponent<vGetTransform>();
                _tempExitWaterTrigger = exitWaterTrigger;
            }
        }

        public override void OnActionExit(Collider other)
        {
            if (other.gameObject.CompareTag(waterTag))
            {
                if (debugMode) Debug.Log("Player left the Water");
                if (isExitingWater) return;
                inTheWater = false;
                ExitSwimState();
                if (waterDrops)
                {
                    var newPos = new Vector3(transform.position.x, transform.position.y + waterDropsYOffset, transform.position.z);
                    GameObject myWaterDrops = Instantiate(waterDrops, newPos, tpInput.transform.rotation) as GameObject;
                    myWaterDrops.transform.parent = transform;
                }
            }

            if (other.gameObject.CompareTag(exitWaterTag) && !isExitingWater)
            {
                exitWaterTrigger = null;
            }
        }

        private void EnterSwimState()
        {
            if (debugMode) Debug.Log("Player is Swimming");

            triggerSwimState = true;
            tpInput.enabled = false;
            ResetPlayerValues();
            tpInput.isStrafing = false;
            tpInput.customAction = true;
            tpInput.mAnimator.CrossFadeInFixedTime(swimmingClip, 0.25f);
            tpInput.freeSpeed.rotationSpeed = swimRotationSpeed;
            tpInput.Rigidbody.useGravity = false;
            tpInput.Rigidbody.drag = 10f;
        }

        private void ExitSwimState()
        {
            if (!triggerSwimState) return;
            if (debugMode) Debug.Log("Player Stop Swimming");

            triggerSwimState = false;
            tpInput.enabled = true;
            tpInput.customAction = false;
            tpInput.mAnimator.SetInteger("ActionState", 0);
            tpInput.colliderRadius = originalColliderRadius;
            tpInput.freeSpeed.rotationSpeed = originalRotationSpeed;
            tpInput.Rigidbody.useGravity = true;
            tpInput.Rigidbody.drag = 0f;
        }

        private void ExitWaterAnimation()
        {
            tpInput.LayerControl();                              // update the verification of the layers 
            tpInput.ActionsControl();                            // update the verifications of actions 

            if (_tempExitWaterTrigger == null) return;

            // verify if the exit water animation is playing
            isExitingWater = tpInput.baseLayerInfo.IsName(exitWaterClip);
            if (isExitingWater)
            {
                tpInput.CameraInput();                              // update the camera input
                tpInput.DisableGravityAndCollision();            // disable gravity and collision so the character can make the animation using root motion                
                tpInput.isGrounded = true;                       // ground the character so that we can run the root motion without any issues
                tpInput.mAnimator.SetBool("IsGrounded", true);    // also ground the character on the animator so that he won't float after finishes the climb animation

                if (_tempExitWaterTrigger.matchTarget != null)
                {
                    if (debugMode) Debug.Log("Match Target...");
                    // use match target to match the Y and Z target 
                    tpInput.MatchTarget(_tempExitWaterTrigger.matchTarget.transform.position, _tempExitWaterTrigger.matchTarget.transform.rotation, _tempExitWaterTrigger.avatarTarget,
                        new MatchTargetWeightMask(_tempExitWaterTrigger.matchTargetMask, 0), _tempExitWaterTrigger.startMatchTarget, _tempExitWaterTrigger.endMatchTarget);
                }

                if (_tempExitWaterTrigger.useTriggerRotation)
                {
                    if (debugMode) Debug.Log("Rotate to Target...");
                    // smoothly rotate the character to the target
                    transform.rotation = Quaternion.Lerp(transform.rotation, _tempExitWaterTrigger.transform.rotation, tpInput.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }

                // after playing the animation we reset some values
                if (tpInput.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1 >= .8f)
                {
                    tpInput.EnableGravityAndCollision(0f);       // enable again the gravity and collision 
                    exitWaterTrigger = null;                        // reset the exitWaterTrigger to null
                    isExitingWater = false;                         // reset the bool isExitingWater so we can exit again
                    inTheWater = false;                             // reset the bool saying that we're not on water anymore
                    ExitSwimState();                                // run the method exit swim state
                }
            }
        }

        private void SwimForwardInput()
        {
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            // get input access from player
            tpInput.input.x = h;//tpInput.horizontalInput.GetAxis();
            tpInput.input.y = v;//tpInput.verticallInput.GetAxis();
            speed = Mathf.Abs(tpInput.input.x) + Mathf.Abs(tpInput.input.y);
            speed = Mathf.Clamp(speed, 0, 1f);
            // update input values to animator 
            tpInput.mAnimator.SetFloat("InputVertical", speed, 0.5f, Time.deltaTime);
            // extra rigibody forward force 
            var velY = transform.forward * swimForwardSpeed * speed;
            velY.y = tpInput.Rigidbody.velocity.y;
            tpInput.Rigidbody.velocity = velY;
            tpInput.Rigidbody.AddForce(transform.forward * (swimForwardSpeed * speed) * Time.deltaTime, ForceMode.VelocityChange);
        }

        private void SwimUpOrDownInput()
        {
            if (tpInput.customAction) return;
            var upConditions = (((tpInput._capsuleCollider.bounds.center.y + heightOffset) - waterHeightLevel) < -.2f);

            if (!swimUpAndDown)
            {
                var newPos = new Vector3(transform.position.x, waterHeightLevel, transform.position.z);
                if (upConditions) tpInput.transform.position = Vector3.Lerp(transform.position, newPos, 0.5f * Time.deltaTime);
                return;
            }

            // extra rigibody up velocity                 
            if (JumpInput.GetButtonDown() && upConditions)
            {
                var vel = tpInput.Rigidbody.velocity;
                vel.y = swimUpSpeed;
                tpInput.Rigidbody.velocity = vel;
                //tpInput.mAnimator.PlayInFixedTime("DiveUp", 0, tpInput.input.magnitude > 0.1f ? 0.5f : 0.1f);
                tpInput.mAnimator.CrossFadeInFixedTime("DiveUp", tpInput.input.magnitude > 0.1f ? 0.5f : 0.1f);
            }
            else if (Input.GetKeyDown(KeyCode.F) && !upConditions)
            {
                var vel = tpInput.Rigidbody.velocity;
                vel.y = -swimUpSpeed;
                tpInput.Rigidbody.velocity = vel;
                tpInput.mAnimator.CrossFadeInFixedTime("DiveDown", tpInput.input.magnitude > 0.1f ? 0.5f : 0.1f);
            }
            else
            {
                // swim up or down based at the camera forward
                float inputGravityY = (Camera.main.transform.forward.y) * speed;
                var vel = tpInput.Rigidbody.velocity;
                vel.y = inputGravityY;
                if (vel.y > 0 && !upConditions)
                    vel.y = 0f;
                if (inputGravityY > cameraRotationLimit || inputGravityY < -cameraRotationLimit)
                {
                    tpInput.Rigidbody.velocity = vel;
                }
            }
        }

        private void ExitWaterInput()
        {
            if (exitWaterTrigger == null) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                tpInput.Rigidbody.drag = 0f;
                OnAboveWater.Invoke();
                tpInput.mAnimator.CrossFadeInFixedTime(exitWaterClip, 0.1f);
            }
        }

        private void WaterRingEffect()
        {
            // switch between waterRingFrequency for idle and swimming
            if (tpInput.input != Vector2.zero) waterRingSpawnFrequency = waterRingFrequencySwim;
            else waterRingSpawnFrequency = waterRingFrequencyIdle;

            // counter to instantiate the waterRingEffects using the current frequency
            timer += Time.deltaTime;
            if (timer >= waterRingSpawnFrequency)
            {
                var newPos = new Vector3(transform.position.x, waterHeightLevel, transform.position.z);
                Instantiate(waterRingEffect, newPos, tpInput.transform.rotation);
                timer = 0f;
            }
        }

        private void ResetPlayerValues()
        {
            tpInput.isJumping = false;
            tpInput.isSprinting = false;
            tpInput.mAnimator.SetFloat("InputHorizontal", 0);
            tpInput.mAnimator.SetFloat("InputVertical", 0);
            tpInput.mAnimator.SetInteger("ActionState", 1);
            tpInput.isGrounded = true;                       // ground the character so that we can run the root motion without any issues
            tpInput.mAnimator.SetBool("IsGrounded", true);    // also ground the character on the animator so that he won't float after finishes the climb animation
            tpInput.verticalVelocity = 0f;
        }

        bool isUnderWater
        {
            get
            {
                if (tpInput._capsuleCollider.bounds.max.y >= waterHeightLevel + 0.25f)
                    return false;
                else
                    return true;
            }
        }
    }//class end
}