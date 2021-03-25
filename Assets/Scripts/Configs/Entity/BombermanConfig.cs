using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "BombermanConfig", menuName = "Configs/Entity/Bomberman Config")]
    public sealed class BombermanConfig : EntityConfig
    {
        public int BlastRadius = 1;
        public int BombCapacity = 1;

        public BombConfig BombConfig;
    }
}
