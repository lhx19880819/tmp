using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace Assets.Scripts.Player
{
    public partial class AInput : MonoBehaviour
    {
        private bool lockMovement;
        private bool lockInStrafe;

        private bool _isStrafing = false;
        private bool isStrafing
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

        void Start()
        {
            mAnimator = GetComponent<Animator>();
            m_Pivot = Camera.main.transform.parent;
            m_CamRig = Camera.main.transform.parent.parent;
        }

        private void Update()
        {
            UpdateMelee();

            if (isAttack) return;

            UpdateMotor();

            UpdateCamera();
        }

        void LateUpdate()
        {
            CameraInput();
        }
    }//class end
}