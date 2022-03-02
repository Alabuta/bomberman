using Configs.Behaviours;
using Configs.Level.Tile;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Configs/Entity/Enemy")]
    public sealed class EnemyConfig : EntityConfig
    {
        public BlockConfig[] FordableTiles;

        public MovementBehaviourBaseConfig BehaviourConfig;
    }
}
