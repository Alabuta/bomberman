using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Hero", menuName = "Configs/Entity/Hero")]
    public sealed class HeroConfig : EntityConfig
    {
        [Space]
        public DamageParameters DamageParameters;
        [Space]
        public MovementParameters MovementParameters;
        [Space]
        public HealthParameters HealthParameters;

        [Header("Hero Parameters")]
        public AssetReferenceSprite Icon;

        public BombConfig BombConfig;

        public int BombBlastDamage = 1;
        public int BombBlastRadius = 1;
        public float BombBlastDelay = 2;
    }
}
