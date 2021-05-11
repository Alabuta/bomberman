using Audio;
using UnityEngine;

namespace Configs.Effects
{
    [CreateAssetMenu(menuName = "Configs/Effects/Destroy Behind Effect Config")]
    public class DestroyEffectConfig : ScriptableObject
    {
        public GameObject Effect;

        public AudioEvent AudioEvent;

        public float DestroyAfterTimeSec = 1f;
    }
}
