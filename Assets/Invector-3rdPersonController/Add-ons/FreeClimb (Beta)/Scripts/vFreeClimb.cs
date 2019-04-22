using UnityEngine;
using System.Collections;
using Invector;
using Invector.CharacterController;
using System.Collections.Generic;
using Assets.Scripts.Player;
using UnityEngine.Internal;
using UnityStandardAssets.CrossPlatformInput;

namespace Invector.CharacterController.Actions
{
//    [vClassHeader("FREE CLIMB ADD-ON (BETA)", "Make sure the mesh you want to climb is assigned with the 'FreeClimb' Tag", iconName = "climbIcon")]
    public class vFreeClimb : MonoBehaviour
    {
        #region Public variables

        public bool autoClimbEdge = true;
        public string cameraState = "Default";
        [Range(0, 180)]
        public float minSurfaceAngle = 30, maxSurfaceAngle = 160;
        [Tooltip("Empty GameObject Child of Character\nPosition this object on the \"hand target position\"")]
        public Transform handTarget;
        [Tooltip("Tags of draggable walls")]
        public List<string> draggableTags = new List<string>() { "FreeClimb" };
        [Tooltip("Layer of draggable wall")]
        public LayerMask draggableWall;
        [Tooltip("Layer to check obstacles in movement direction ")]
        public LayerMask obstacle;
        [Tooltip("Use this to check if can go to horizontal position")]
        public float lastPointDistanceH = 0.4f;
        [Tooltip("Use this to check if can go to vertical position")]
        public float lastPointDistanceVUp = 0.2f;
        public float lastPointDistanceVDown = 1.25f;
        [Tooltip("Start Point of RayCast to check if Can GO")]
        public float offsetHandTarget = -0.2f;
        [Tooltip("Start Point of RayCast to check Base Rotation")]
        public float offsetBase = 0.35f;
        [Tooltip("Min Wall thickeness to climbUp")]
        public float climbUpMinThickness = 0.3f;
        [Tooltip("Min space  to climbUp with obstruction")]
        public float climbUpMinSpace = 0.5f;
        [Tooltip("Max Distance to ClimbJump")]
        public float climbJumpDistance = 2f;
        public float climbJumpDepth = 2f;
        public float climbUpHeight = 2f;
        [Tooltip("Offset to Hand IK")]
        public Vector3 offsetHandPositionL, offsetHandPositionR;
        [Tooltip("Root Animator state to call")]
        public string animatorStateHierarchy = "Base Layer.Actions.FreeClimb";
        public bool debugRays;
        [HideInInspector]
        public bool debugClimbMovement = true;
        [HideInInspector]
        public bool debugClimbUp;
        [HideInInspector]
        public bool debugClimbJump;
        [HideInInspector]
        public bool debugBaseRotation;
        [HideInInspector]
        public bool debugHandIK;
        public UnityEngine.Events.UnityEvent onEnterClimb, onExitClimb;

        #endregion

        #region Protected variables

        protected vDragInfo dragInfo;
        protected AInput TP_Input;
        protected float horizontal, vertical;
        protected RaycastHit hit;
        protected bool canMoveClimb;
        protected bool inClimbUp;
        protected bool inClimbJump;
        protected bool inAlingClimb;
        protected bool climbEnterGrounded, climbEnterAir;
        protected Vector3 upPoint;
        protected Vector3 jumpPoint;
        protected float oldInput = 0.1f;
        protected Quaternion jumpRotation;
        protected Vector2 input;
        protected float ikWeight;
        protected float posTransition;
        protected float rotationTransition;
        Vector3 lHandPos;
        Vector3 rHandPos;
        Vector3 targetPositionL;
        Vector3 targetPositionR;
        Vector2 lastInput;
        Quaternion lastRotation;

        Vector3 handTargetPosition
        {
            get
            {
                return transform.TransformPoint(handTarget.localPosition.x, handTarget.localPosition.y, 0);
            }
        }
        #endregion

        #region UnityEngine Methods

        protected virtual void Start()
        {
            dragInfo = new vDragInfo();
            TP_Input = GetComponent<AInput>();
        }

