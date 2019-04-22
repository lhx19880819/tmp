using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        [Header("Camera")]
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
        Transform m_Pivot, m_CamRig;

        private void InitCamera()
        {
            m_Pivot = Camera.main.transform.parent;
            m_CamRig = Camera.main.transform.parent.parent;
        }

        private void UpdateCamera()
        {
            float mouseX = CrossPlatformInputManager.GetAxis("Mouse X");
            float mouseY = CrossPlatformInputManager.GetAxis("Mouse Y");
            RotateCamera(mouseX, mouseY);
        }

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

        public void CameraInput()
        {
            if (isAttack)
            {
                return;
            }
            if (!Camera.main) Debug.Log("Missing a Camera with the tag MainCamera, please add one.");
            if (!keepDirection) UpdateTargetDirection(Camera.main.transform);
            RotateWithCamera(Camera.main.transform);
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
    }//class end
}
