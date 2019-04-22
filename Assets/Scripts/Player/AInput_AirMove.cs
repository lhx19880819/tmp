using UnityEngine;

namespace Assets.Scripts.Player
{
    public partial class AInput
    {
        private float m_fMaxBailOutSpeed = 12.0f, m_fMinBailOutSpeed = 10.0f, m_fMaxGlideSpeed = 5.0f, m_fMinGlideSpeed = 3.0f;

        private float m_fForceGravity = 10.0f, m_fResistance = 5.0f, m_fAcceleration = 5.0f;
        private float m_fForceGravity_Glide = 5.0f, m_fResistance_Glide = 3.0f, m_fAcceleration_Glide = 2.0f;

        private bool m_bBailOutControl = false;
        private bool m_bGlide = false;
        private bool m_bAirMoving = false;
        private float m_fRateAirMove = .0f;
        private float m_fGlidSmooth = 1.0f;
        private float m_fMaxFall = .0f, m_fMaxFallGlide = .0f;
        private float m_fAirMoving = .0f;

        // 飞行镜头动画的时候人不能降落
        private float m_fAirMoveAnimateTime = .0f;

        protected virtual void AirMoveVelocity(float velocity)
        {
            if (lockMovement || !IsBailOutControl())
            {
                return;
            }

            float fRateAirMove = m_fRateAirMove;
            float fMaxFall = .0f;
            float fAcceleration = .0f;
            Vector3 v = Vector3.zero;
            float fSpeedY = 0;
            float smooth = 20.0f;
            if (IsGlide())
            {
                smooth = m_fGlidSmooth;
                fMaxFall = (m_fMaxGlideSpeed - m_fMaxFallGlide) * m_fRateAirMove - m_fMaxGlideSpeed;
                m_fAirMoving = inputAir.y;
                fRateAirMove = fRateAirMove > 0.7f ? 0.7f : fRateAirMove;
                fAcceleration = m_fAcceleration_Glide;
                //v = (transform.TransformDirection(new Vector3(inputAir.x, 0, m_fAirMoving * (1.0f - fRateAirMove))) * (velocity > 0 ? velocity : 1f));
                v = (transform.TransformDirection(new Vector3(inputAir.x, 0, inputAir.y).normalized) * (velocity > 0 ? velocity : 1f));
                fSpeedY = Rigidbody.velocity.y;
                if (inputAir.y > 0 && fSpeedY > fMaxFall)
                {
                    fSpeedY = Rigidbody.velocity.y - inputAir.y * fAcceleration;
                    fSpeedY = fSpeedY < fMaxFall ? fMaxFall : fSpeedY;
                }
            }
            else
            {
                fMaxFall = (m_fMaxBailOutSpeed - m_fMaxFall) * m_fRateAirMove - m_fMaxBailOutSpeed;
                m_fAirMoving = inputAir.y;
                fAcceleration = m_fAcceleration;
                v = (transform.TransformDirection(new Vector3(inputAir.x, 0, m_fAirMoving * (1.0f - fRateAirMove))) * (velocity > 0 ? velocity : 1f));
                fSpeedY = Rigidbody.velocity.y;
                if (inputAir.y > 0 && fSpeedY > fMaxFall)
                {
                    fSpeedY = Rigidbody.velocity.y - inputAir.y * fAcceleration;
                    fSpeedY = fSpeedY < fMaxFall ? fMaxFall : fSpeedY;
                }
            }
            // test
            //fSpeedY = 0;
            if (m_fAirMoveAnimateTime > .0f)
            {
                m_fAirMoveAnimateTime -= Time.deltaTime;
                fSpeedY = 0;
            }
            //
            v.y = fSpeedY;
            Rigidbody.velocity = Vector3.Lerp(Rigidbody.velocity, v, smooth * Time.deltaTime);
            //Debug.Log("speed:" + fSpeedY.ToString());
        }

        public bool IsBailOutControl()
        {
            return m_bBailOutControl;
        }

        public bool IsGlide()
        {
            return m_bGlide;
        }


        public bool IsAirMoving()
        {
            return m_bAirMoving;
        }
    }
}//class end