        protected virtual void Update()
        {
            if (CheckClimbCondictions())
            {
                float h = CrossPlatformInputManager.GetAxis("Horizontal");
                float v = CrossPlatformInputManager.GetAxis("Vertical");
                input = new Vector3(h, v);
                ClimbHandle();
                ClimbJumpHandle();
                ClimbUpHandle();
            }
            else
            {
                input = Vector2.zero;
                TP_Input.mAnimator.SetFloat("InputHorizontal", 0);
                TP_Input.mAnimator.SetFloat("InputVertical", 0);
            }
        }

        protected virtual void LateUpdate()
        {
            if (dragInfo.inDrag)
            {
                TP_Input.LateUpdate();
//                TP_Input.CameraInput();
//                TP_Input.tpCamera.ChangeState(cameraState, true);
            }
        }

        #endregion

        #region Climb Behaviour

        [System.Serializable]
        public class vDragInfo
        {
            public bool canGo;
            public bool inDrag;
            public Vector3 position;
            public Vector3 normal;
        }

        protected virtual bool CheckClimbCondictions()
        {
            if (!TP_Input)
            {
                if (TP_Input.enabled == false)
                {
                    dragInfo.inDrag = false;
                    dragInfo.canGo = false;
                    TP_Input.Rigidbody.isKinematic = false;
//                    TP_Input.enabled = true;
                    TP_Input.enabled = true;
                }
                return false;
            }
            return true;
        }

        protected virtual void ClimbHandle()
        {
//            if (!TP_Input.mAnimator) return;

            if (/*(TP_Input.isJumping || !TP_Input.isGrounded)*/ !dragInfo.inDrag)
            {
                if (Physics.Raycast(handTargetPosition, transform.forward, out hit, 0.7f, draggableWall))
                {
                    if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                    {
                        dragInfo.canGo = true;
                        dragInfo.position = hit.point;
                        dragInfo.normal = hit.normal;
                    }
                }
                else
                    dragInfo.canGo = false;
            }

            if (Physics.Raycast(handTargetPosition, transform.forward, out hit, 0.7f, draggableWall) && dragInfo.canGo)
            {
                dragInfo.position = hit.point;
                if (CrossPlatformInputManager.GetButtonDown("Jump") && dragInfo.inDrag && input.magnitude == 0 && Time.time > (oldInput + 0.5f))
                    ExitClimb();
                else if (dragInfo.canGo && (CrossPlatformInputManager.GetButton("Jump") || TP_Input.input.y > 0.1f) && !dragInfo.inDrag && Time.time > (oldInput + 0.5f))
                    EnterClimb();
            }
            ClimbMovement();
        }

        protected virtual void ClimbMovement()
        {
            if (!dragInfo.inDrag) return;

            horizontal = input.x;
            vertical = input.y;
            canMoveClimb = CheckCanMoveClimb();

            if (canMoveClimb)
            {
                TP_Input.mAnimator.SetFloat("InputHorizontal", horizontal, 0.2f, Time.deltaTime);
                TP_Input.mAnimator.SetFloat("InputVertical", vertical, 0.2f, Time.deltaTime);
            }
            else if (!inAlingClimb && !inClimbJump)
            {
                TP_Input.mAnimator.SetFloat("InputHorizontal", 0, 0.2f, Time.deltaTime);
                TP_Input.mAnimator.SetFloat("InputVertical", 0, 0.2f, Time.deltaTime);
            }

            if (input.y < 0 && Physics.Raycast(transform.position + Vector3.up * (TP_Input._capsuleCollider.height * 0.5f), Vector3.down, TP_Input._capsuleCollider.height, TP_Input.groundLayer))
            {
                ExitClimb();
            }
        }

