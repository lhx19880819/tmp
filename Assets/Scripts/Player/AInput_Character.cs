using UnityEngine;
using UnityEngine.Events;

namespace Assets.Scripts.Player
{
    [System.Serializable]
    public class OnDead : UnityEvent<GameObject> { }
    [System.Serializable]
    public class OnActiveRagdoll : UnityEvent { }
    [System.Serializable]
    public class OnActionHandle : UnityEvent<Collider> { }
    public partial class AInput
    {
        [HideInInspector]
        public OnActionHandle onActionEnter = new OnActionHandle();
        [HideInInspector]
        public OnActionHandle onActionStay = new OnActionHandle();
        [HideInInspector]
        public OnActionHandle onActionExit = new OnActionHandle();

        void InitCharacter()
        {
            var actionListeners = GetComponents<vActionListener>();
            for (int i = 0; i < actionListeners.Length; i++)
            {
                if (actionListeners[i].actionEnter)
                    onActionEnter.AddListener(actionListeners[i].OnActionEnter);
                if (actionListeners[i].actionStay)
                    onActionStay.AddListener(actionListeners[i].OnActionStay);
                if (actionListeners[i].actionExit)
                    onActionExit.AddListener(actionListeners[i].OnActionExit);
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            onActionEnter.Invoke(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            onActionStay.Invoke(other);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            onActionExit.Invoke(other);
        }
    }//class end
}
