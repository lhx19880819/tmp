using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Other
{
    public class ADestroyGameObject : MonoBehaviour
    {
        public float delay;

        IEnumerator Start()
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}
