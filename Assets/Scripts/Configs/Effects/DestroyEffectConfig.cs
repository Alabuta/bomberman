using Audio;
using UnityEngine;

namespace Configs.Effects
{
    [CreateAssetMenu(menuName = "Configs/Effects/Destroy Behind Effect", fileName = "DestroyEffect", order = 0)]
    public class DestroyEffectConfig : ConfigBase
    {
        public GameObject Effect;

        public AudioEvent AudioEvent;

        public float DestroyAfterTimeSec = 1f;
    }
}
