using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        [System.Serializable]
        public class vMovementSpeed
        {
            [Tooltip("Rotation speed of the character")]
            public float rotationSpeed = 10f;
            [Tooltip("Character will walk by default and run when the sprint input is pressed. The Sprint animation will not play")]
            public bool walkByDefault = false;
            [Tooltip("Speed to Walk using rigibody force or extra speed if you're using RootMotion")]
            public float walkForwardSpeed = 2f;
            [Tooltip("Speed to Walk using rigibody force or extra speed if you're using RootMotion")]
            public float walkBackwardSpeed = 2f;
            [Tooltip("Speed to Run using rigibody force or extra speed if you're using RootMotion")]
            public float runningSpeed = 3f;
            [Tooltip("Speed to Sprint using rigibody force or extra speed if you're using RootMotion")]
            public float sprintSpeed = 4f;
            [Tooltip("Speed to Crouch using rigibody force or extra speed if you're using RootMotion")]
            public float crouchSpeed = 2f;
            [Tooltip("Speed to Crouch Sprint using rigibody force or extra speed if you're using RootMotion")]
            public float crouchSprintSpeed = 2f;
        }

        [Header("Motor")]
        public bool useRootMotion = false;
        public vMovementSpeed freeSpeed, strafeSpeed;

        [HideInInspector]
        public bool
            isGrounded,
            isCrouching,
            inCrouchArea,
            isSprinting,
            isSliding,
            stopMove,
            lockSpeed,
            autoCrouch;

        [HideInInspector]
        public bool
            isRolling,
            isJumping,
            isGettingUp,
            inTurn,
            quickStop,
            landHigh;

        [HideInInspector]
        public bool customAction;
        [HideInInspector]
        public bool actions
        {
            get
            {
                return isRolling || quickStop || landHigh || customAction;
            }
        }

        #region Direction Variables
        [HideInInspector]
        public bool forceRootMotion;
        [HideInInspector]
        public Vector3 targetDirection;
        [HideInInspector]
        public Quaternion targetRotation;
        [HideInInspector]
        public float strafeMagnitude;
        [HideInInspector]
        public Quaternion freeRotation;
        [HideInInspector]
        public bool keepDirection;
        [HideInInspector]
        public Vector2 oldInput;
        [HideInInspector]
        public Vector2 input;
        [HideInInspector]
        public Vector2 inputAir;                               // generate input for the controller    
        [HideInInspector]
        public float velocity;                               // velocity to apply to rigdibody
        #endregion

        public float speed, direction, verticalVelocity;
        private float dampTIme = 0.2f;

        protected float mForwardSpeed = 4.0f;
        protected float mBackwardSpeed = 3.2f;
        protected float mCrouchForward = 3.0f;
        protected float mCrouchBackward = 2.3f;
        protected float mGlideForward = 2.3f;
        protected float mBailOutForward = 2.3f;

        private void UpdateMotor()
        {
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            input.x = h;
            input.y = v;

            if ((!isGrounded && !jumpAirControl) || lockMovement || isAttack || actions)
            {
                StopMove();
                return;
            }
            ControlCapsuleHeight();
            ControlLocomotion();
        }

        public void ControlCapsuleHeight()
        {
            if (isCrouching || isRolling || landHigh)
            {
                _capsuleCollider.center = colliderCenter / 1.5f;
                _capsuleCollider.height = colliderHeight / 1.5f;
            }
            else
            {
                // back to the original values
                _capsuleCollider.center = colliderCenter;
                _capsuleCollider.radius = colliderRadius;
                _capsuleCollider.height = colliderHeight;
            }
        }

        private void ControlLocomotion()
        {
            if (lockMovement || currentHealth <= 0) return;

            if (isStrafing)
            {
                StrafeMovement();
            }
            else
            {
                FreeMovement();
            }
        }

        public void FreeMovement()
        {
            // set speed to both vertical and horizontal inputs
            speed = Mathf.Abs(input.x) + Mathf.Abs(input.y);
            // limits the character to walk by default
            if (freeSpeed.walkByDefault)
                speed = Mathf.Clamp(speed, 0, 0.5f);
            else
                speed = Mathf.Clamp(speed, 0, 1f);
            // add 0.5f on sprint to change the animation on animator
            if (isSprinting) speed += 0.5f;
            if (stopMove || lockSpeed) speed = 0f;

            Animator.SetFloat("InputMagnitude", speed, dampTIme, Time.deltaTime);

            var conditions = (!actions || quickStop || isRolling);
            if (input != Vector2.zero && targetDirection.magnitude > 0.1f && conditions)
            {
                Vector3 lookDirection = targetDirection.normalized;
                freeRotation = Quaternion.LookRotation(lookDirection, transform.up);
                var diferenceRotation = freeRotation.eulerAngles.y - transform.eulerAngles.y;
                var eulerY = transform.eulerAngles.y;
                // apply free directional rotation while not turning180 animations
//                if (isGrounded || (!isGrounded))
                {
                    if (diferenceRotation < 0 || diferenceRotation > 0) eulerY = freeRotation.eulerAngles.y;
                    var euler = new Vector3(transform.eulerAngles.x, eulerY, transform.eulerAngles.z);
                    if (inTurn) return;
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(euler), freeSpeed.rotationSpeed * Time.deltaTime);
                }
                if (!keepDirection)
                    oldInput = input;
                if (Vector2.Distance(oldInput, input) > 0.9f && keepDirection)
                    keepDirection = false;
            }
        }

        public virtual void StrafeMovement()
        {
            StrafeLimitSpeed(.8f);

            if (stopMove) strafeMagnitude = 0f;
            Animator.SetFloat("InputMagnitude", strafeMagnitude, dampTIme, Time.deltaTime);
        }

        protected virtual void StrafeLimitSpeed(float value)
        {
            var limitInput = isSprinting ? value + 0.5f : value;
            var _input = input * limitInput;
            var _speed = Mathf.Clamp(_input.y, -limitInput, limitInput);
            var _direction = Mathf.Clamp(_input.x, -limitInput, limitInput);
            speed = _speed;
            direction = _direction;
            var newInput = new Vector2(speed, direction);
            strafeMagnitude = Mathf.Clamp(newInput.magnitude, 0, limitInput);
        }

        public void OnAnimatorMove()
        {
            if (isAttack)
            {
                transform.rotation = Animator.rootRotation;
                transform.position = Animator.rootPosition;
                return;
            }
            if (!isGrounded && !jumpAirControl || lockMovement || customAction)
            {
                StopMove();
                return;
            }
            if (!this.enabled) return;

            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (isGrounded)
            {
                transform.rotation = Animator.rootRotation;

                //strafe extra speed
                if (isStrafing)
                {
                    var _speed = Mathf.Abs(strafeMagnitude);
                    float velocity = .0f;
                    if (input.y > 0 && input.y > input.x)
                        velocity = isCrouching ? mCrouchForward : mForwardSpeed;
                    else
                        velocity = isCrouching ? mCrouchBackward : mBackwardSpeed;
                    float fWalkSpeed = input.y > 0 ? strafeSpeed.walkForwardSpeed : strafeSpeed.walkBackwardSpeed;
                    if (isCrouching)
                    {
                        if (_speed <= 0.5f)
                            ControlSpeed(strafeSpeed.crouchSpeed * velocity);
                        else if (_speed <= 1.0f)
                            ControlSpeed(velocity);
                        else
                            ControlSpeed(strafeSpeed.crouchSprintSpeed * velocity);
                    }
                    else
                    {
                        if (_speed <= 0.5f)
                            //ControlSpeed(strafeSpeed.walkForwardSpeed * velocity);
                            ControlSpeed(fWalkSpeed * velocity);
                        else if (_speed <= 1f)
                            ControlSpeed(velocity);
                        else
                            ControlSpeed(strafeSpeed.sprintSpeed * velocity);
                    }

                }
                else if (!isStrafing)
                {
                    //free extra speed
                    if (speed <= 0.5f)
                        ControlSpeed(freeSpeed.walkForwardSpeed);
                    else if (speed > 0.5 && speed <= 1f)
                        ControlSpeed(freeSpeed.runningSpeed);
                    else
                        ControlSpeed(freeSpeed.sprintSpeed);

                    if (isCrouching)
                        ControlSpeed(freeSpeed.crouchSpeed);
                }
            }
            else if (IsAirMoving())
            {
                if (IsGlide())
                {
                    float velocity = mGlideForward;
                    ControlSpeed(velocity);
                }
                else
                {
                    float velocity = inputAir.y > 0 ? mBailOutForward : .0f;
                    ControlSpeed(velocity);
                }
            }
            else
            {
                //free extra speed
                if (speed <= 0.5f)
                    ControlSpeed(freeSpeed.walkForwardSpeed);
                else if (speed > 0.5 && speed <= 1f)
                    ControlSpeed(freeSpeed.runningSpeed);
                else
                    ControlSpeed(freeSpeed.sprintSpeed);

                if (isCrouching)
                    ControlSpeed(freeSpeed.crouchSpeed);
            }
        }

        public virtual void ControlSpeed(float velocity)
        {
            if (Time.deltaTime == 0 || isRolling) return;

            // use RootMotion and extra speed values to move the character
            if (useRootMotion && !actions && !customAction)
            {
                this.velocity = velocity;
                var deltaPosition = new Vector3(Animator.deltaPosition.x, transform.position.y, Animator.deltaPosition.z);
                Vector3 v = (deltaPosition * (velocity > 0 ? velocity : 1f)) / Time.deltaTime;
                v.y = Rigidbody.velocity.y;
                Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, v, 20f * Time.deltaTime);
            }
            // use only RootMotion 
            else if (actions || customAction || lockMovement || forceRootMotion)
            {
                this.velocity = velocity;
                Vector3 v = Vector3.zero;
                v.y = Rigidbody.velocity.y;
                Rigidbody.velocity = v;
                transform.position = Animator.rootPosition;
                if (forceRootMotion)
                    transform.rotation = Animator.rootRotation;
            }
            else if (IsAirMoving())
            {
                AirMoveVelocity(velocity);
            }
            //use only Rigibody Force to move the character (ideal for 'inplace' animations and better behaviour in general) 
            else
            {
                if (isStrafing)
                {
                    StrafeVelocity(velocity);
                }
                else
                {
                    FreeVelocity(velocity);
                }
            }
        }

        protected virtual void StrafeVelocity(float velocity)
        {
            if (lockMovement)
            {
                return;
            }
            float fMoveYEx = .0f;
            float fMoveXEx = .0f;
            Vector3 v = Vector3.zero;
            if (direction > 1.0f || speed > 1.0f)
            {
                v = (transform.TransformDirection(new Vector3(direction + fMoveXEx, 0, speed + fMoveYEx).normalized) * (velocity > 0 ? velocity : 1f));
            }
            else
            {
                v = transform.TransformDirection(new Vector3(direction + fMoveXEx, 0, speed + fMoveYEx) * (velocity > 0 ? velocity : 1f));
            }
            // 根据人物确认是否跑动
            Vector3 vReal = Rigidbody.velocity;
            vReal.y = .0f;
            if (vReal.magnitude > .0f && v.magnitude > .0f)
            {
                float fMultiple = vReal.magnitude / v.magnitude;
                fMultiple = Mathf.Clamp(fMultiple, .0f, 1.0f);
                float fSpeed = speed * fMultiple;
                float fDirection = direction * fMultiple;
                var newInput = new Vector2(fSpeed, fDirection);
                strafeMagnitude = newInput.magnitude;
                if (strafeMagnitude > 1.0f && fSpeed < 1.1f)
                {
                    strafeMagnitude = 1.0f;
                }
            }
            else
            {
                strafeMagnitude = .0f;
            }
            //
            v.y = Rigidbody.velocity.y;
            if (v.magnitude > 3.5f)
            {
                int i = 0;
                ++i;
            }
            Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, v, 20f * Time.deltaTime);
        }

        protected virtual void FreeVelocity(float velocity)
        {
                        var _targetVelocity = transform.forward * velocity * speed;
            _targetVelocity.y = Rigidbody.velocity.y;
            Rigidbody.velocity = _targetVelocity;
            Rigidbody.AddForce(transform.forward * (velocity * speed) * Time.deltaTime, ForceMode.VelocityChange);

//            var velY = transform.forward * velocity * speed;
//            velY.y = _rigidbody.velocity.y;
//            var velX = transform.right * velocity * direction;
//            velX.x = _rigidbody.velocity.x;
//
//            if (isStrafing)
//            {
//                Vector3 v = (transform.TransformDirection(new Vector3(input.x, 0, input.y)) * (velocity > 0 ? velocity : 1f));
//                v.y = _rigidbody.velocity.y;
//                _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, v, 20f * Time.deltaTime);
//            }
//            else
//            {
//                _rigidbody.velocity = velY;
//                _rigidbody.AddForce(transform.forward * (velocity * speed) * Time.deltaTime, ForceMode.VelocityChange);
//            }
        }

        public void EnableGravityAndCollision(float normalizedTime)
        {
            // enable collider and gravity at the end of the animation
            if (baseLayerInfo.normalizedTime >= normalizedTime)
            {
                _capsuleCollider.isTrigger = false;
                Rigidbody.useGravity = true;
            }
        }

        public void ResetPlayerMotor()
        {
            Animator.SetFloat("InputMagnitude", 0);
            direction = 0;
            speed = 0;
            Rigidbody.velocity = Vector3.zero;
        }

        public void OnAnimatorMoveSwim()
        {
            if (isGrounded)
            {
                transform.rotation = Animator.rootRotation;

                //strafe extra speed
                if (isStrafing)
                {
                    var _speed = Mathf.Abs(strafeMagnitude);
                    float velocity = .0f;
                    if (input.y > 0 && input.y > input.x)
                        velocity = isCrouching ? mCrouchForward : mForwardSpeed;
                    else
                        velocity = isCrouching ? mCrouchBackward : mBackwardSpeed;
                    float fWalkSpeed = input.y > 0 ? strafeSpeed.walkForwardSpeed : strafeSpeed.walkBackwardSpeed;
                    if (isCrouching)
                    {
                        if (_speed <= 0.5f)
                            ControlSpeed(strafeSpeed.crouchSpeed * velocity);
                        else if (_speed <= 1.0f)
                            ControlSpeed(velocity);
                        else
                            ControlSpeed(strafeSpeed.crouchSprintSpeed * velocity);
                    }
                    else
                    {
                        if (_speed <= 0.5f)
                            //ControlSpeed(strafeSpeed.walkForwardSpeed * velocity);
                            ControlSpeed(fWalkSpeed * velocity);
                        else if (_speed <= 1f)
                            ControlSpeed(velocity);
                        else
                            ControlSpeed(strafeSpeed.sprintSpeed * velocity);
                    }

                }
                else if (!isStrafing)
                {
                    //free extra speed
                    if (speed <= 0.5f)
                        ControlSpeed(freeSpeed.walkForwardSpeed);
                    else if (speed > 0.5 && speed <= 1f)
                        ControlSpeed(freeSpeed.runningSpeed);
                    else
                        ControlSpeed(freeSpeed.sprintSpeed);

                    if (isCrouching)
                        ControlSpeed(freeSpeed.crouchSpeed);
                }
            }
            else if (IsAirMoving())
            {
                if (IsGlide())
                {
                    float velocity = mGlideForward;
                    ControlSpeed(velocity);
                }
                else
                {
                    float velocity = inputAir.y > 0 ? mBailOutForward : .0f;
                    ControlSpeed(velocity);
                }
            }
            else
            {
                //free extra speed
                if (speed <= 0.5f)
                    ControlSpeed(freeSpeed.walkForwardSpeed);
                else if (speed > 0.5 && speed <= 1f)
                    ControlSpeed(freeSpeed.runningSpeed);
                else
                    ControlSpeed(freeSpeed.sprintSpeed);

                if (isCrouching)
                    ControlSpeed(freeSpeed.crouchSpeed);
            }
        }

        [Tooltip("Select the layers the your character will stop moving when close to")]
        public LayerMask stopMoveLayer;
        [Tooltip("[RAYCAST] Stopmove Raycast Height")]
        public float stopMoveHeight = 0.65f;
        [Tooltip("[RAYCAST] Stopmove Raycast Distance")]
        public float stopMoveDistance = 0.5f;

        protected void StopMove()
        {
            if (input.sqrMagnitude < 0.1 || !isGrounded) return;

            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position + new Vector3(0, stopMoveHeight, 0), targetDirection.normalized);

            if (Physics.Raycast(ray, out hitinfo, _capsuleCollider.radius + stopMoveDistance, stopMoveLayer))
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitinfo.distance <= stopMoveDistance && hitAngle > 85)
                    stopMove = true;
                else if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer))
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else
                stopMove = false;
        }

    }//class end
}
