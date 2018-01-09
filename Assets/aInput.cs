using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public class aInput : MonoBehaviour
{
    private Animator animator;
    Vector2 input;
    public float speed, direction, verticalVelocity;    // general variables to the locomotion
    public float strafeMagnitude;

    private float dampTIme = 0.2f;

    public bool
        isGrounded,
        isCrouching,
        inCrouchArea,
        isSprinting,
        isSliding,
        stopMove,
        autoCrouch;

    private bool _isStrafing;
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

    // Use this for initialization
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (CrossPlatformInputManager.GetButtonDown("Jump"))
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

        StrafeMovement();

        if (isStrafing)
        {
            // strafe movement get the input 1 or -1
            animator.SetFloat("InputHorizontal", direction, dampTIme, Time.deltaTime);
        }
        animator.SetFloat("InputVertical",speed, dampTIme, Time.deltaTime);
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

    public void OnEnableAttack()
    {
        isAttack = true;
    }

    public void OnDisableAttack()
    {
        isAttack = false;
    }

    public void ResetAttackTriggers()
    {
        animator.ResetTrigger("Attack");
        //animator.ResetTrigger("StrongAttack");
    }
}