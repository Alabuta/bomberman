using Configs.Game.Colliders;
using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Hero", menuName = "Configs/Entity/Hero")]
    public sealed class HeroConfig : EntityConfig
    {
        public Sprite Icon;

        public BombConfig BombConfig;

        public ColliderComponentConfig Collider;
    }
}
