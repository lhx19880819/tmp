using UnityEngine;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        private const float Mass_Normal = 50.0f;
        private const float Mass_Max = 9999.0f;

        [Header("Jump")]
        public LayerMask groundLayer = 1 << 0;

        [Tooltip("Check to control the character while jumping")]
        public bool jumpAirControl = true;
        [Tooltip("How much time the character will be jumping")]
        public float jumpTimer = 0.3f;
        [HideInInspector]
        public float jumpCounter;
        [Tooltip("Add Extra jump speed, based on your speed input the character will move forward")]
        public float jumpForward = 3f;
        [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
        public float jumpHeight = 4f;
        [Tooltip("Distance to became not grounded")]
        [SerializeField]
        protected float groundMinDistance = 0.25f;
        [SerializeField]
        protected float groundMaxDistance = 0.5f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
        public float stepOffsetEnd = 0.45f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
        public float stepOffsetStart = 0f;
        [Tooltip("Higher value will result jittering on ramps, lower values will have difficulty on steps")]
        public float stepSmooth = 4f;
        [Tooltip("Max angle to walk")]
        public float slopeLimit = 45f;
        [Tooltip("Apply extra gravity when the character is not grounded")]
        public float extraGravity = -10f;
        [Tooltip("Apply extra gravity when the character is not grounded and rigidbody's velocity go up!")]
        public float jumpExtraGravity = -14f;
        [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
        public float ragdollVel = -16f;
        [Tooltip("Turn Rotate")]
        public float m_rotateTurn = 60.0f;

        [HideInInspector]
        public PhysicMaterial frictionPhysics;
        [HideInInspector]
        public PhysicMaterial maxFrictionPhysics;
        [HideInInspector]
        public PhysicMaterial slippyPhysics;       // create PhysicMaterial for the Rigidbody
        [HideInInspector]
        public float colliderRadius, colliderHeight;        // storage capsule collider extra information        
        [HideInInspector]
        public Vector3 colliderCenter;

        RaycastHit groundHit;
        Rigidbody _rigidbody;                                // access the Rigidbody component
        CapsuleCollider _capsuleCollider;
        float groundDistance;

        private void InitJump()
        {
            frictionPhysics = new PhysicMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = .25f;
            frictionPhysics.dynamicFriction = .25f;
            frictionPhysics.frictionCombine = PhysicMaterialCombine.Multiply;

            // prevents the collider from slipping on ramps
            maxFrictionPhysics = new PhysicMaterial();
            maxFrictionPhysics.name = "maxFrictionPhysics";
            maxFrictionPhysics.staticFriction = 1f;
            maxFrictionPhysics.dynamicFriction = 1f;
            maxFrictionPhysics.frictionCombine = PhysicMaterialCombine.Maximum;

            // air physics 
            slippyPhysics = new PhysicMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;
            slippyPhysics.frictionCombine = PhysicMaterialCombine.Minimum;

            _capsuleCollider = GetComponent<CapsuleCollider>();
            colliderCenter = _capsuleCollider.center;
            colliderRadius = _capsuleCollider.radius;
            colliderHeight = _capsuleCollider.height;
            _rigidbody = GetComponent<Rigidbody>();
        }

        public void Jump()
        {
            if (customAction || isAttack) return;
            bool jumpConditions = !isCrouching && isGrounded && !actions && !isJumping;
            if (!jumpConditions) return;
            jumpCounter = jumpTimer;
            isJumping = true;
            if (input.sqrMagnitude < 0.1f)
                mAnimator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                mAnimator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        private void ControlJumpBehaviour()
        {
            if (!isJumping) return;

            jumpCounter -= Time.deltaTime;
            if (jumpCounter <= 0)
            {
                jumpCounter = 0;
                isJumping = false;
            }
            // apply extra force to the jump height   
            var vel = _rigidbody.velocity;
            vel.y = jumpHeight;
            _rigidbody.velocity = vel;
        }

        private void UpdateJump()
        {
            CheckGroundDistance();
            CheckGround();
            ControlJumpBehaviour();
        }

        void CheckGroundDistance()
        {
            if (isDead) return;
            if (_capsuleCollider != null)
            {
                // radius of the SphereCast
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = 10f;
                // position of the SphereCast origin starting at the base of the capsule
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                // ray for RayCast
                Ray ray1 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);
                // ray for SphereCast
                Ray ray2 = new Ray(pos, -Vector3.up);
                // raycast for check the ground distance
                if (Physics.Raycast(ray1, out groundHit, colliderHeight / 2 + 4f, groundLayer))
                    dist = transform.position.y - groundHit.point.y;
                // sphere cast around the base of the capsule to check the ground distance
                if (Physics.SphereCast(ray2, radius, out groundHit, _capsuleCollider.radius + 2f, groundLayer))
                {
                    // check if sphereCast distance is small than the ray cast distance
                    if (dist > (groundHit.distance - _capsuleCollider.radius * 0.1f))
                        dist = (groundHit.distance - _capsuleCollider.radius * 0.1f);
                }
                groundDistance = (float)System.Math.Round(dist, 2);
            }
        }

        private void CheckGround()
        {
            if (isDead || customAction)
            {
                isGrounded = true;
                return;
            }

            // change the physics material to very slip when not grounded
            _capsuleCollider.material = (isGrounded && GroundAngle() <= slopeLimit + 1) ? frictionPhysics : slippyPhysics;

            if (isGrounded && input == Vector2.zero)
                _capsuleCollider.material = maxFrictionPhysics;
            else if (isGrounded && input != Vector2.zero)
                _capsuleCollider.material = frictionPhysics;
            else
                _capsuleCollider.material = slippyPhysics;

            // we don't want to stick the character grounded if one of these bools is true
            bool checkGroundConditions = !isRolling;

            var magVel = (float)System.Math.Round(new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z).magnitude, 2);
            magVel = Mathf.Clamp(magVel, 0, 1);

            var groundCheckDistance = groundMinDistance;
            if (magVel > 0.25f) groundCheckDistance = groundMaxDistance;

            if (checkGroundConditions)
            {
                // clear the checkground to free the character to attack on air
                var onStep = StepOffset();

                if (groundDistance <= 0.01f && !isJumping)
                {
                    //isGrounded = true;
                    OnGround();
                    Sliding();
                    verticalVelocity = 0f;
                }
                else
                {
                    if (groundDistance >= groundCheckDistance || isJumping)
                    {
                        OnOffGround();
                        // check vertical velocity
                        verticalVelocity = _rigidbody.velocity.y;
                        // apply extra gravity when falling
                        // if (!onStep && !isJumping)
                        // {
                        //     _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                        // }
                        if (IsAirMoving())
                        {
                            if (IsBailOutControl())
                            {
                                if (IsGlide())
                                {
                                    if (verticalVelocity < -m_fMaxGlideSpeed)
                                    {
                                        float fDiff = verticalVelocity + m_fMaxGlideSpeed;
                                        if (fDiff < -10)
                                        {
                                            _rigidbody.AddForce(transform.up * m_fResistance_Glide * Time.deltaTime * 10, ForceMode.VelocityChange);
                                        }
                                        else
                                        {
                                            _rigidbody.AddForce(transform.up * m_fResistance_Glide * Time.deltaTime, ForceMode.VelocityChange);
                                        }
                                    }
                                    else if (verticalVelocity > -m_fMinGlideSpeed)
                                    {
                                        _rigidbody.AddForce(transform.up * (-m_fForceGravity_Glide) * Time.deltaTime, ForceMode.VelocityChange);
                                    }
                                }
                                else
                                {
                                    if (verticalVelocity > -m_fMinBailOutSpeed)
                                    {
                                        _rigidbody.AddForce(transform.up * (-m_fForceGravity) * Time.deltaTime, ForceMode.VelocityChange);

                                    }
                                    else if (verticalVelocity < -m_fMaxBailOutSpeed)
                                    {
                                        _rigidbody.AddForce(transform.up * m_fResistance * Time.deltaTime, ForceMode.VelocityChange);
                                    }
                                }
                            }
                        }
                        else if (!onStep && !isJumping)
                        {
                            if (_rigidbody.velocity.y > .0f)
                                _rigidbody.AddForce(transform.up * jumpExtraGravity * Time.deltaTime, ForceMode.VelocityChange);
                            else
                                _rigidbody.AddForce(transform.up * extraGravity * Time.deltaTime, ForceMode.VelocityChange);
                        }
                    }
                    else if (!onStep && !isJumping)
                    {
                        _rigidbody.AddForce(transform.up * (extraGravity * 2 * Time.deltaTime), ForceMode.VelocityChange);
                    }
                }
            }
        }

        void OnGround()
        {
            if (!isGrounded)
            {
                _rigidbody.mass = Mass_Max;
                _rigidbody.velocity = Vector3.zero;
            }
            isGrounded = true;
            if (groundHit.transform != null)
            {
                int maskTmp = (1 << groundHit.transform.gameObject.layer);
            }
        }

        void OnOffGround()
        {
            if (isGrounded)
            {
                _rigidbody.mass = Mass_Normal;
            }
            isGrounded = false;
        }

        void Sliding()
        {
            var onStep = StepOffset();
            var groundAngleTwo = 0f;
            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position, -transform.up);

            if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer))
            {
                groundAngleTwo = Vector3.Angle(Vector3.up, hitinfo.normal);
            }

            if (GroundAngle() > slopeLimit + 1f && GroundAngle() <= 85 &&
                groundAngleTwo > slopeLimit + 1f && groundAngleTwo <= 85 &&
                groundDistance <= 0.05f && !onStep)
            {
                isSliding = true;
                isGrounded = false;
                var slideVelocity = (GroundAngle() - slopeLimit) * 2f;
                slideVelocity = Mathf.Clamp(slideVelocity, 0, 10);
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, -slideVelocity, _rigidbody.velocity.z);
            }
            else
            {
                isSliding = false;
                isGrounded = true;
            }
        }

        bool StepOffset()
        {
            // test 临时去掉阶梯
            return false;
        }

        public virtual float GroundAngle()
        {
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;
        }
    }//class end
}
