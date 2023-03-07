using Unity.Mathematics;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Bomb", menuName = "Configs/Entity/Bomb")]
    public class BombConfig : EntityConfig
    {
        [Space]
        public DamageParameters DamageParameters;
        [Space]
        public HealthParameters HealthParameters;
    }
}
