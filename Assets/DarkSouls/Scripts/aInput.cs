using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class aInput : MonoBehaviour
{
    public static aInput Instance;

    private Animator animator;
    Vector2 input;
    public float speed, direction, verticalVelocity;    // general variables to the locomotion

    private float dampTIme = 0.2f;

    [HideInInspector]
    public bool
        isGrounded,
        isCrouching,
        inCrouchArea,
        isSprinting,
        isSliding,
        stopMove,
        autoCrouch;
    // action bools
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

    private bool _isStrafing = false;
    public bool lockMovement;
    public bool lockInStrafe;
    public bool isStrafing
    {
        get
        {
            return _isStrafing || lockInStrafe;
        }
        set
        {
            _isStrafing = value;
        }
    }

    private int attackId = 0;
    private bool isAttack = false;

    Transform m_Pivot,m_CamRig;

    // Use this for initialization
    void Start()
    {
        Instance = this;
        animator = GetComponent<Animator>();
        m_Pivot = Camera.main.transform.parent;
        m_CamRig = Camera.main.transform.parent.parent;
    }

    public virtual void UpdateTargetDirection(Transform referenceTransform = null)
    {
        if (referenceTransform)
        {
            var forward = keepDirection ? referenceTransform.forward : referenceTransform.TransformDirection(Vector3.forward);
            forward.y = 0;

            forward = keepDirection ? forward : referenceTransform.TransformDirection(Vector3.forward);
            forward.y = 0; //set to 0 because of referenceTransform rotation on the X axis

            //get the right-facing direction of the referenceTransform
            var right = keepDirection ? referenceTransform.right : referenceTransform.TransformDirection(Vector3.right);

            // determine the direction the player will face based on input and the referenceTransform's right and forward directions
            targetDirection = input.x * right + input.y * forward;
        }
        else
            targetDirection = keepDirection ? targetDirection : new Vector3(input.x, 0, input.y);
    }

    private void Update()
    {
        if (isStrafing && CrossPlatformInputManager.GetButtonDown("Jump"))
        {
            animator.SetInteger("AttackID", attackId);
            animator.SetTrigger("Attack");
        }

        if (isAttack)
        {
            return;
        }

        // read inputs
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

    public vMovementSpeed freeSpeed;
    // one bool to rule then all
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

        animator.SetFloat("InputMagnitude", speed, dampTIme, Time.deltaTime);

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
        animator.SetFloat("InputMagnitude", strafeMagnitude, dampTIme, Time.deltaTime);
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
        //Debug.LogError("direction:"+ newInput + ",strafeMagnitude:" + strafeMagnitude);
    }

    private void LocomotionAnimation()
    {
        if (isStrafing)
        {
            // strafe movement get the input 1 or -1
            animator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
        }
        animator.SetFloat("InputVertical", speed, dampTIme, Time.deltaTime);
    }

    void LateUpdate()
    {
        CameraInput();
    }

    private float gap = 1f;
    private void CameraInput()
    {
        if (isAttack)
        {
            return;
        }
        if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
        if (!keepDirection) UpdateTargetDirection(Camera.main.transform);
        RotateWithCamera(Camera.main.transform);

        //var Y = CrossPlatformInputManager.GetAxisRaw("Mouse Y");
        //var X = CrossPlatformInputManager.GetAxisRaw("Mouse X");
        //if (X > 0)
        //{
        //    X = 0.1f;
        //}
        //else if (X < 0)
        //{
        //    X = -0.1f;
        //}
        //if (Y > 0)
        //{
        //    Y = 0.1f;
        //}
        //else if (Y < 0)
        //{
        //    Y = -0.1f;
        //}
        //Debug.Log(X + " , " + Y);

        //RotateCamera(X, Y);
    }

    protected virtual void RotateWithCamera(Transform cameraTransform)
    {
        if (isStrafing && !actions && !lockMovement)
        {
            RotateWithAnotherTransform(cameraTransform);
        }
    }

    public virtual void RotateWithAnotherTransform(Transform referenceTransform)
    {
        var newRotation = new Vector3(transform.eulerAngles.x, referenceTransform.eulerAngles.y, transform.eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(newRotation), freeSpeed.rotationSpeed * Time.fixedDeltaTime);
        targetRotation = transform.rotation;
    }

    [SerializeField]
    private float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
    [Range(0f, 10f)]
    [SerializeField]
    private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
    [SerializeField]
    private float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
    [SerializeField]
    private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
    [SerializeField]
    private float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
    [SerializeField]
    private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
    [SerializeField]
    private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return

    private float m_LookAngle;                    // The rig's y axis rotation.
    private float m_TiltAngle;                    // The pivot's x axis rotation.
    private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
    private Vector3 m_PivotEulers;
    private Quaternion m_PivotTargetRot;
    private Quaternion m_TransformTargetRot;

    public void RotateCamera(float x, float y)
    {
        if (isAttack)
        {
            return;
        }

        if (x == 0 && y == 0)
        {
            return;
        }

        if (Time.timeScale < float.Epsilon)
            return;

        m_LookAngle += x * m_TurnSpeed;

        // Rotate the rig (the root object) around Y axis only:
        //m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

        //if (m_VerticalAutoReturn)
        //{
        //    // For tilt input, we need to behave differently depending on whether we're using mouse or touch input:
        //    // on mobile, vertical input is directly mapped to tilt value, so it springs back automatically when the look input is released
        //    // we have to test whether above or below zero because we want to auto-return to zero even if min and max are not symmetrical.
        //    m_TiltAngle = y > 0 ? Mathf.Lerp(0, -m_TiltMin, y) : Mathf.Lerp(0, m_TiltMax, -y);
        //}
        //else
        {
            // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
            m_TiltAngle -= y * m_TurnSpeed;
            // and make sure the new value is within the tilt range
            m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);
        }

        // Tilt input around X is applied to the pivot (the child of this object)
        //m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y, m_PivotEulers.z);
        m_TransformTargetRot = Quaternion.Euler(m_TiltAngle, m_LookAngle, 0f);
        //if (m_TurnSmoothing > 0)
        //{
        //    m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * Time.deltaTime);
        //    m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_TransformTargetRot, m_TurnSmoothing * Time.deltaTime);
        //}
        //else
        {
            //m_Pivot.localRotation = m_PivotTargetRot;
            //m_CamRig.localRotation = m_TransformTargetRot;
            m_CamRig.localRotation = Quaternion.Lerp(m_CamRig.localRotation, m_TransformTargetRot, 1);
        }
    }

    private int combo = 0;
    public void OnEnableAttack()
    {
        combo++;
        isAttack = true;
    }

    public void OnDisableAttack()
    {
        combo--;
        if (combo == 0)
        {
            isAttack = false;
        }
    }

    public void ResetAttackTriggers()
    {
        animator.ResetTrigger("Attack");
        //animator.ResetTrigger("StrongAttack");
    }

    public void SwitchStrafe()
    {
        isStrafing = !isStrafing;
        animator.SetBool("IsStrafing", isStrafing);
    }



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
}