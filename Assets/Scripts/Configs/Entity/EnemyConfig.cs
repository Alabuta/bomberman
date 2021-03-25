using Configs.Level.Tile;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Entity/Enemy Config")]
    public sealed class EnemyConfig : EntityConfig
    {
        public BlockConfig[] FordableTiles;
    }
}
