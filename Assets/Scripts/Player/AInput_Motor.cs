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
            public float walkSpeed = 2f;
            [Tooltip("Speed to Run using rigibody force or extra speed if you're using RootMotion")]
            public float runningSpeed = 3f;
            [Tooltip("Speed to Sprint using rigibody force or extra speed if you're using RootMotion")]
            public float sprintSpeed = 4f;
            [Tooltip("Speed to Crouch using rigibody force or extra speed if you're using RootMotion")]
            public float crouchSpeed = 2f;
        }
        private Animator mAnimator;
        private float speed, direction, verticalVelocity; 
        private float dampTIme = 0.2f;
        private Vector2 input;

        [HideInInspector]
        public bool
            isGrounded,
            isCrouching,
            inCrouchArea,
            isSprinting,
            isSliding,
            stopMove,
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
        public vMovementSpeed freeSpeed;
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
        #endregion

        private void UpdateMotor()
        {
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");
            input.x = h;
            input.y = v;

            ControlLocomotion();

            LocomotionAnimation();
        }

        private void ControlLocomotion()
        {
            if (isStrafing)
            {
                StrafeMovement();
            }
            else
            {
                FreeMovement();
            }
        }

        private void LocomotionAnimation()
        {
            if (isStrafing)
            {
                // strafe movement get the input 1 or -1
                mAnimator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
            }
            mAnimator.SetFloat("InputVertical", speed, dampTIme, Time.deltaTime);
        }

        private void FreeMovement()
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
            //if (stopMove || lockSpeed) speed = 0f;

            mAnimator.SetFloat("InputMagnitude", speed, dampTIme, Time.deltaTime);

            var conditions = (!actions || quickStop || isRolling);
            if (input != Vector2.zero && targetDirection.magnitude > 0.1f && conditions)
            {
                Vector3 lookDirection = targetDirection.normalized;
                freeRotation = Quaternion.LookRotation(lookDirection, transform.up);
                var diferenceRotation = freeRotation.eulerAngles.y - transform.eulerAngles.y;
                var eulerY = transform.eulerAngles.y;
                // apply free directional rotation while not turning180 animations
                if (isGrounded || (!isGrounded))
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
            isStrafing = true;

            StrafeLimitSpeed(.8f);

            if (stopMove) strafeMagnitude = 0f;
            mAnimator.SetFloat("InputMagnitude", strafeMagnitude, dampTIme, Time.deltaTime);
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
    }//class end
}