        protected virtual bool CheckCanMoveClimb()
        {
            //if (input.magnitude <= 0.1f) return false;
            if (input.magnitude > 0.1f)
            {
                lastInput = input;
            }
            var h = lastInput.x > 0 ? 1 * lastPointDistanceH : lastInput.x < 0 ? -1 * lastPointDistanceH : 0;
            var v = lastInput.y > 0 ? 1 * lastPointDistanceVUp : lastInput.y < 0 ? -1 * lastPointDistanceVDown : 0;
            var centerCharacter = handTargetPosition + transform.up * offsetHandTarget;
            var targetPosNormalized = centerCharacter + (transform.right * h) + (transform.up * v);
            var targetPos = centerCharacter + (transform.right * lastInput.x) + (transform.up * lastInput.y);
            var castDir = (targetPosNormalized - handTargetPosition + (transform.forward * -0.5f)).normalized;
            var castDirCapsule = (targetPos - handTargetPosition + (transform.forward * -0.5f)).normalized;

            if (TP_Input._capsuleCollider.CheckCapsule(castDirCapsule, out hit, obstacle) && !draggableTags.Contains(hit.collider.gameObject.tag))
            {
                return false;
            }

            if (inClimbJump || inClimbUp) return false;
            vLine climbLine = new vLine(centerCharacter, targetPosNormalized);
            climbLine.Draw(Color.green, draw: debugRays && debugClimbMovement);
            if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, draggableWall))
            {
                if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                {
                    dragInfo.normal = hit.normal;
                    return true;
                }
            }

