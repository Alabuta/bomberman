using System.Collections;
using UnityEngine;

namespace Core
{
    public static class StartCoroutine
    {
        public static void Start(IEnumerator routine)
        {
            if (_holder == null)
                _holder = new GameObject("StaticCoroutineHolder").AddComponent<CoroutineHolder>();

            _holder.StartCoroutine(routine);
        }

        private class CoroutineHolder : MonoBehaviour
        {
        }

        private static CoroutineHolder _holder;
    }
}
