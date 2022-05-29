using Audio;
using UnityEngine;

namespace Configs.Effects
{
    [CreateAssetMenu(menuName = "Configs/Effects/Destroy Behind Effect", fileName = "DestroyEffect")]
    public class DestroyEffectConfig : ConfigBase
    {
        public GameObject Effect;

        public AudioEvent AudioEvent;

        public float DestroyAfterTimeSec = 1f;
    }
}