            climbLine.p1 = climbLine.p2;
            climbLine.p2 = climbLine.p1 + transform.forward * TP_Input._capsuleCollider.radius * 2f;
            climbLine.Draw(Color.yellow, draw: debugRays && debugClimbMovement);
            if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, draggableWall))
            {
                if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                {
                    dragInfo.normal = hit.normal;
                    return true;
                }
            }

            climbLine.p1 += transform.forward * 0.5f;
            climbLine.p2 += (transform.right * -h * 2f) + (transform.up * -v) + transform.forward * TP_Input._capsuleCollider.radius;
            climbLine.Draw(Color.red, draw: debugRays && debugClimbMovement);
            if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, draggableWall))
            {
                if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                {
                    dragInfo.normal = hit.normal;
                    return true;
                }
            }
            return false;
        }

        protected virtual void ClimbJumpHandle()
        {
            if (TP_Input.enabled || !TP_Input.mAnimator || !dragInfo.inDrag || inClimbUp) return;
            if (CrossPlatformInputManager.GetButton("Jump") && !inClimbJump && input.magnitude > 0 && !TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".ClimbJump"))
            {
                var angleBetweenCharacterAndCamera = Vector3.Angle(transform.right, Camera.main.transform.right);
                var rightDirection = angleBetweenCharacterAndCamera > 60 ? Camera.main.transform.right : transform.right;
                var pos360 = handTargetPosition + (transform.forward * -0.5f) + (rightDirection * climbJumpDistance * horizontal) + (Vector3.up * climbJumpDistance * vertical);
                if (debugRays && debugClimbJump)
                {
                    Debug.DrawLine(handTargetPosition + (transform.forward * -0.05f), pos360, Color.red, 1f);
                    Debug.DrawRay(pos360, transform.forward * climbJumpDepth, Color.red, 1f);
                }

                float casts = 0f;
                for (int i = 0; casts < 1f; i++)
                {
                    var radius = TP_Input._capsuleCollider.radius / 0.45f;

                    var dir = (rightDirection * input.x + Vector3.up * input.y).normalized;
                    for (float a = 0; a < 1; a += 0.2f)
                    {
                        var p = transform.position + Vector3.up * TP_Input._capsuleCollider.height * casts;
                        p = p + rightDirection * ((-TP_Input._capsuleCollider.radius) + (radius * a));

                        if (Physics.Raycast(p, dir.normalized, out hit, climbJumpDistance, obstacle))
                        {
                            if (!(draggableWall == (draggableWall | (1 << hit.collider.gameObject.layer))) || !draggableTags.Contains(hit.collider.gameObject.tag))
                            {
                                if (debugRays && debugClimbJump) Debug.DrawRay(p, dir.normalized * climbJumpDistance, Color.red, 0.4f);
                                return;
                            }
                            else if (debugRays && debugClimbJump) Debug.DrawRay(p, dir.normalized * climbJumpDistance, Color.yellow, 0.4f);
                        }
                        else if (debugRays && debugClimbJump) Debug.DrawRay(p, dir.normalized * climbJumpDistance, Color.green, 0.4f);

                    }

                    casts += 0.1f;
                }

                if (Physics.Linecast(handTargetPosition + (transform.forward * -0.5f), pos360, out hit, draggableWall))
                {
                    if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                    {
                        var dir = pos360 - handTargetPosition;
                        var angle = Vector3.Angle(Vector3.up, dir);
                        angle = angle * (input.x > 0 ? 1 : input.x < 0 ? -1 : 1);
                        jumpRotation = Quaternion.LookRotation(-hit.normal);

                        jumpPoint = hit.point;

                        dragInfo.position = hit.point;
                        ClimbJump();
                    }
                }
                else if (Physics.Raycast(pos360, transform.forward, out hit, climbJumpDepth, draggableWall))
                {
                    if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                    {
                        var dir = pos360 - handTargetPosition;
                        var angle = Vector3.Angle(Vector3.up, dir);
                        angle = angle * (input.x > 0 ? 1 : input.x < 0 ? -1 : 1);
                        jumpRotation = Quaternion.LookRotation(-hit.normal);
                        jumpPoint = hit.point;
                        dragInfo.position = hit.point;
                        ClimbJump();
                    }
                }
            }
        }

        protected virtual void ClimbUpHandle()
        {
            if (inClimbJump || TP_Input.enabled || !TP_Input.mAnimator || !dragInfo.inDrag) return;

            if (inClimbUp && !inAlingClimb)
            {
                if (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".ClimbUpWall"))
                {
                    if (!TP_Input.mAnimator.IsInTransition(0))
                        TP_Input.mAnimator.MatchTarget(upPoint + Vector3.up * 0.1f, Quaternion.Euler(0, transform.eulerAngles.y, 0), AvatarTarget.RightHand, new MatchTargetWeightMask(new Vector3(0, 1, 1), 1), 0.1f, 0.4f);
                    if (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > .9f) ExitClimb();
                }
                return;
            }
            CheckClimbUp();
        }

        private void CheckClimbUp(bool ignoreInput = false)
        {
            var climbUpConditions = autoClimbEdge ? vertical > 0f : CrossPlatformInputManager.GetButtonDown("Jump");

            if (!canMoveClimb && !inClimbUp && (climbUpConditions || ignoreInput))
            {
                var dir = transform.forward;
                dir.y = 0;
                var startPoint = dragInfo.position + transform.forward * -0.1f;
                var endPoint = startPoint + Vector3.up * (TP_Input._capsuleCollider.height * 0.25f);
                var obstructionPoint = endPoint + dir.normalized * (climbUpMinSpace + 0.1f);
                var thicknessPoint = endPoint + dir.normalized * (climbUpMinThickness + 0.1f);
                var climbPoint = thicknessPoint + Vector3.down * (TP_Input._capsuleCollider.height * 0.5f);

                if (!Physics.Linecast(startPoint, endPoint, obstacle))
                {
                    if (debugRays && debugClimbUp) Debug.DrawLine(startPoint, endPoint, Color.green, 2f);
                    if (!Physics.Linecast(endPoint, obstructionPoint, obstacle))
                    {
                        if (debugRays && debugClimbUp) Debug.DrawLine(endPoint, obstructionPoint, Color.green, 2f);
                        if (Physics.Linecast(thicknessPoint, climbPoint, out hit, TP_Input.groundLayer))
                        {
                            if (debugRays && debugClimbUp) Debug.DrawLine(thicknessPoint, climbPoint, Color.green, 2f);
                            var angle = Vector3.Angle(Vector3.up, hit.normal);
                            var localUpPoint = transform.InverseTransformPoint(hit.point + (angle > 25 ? Vector3.up * TP_Input._capsuleCollider.radius : Vector3.zero) + dir * -(climbUpMinThickness * 0.5f));
                            localUpPoint.z = TP_Input._capsuleCollider.radius;
                            upPoint = transform.TransformPoint(localUpPoint);
                            if (Physics.Raycast(hit.point + Vector3.up * -0.05f, Vector3.up, out hit, TP_Input._capsuleCollider.height, obstacle))
                            {
                                if (hit.distance > TP_Input._capsuleCollider.height * 0.5f)
                                {
                                    if (hit.distance < TP_Input._capsuleCollider.height)
                                    {
                                        TP_Input.isCrouching = true;
                                        TP_Input.mAnimator.SetBool("IsCrouching", true);
                                    }
                                    ClimbUp();
                                }
                                else
                                {
                                    if (debugRays && debugClimbUp) Debug.DrawLine(upPoint, hit.point, Color.red, 2f);
                                }
                            }
                            else ClimbUp();
                        }
                        else if (debugRays && debugClimbUp) Debug.DrawLine(thicknessPoint, climbPoint, Color.red, 2f);
                    }
                    else if (debugRays && debugClimbUp) Debug.DrawLine(endPoint, obstructionPoint, Color.red, 2f);
                }
                else if (debugRays && debugClimbUp) Debug.DrawLine(startPoint, endPoint, Color.red, 2f);

            }
        }

        IEnumerator AlignClimb()
        {
            inAlingClimb = true;
            var transition = 0f;
            var dir = transform.forward;
            dir.y = 0;
            var angle = Vector3.Angle(Vector3.up, transform.forward);

            var targetRotation = Quaternion.LookRotation(-dragInfo.normal);
            var targetPosition = ((dragInfo.position + dir * -TP_Input._capsuleCollider.radius + Vector3.up * 0.1f) - transform.rotation * handTarget.localPosition);

            TP_Input.mAnimator.SetFloat("InputVertical", 1f);
            while (transition < 1 && Vector3.Distance(targetRotation.eulerAngles, transform.rotation.eulerAngles) > 0.2f && angle < 60)
            {
                TP_Input.mAnimator.SetFloat("InputVertical", 1f);
                transition += Time.deltaTime * 0.5f;
                targetPosition = ((dragInfo.position + dir * -TP_Input._capsuleCollider.radius) - transform.rotation * handTarget.localPosition);
                // + transform.right * root.x + transform.up * root.y;
                transform.position = Vector3.Slerp(transform.position, targetPosition, transition);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, transition);
                yield return null;
            }
            TP_Input.mAnimator.CrossFadeInFixedTime("ClimbUpWall", 0.1f);
            inAlingClimb = false;

        }

        protected virtual bool IsValidPoint(Vector3 normal, string tag)
        {
            if (!draggableTags.Contains(hit.transform.gameObject.tag)) return false;

            var angle = Vector3.Angle(Vector3.up, normal);
            if (angle >= minSurfaceAngle && angle <= maxSurfaceAngle)
                return true;
            return false;
        }

        protected virtual bool IsInLayerMask(int layer, LayerMask layermask)
        {
            return layermask == (layermask | (1 << layer));
        }

        #endregion

        #region Trigger Animations

        protected virtual void ClimbJump()
        {
            inClimbJump = true;
            TP_Input.mAnimator.SetFloat("InputHorizontal", input.x);
            TP_Input.mAnimator.SetFloat("InputVertical", input.y);
            TP_Input.mAnimator.CrossFadeInFixedTime("ClimbJump", 0.2f);
        }

        protected virtual void ClimbUp()
        {
            StartCoroutine(AlignClimb());
            inClimbUp = true;
        }

        protected virtual void EnterClimb()
        {
            oldInput = Time.time;

            TP_Input.enabled = false;
            TP_Input.enabled = false;
            TP_Input.Rigidbody.isKinematic = true;
            RaycastHit hit;
            var climbUpConditions = TP_Input.isGrounded && !Physics.Raycast(transform.position + Vector3.up * TP_Input._capsuleCollider.height, Vector3.up, TP_Input._capsuleCollider.height * 0.5f, obstacle) &&
                Physics.Raycast(transform.position + Vector3.up * (TP_Input._capsuleCollider.height * climbUpHeight), transform.forward, out hit, 1f, draggableWall) && draggableTags.Contains(hit.collider.gameObject.tag);
            TP_Input.mAnimator.SetBool("IsGrounded", true);
            TP_Input.mAnimator.CrossFadeInFixedTime(climbUpConditions ? "EnterClimbGrounded" : "EnterClimbAir", 0.2f);
            //if (!climbEnterGrounded)
            //    transform.rotation = Quaternion.LookRotation(-dragInfo.normal);
            transform.position = (dragInfo.position - transform.rotation * handTarget.localPosition);
            dragInfo.inDrag = true;
            onEnterClimb.Invoke();
        }

        protected virtual void ExitClimb()
        {
            oldInput = Time.time;
            dragInfo.inDrag = false;
            dragInfo.canGo = false;

            inClimbJump = false;
            TP_Input.Rigidbody.isKinematic = false;
            TP_Input.isJumping = false;
            if (!inClimbUp)
            {
                bool nextGround = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.5f, TP_Input.groundLayer);
                var charAngle = Vector3.Angle(transform.forward, Vector3.up);
                if (charAngle < 80)
                {
                    var dir = transform.forward;
                    dir.y = 0;

                    var postion = dragInfo.position + dir.normalized * -TP_Input._capsuleCollider.radius + Vector3.down * TP_Input._capsuleCollider.height;
                    transform.position = postion;
                }

                TP_Input.mAnimator.CrossFadeInFixedTime(nextGround ? "ExitGrounded" : "ExitAir", 0.2f);
            }
            else
            {
                TP_Input.verticalVelocity = 0;
                TP_Input.mAnimator.SetFloat("GroundDistance", 0);
            }

            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            TP_Input.enabled = true;
            TP_Input.enabled = true;
            inClimbUp = false;
            onExitClimb.Invoke();
        }

        #endregion

        #region RootMotion and AnimatorIK

        protected virtual void OnAnimatorMove()
        {
            if (TP_Input.enabled) return;

            climbEnterGrounded = (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".EnterClimbGrounded"));
            climbEnterAir = (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".EnterClimbAir"));

            if (dragInfo.inDrag && (canMoveClimb) && !inClimbUp && !inClimbJump && !climbEnterGrounded)
            {
                ApplyClimbMovement();
            }
            else if (inClimbJump)
            {
                ApplyClimbJump();
            }
            else if (inClimbUp || climbEnterGrounded || climbEnterAir)
            {
                if (!inClimbUp)
                    CheckClimbUp(true);

                ApplyRootMotion();
            }
        }

        protected virtual void OnAnimatorIK()
        {
            if (TP_Input.enabled || inClimbJump || inClimbUp || !dragInfo.inDrag) { ikWeight = 0; return; }
            ikWeight = Mathf.Lerp(ikWeight, 1f, 2f * Time.deltaTime);
            if (ikWeight > 0)
            {
                var lRoot = transform.InverseTransformPoint(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position);
                var rRoot = transform.InverseTransformPoint(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position);
                RaycastHit hit2;


                if (Physics.Raycast(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position + transform.forward * -0.5f + transform.up * -0.2f, transform.forward, out hit2, 1f, draggableWall))
                {
                    targetPositionL = transform.InverseTransformPoint(hit2.point);
                    if (debugRays && debugHandIK) Debug.DrawLine(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position + transform.forward * -0.5f + transform.up * -0.2f, hit2.point, Color.green);
                }
                else
                {
                    var center = transform.TransformPoint(0, lRoot.y, 0);
                    var target = rRoot;
                    if (Physics.Raycast(center, transform.forward, out hit2, 1f, draggableWall))
                    {
                        target = transform.InverseTransformPoint(hit2.point);
                    }
                    target.x = 0;
                    targetPositionL = Vector3.Lerp(targetPositionL, target, 5f * Time.deltaTime);
                    if (debugRays && debugHandIK) Debug.DrawRay(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.LeftHand).position + transform.forward * -0.5f + transform.up * -0.2f, transform.forward, Color.red);
                }

                if (Physics.Raycast(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position + transform.forward * -0.5f + transform.up * -0.2f, transform.forward, out hit2, 1f, draggableWall))
                {
                    targetPositionR = transform.InverseTransformPoint(hit2.point);
                    if (debugRays && debugHandIK) Debug.DrawLine(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position + transform.forward * -0.5f + transform.up * -0.2f, hit2.point, Color.green);
                }
                else
                {
                    var center = transform.TransformPoint(0, rRoot.y, 0);
                    var target = lRoot;
                    if (Physics.Raycast(center, transform.forward, out hit2, 1f, draggableWall))
                        target = transform.InverseTransformPoint(hit2.point);

                    target.x = 0;
                    targetPositionR = Vector3.Lerp(targetPositionR, target, 5f * Time.deltaTime);
                    if (debugRays && debugHandIK) Debug.DrawRay(TP_Input.mAnimator.GetBoneTransform(HumanBodyBones.RightHand).position + transform.forward * -0.5f + transform.up * -0.2f, transform.forward, Color.red);
                }
                var leftHandPosition = transform.position + transform.right * targetPositionL.x + transform.up * lRoot.y + transform.forward * targetPositionL.z;
                var rightHandPosition = transform.position + transform.right * targetPositionR.x + transform.up * rRoot.y + transform.forward * targetPositionR.z;
                lHandPos = transform.forward * offsetHandPositionL.z + transform.right * offsetHandPositionL.x + transform.up * offsetHandPositionL.y;// Vector3.Lerp(lHandPos, transform.forward * offsetHandPositionL.z + transform.right * offsetHandPositionL.x + transform.up * offsetHandPositionL.y, 2 * Time.deltaTime);
                rHandPos = transform.forward * offsetHandPositionR.z + transform.right * offsetHandPositionR.x + transform.up * offsetHandPositionR.y;// Vector3.Lerp(rHandPos, transform.forward * offsetHandPositionR.z + transform.right * offsetHandPositionR.x + transform.up * offsetHandPositionR.y, 2 * Time.deltaTime);
                leftHandPosition += lHandPos;
                rightHandPosition += rHandPos;

                TP_Input.mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikWeight);
                TP_Input.mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, ikWeight);

                TP_Input.mAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPosition);
                TP_Input.mAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPosition);
            }
            else
            {
                TP_Input.mAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
                TP_Input.mAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            }
        }

        void ApplyClimbMovement()
        {
            ///Apply Rotation
            CalculateMovementRotation();
            ///Apply Position
            posTransition = Mathf.Lerp(posTransition, 1f, 5 * Time.deltaTime);
            var root = transform.InverseTransformPoint(TP_Input.mAnimator.rootPosition);
            var position = (dragInfo.position - transform.rotation * handTarget.localPosition) + transform.right * root.x + transform.up * root.y;
            transform.position = Vector3.Lerp(transform.position, position, posTransition);
        }

        void CalculateMovementRotation()
        {
            var h = lastInput.x;
            var v = lastInput.y;
            var characterBase = transform.position + transform.up * (TP_Input._capsuleCollider.radius + offsetBase);
            var directionPoint = characterBase + transform.right * (h * lastPointDistanceH) + transform.up * (v * lastPointDistanceVUp);

            RaycastHit rotationHit;
            vLine centerLine = new vLine(characterBase, directionPoint);
            centerLine.Draw(Color.cyan, draw: debugRays && debugBaseRotation);
            var hasBasePoint = CheckBasePoint(out rotationHit);

            var basePoint = rotationHit.point;
            if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, draggableWall) && draggableTags.Contains(rotationHit.collider.gameObject.tag))
            {
                RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point);
                return;
            }

            centerLine.p1 = centerLine.p2;
            centerLine.p2 += transform.forward * (TP_Input._capsuleCollider.radius * 2);
            centerLine.Draw(Color.yellow, draw: debugRays && debugBaseRotation);

            if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, draggableWall) && draggableTags.Contains(rotationHit.collider.gameObject.tag))
            {
                RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point);
                return;
            }

            centerLine.p2 += (transform.right * lastPointDistanceH * -h * 2f) + (transform.up * lastPointDistanceVUp * -v) + transform.forward * TP_Input._capsuleCollider.radius;
            centerLine.Draw(Color.red, draw: debugRays && debugBaseRotation);

            if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, draggableWall) && draggableTags.Contains(rotationHit.collider.gameObject.tag))
            {
                RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point);
                return;
            }
        }

        bool CheckBasePoint(out RaycastHit baseHit)
        {
            var forward = new Vector3(transform.forward.x, 0, transform.forward.z);
            var characterBase = transform.position + transform.up * (TP_Input._capsuleCollider.radius + offsetBase) - forward * (TP_Input._capsuleCollider.radius * 2);

            var targetPoint = transform.position + forward * (1 + TP_Input._capsuleCollider.radius);
            vLine baseLine = new vLine(characterBase, targetPoint);

            if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, draggableWall) && draggableTags.Contains(baseHit.collider.gameObject.tag))
            {
                baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
                return true;
            }
            baseLine.Draw(Color.magenta, draw: debugRays);
            baseLine.p1 = baseLine.p2;
            baseLine.p2 = baseLine.p1 + forward + Vector3.up;

            if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, draggableWall) && draggableTags.Contains(baseHit.collider.gameObject.tag))
            {
                baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
                return true;
            }
            baseLine.Draw(Color.magenta, draw: debugRays);
            baseLine.p2 = baseLine.p1 + forward + Vector3.down;

            if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, draggableWall) && draggableTags.Contains(baseHit.collider.gameObject.tag))
            {
                baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
                return true;
            }
            baseLine.Draw(Color.magenta, draw: debugRays && debugBaseRotation);
            return false;
        }

        void RotateTo(Vector3 direction, Vector3 point)
        {
            var referenceDirection = point - dragInfo.position;
            if (debugRays && debugBaseRotation) Debug.DrawLine(point, dragInfo.position, Color.blue, .1f);
            var resultDirection = Quaternion.AngleAxis(-90, transform.right) * referenceDirection;
            var eulerX = Quaternion.LookRotation(resultDirection).eulerAngles.x;
            var baseRotation = Quaternion.LookRotation(direction);
            var resultRotation = Quaternion.Euler(eulerX, baseRotation.eulerAngles.y, baseRotation.eulerAngles.z);
            transform.rotation = Quaternion.Lerp(transform.rotation, resultRotation, (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1) * 0.2f);
        }

        void ApplyClimbJump()
        {
            if (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".ClimbJump"))
            {
                var pos = (jumpPoint - transform.rotation * handTarget.localPosition);
                if (!TP_Input.mAnimator.IsInTransition(0) && TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.25f)
                {
                    var percentage = (((TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime - 0.25f) / 0.8f) * 100f) * 0.01f;
                    transform.position = Vector3.Lerp(transform.position, pos, percentage);
                    transform.rotation = Quaternion.Lerp(transform.rotation, jumpRotation, percentage);
                }

                if (TP_Input.mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.8f)
                {
                    inClimbJump = false;
                    transform.position = pos;
                    transform.rotation = jumpRotation;
                }
            }
            if (Physics.Raycast(handTargetPosition + (transform.forward * -0.5f), transform.forward, out hit, 1, draggableWall))
            {
                if (IsValidPoint(hit.normal, hit.transform.gameObject.tag))
                {
                    dragInfo.canGo = true;
                    dragInfo.position = hit.point;
                    dragInfo.normal = hit.normal;
                }
            }
        }

        void ApplyRootMotion()
        {
            transform.position = TP_Input.mAnimator.rootPosition;
            transform.rotation = TP_Input.mAnimator.rootRotation;
            posTransition = 0;
        }

        class vLine
        {
            public Vector3 p1;
            public Vector3 p2;

            public vLine(Vector3 p1, Vector3 p2)
            {
                this.p1 = p1;
                this.p2 = p2;
            }
            public void Draw(float duration = 0, bool draw = true)
            {
                if (draw) Debug.DrawLine(p1, p2, Color.white, duration);
            }
            public void Draw(Color color, float duration = 0, bool draw = true)
            {
                if (draw) Debug.DrawLine(p1, p2, color, duration);
            }
        }
        #endregion
    }
}