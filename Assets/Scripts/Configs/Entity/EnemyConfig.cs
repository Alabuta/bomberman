using Configs.Behaviours;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Configs/Entity/Enemy")]
    public sealed class EnemyConfig : EntityConfig
    {
        public BehaviourConfig[] BehaviourConfigs;
    }
}
