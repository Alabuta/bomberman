using Configs.Behaviours;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Configs/Entity/Enemy")]
    public sealed class EnemyConfig : EntityConfig
    {
        [Space]
        public DamageParameters DamageParameters;
        [Space]
        public MovementParameters MovementParameters;
        [Space]
        public HealthParameters HealthParameters;

        [Header("Enemy Parameters")]
        public BehaviourConfig[] BehaviourConfigs;
    }
}
