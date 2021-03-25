using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "BombermanConfig", menuName = "Configs/Entity/Bomberman Config")]
    public class BombermanConfig : EntityConfig
    {
        public int BombCapacity = 1;
        public int BlastRadius = 2;
    }
}
