using System.Collections;
using UnityEngine;

namespace Effects
{
    public abstract class DestructionSequence : ScriptableObject
    {
        protected abstract IEnumerator SequenceCoroutine(MonoBehaviour runner);
    }
}
