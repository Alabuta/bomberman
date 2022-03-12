using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Hero", menuName = "Configs/Entity/Hero")]
    public sealed class HeroConfig : EntityConfig
    {
        public int BlastRadius = 1;
        public int BombCapacity = 1;

        public Sprite Icon;

        public BombConfig BombConfig;
    }
}
