using UnityEngine;

namespace Configs.Effects
{
    [CreateAssetMenu(
        menuName = "Configs/Effects/Blast",
        fileName = "BlastEffect")
    ]
    public class BlastEffectConfig : ConfigBase
    {
        public GameObject Prefab;
    }
}
