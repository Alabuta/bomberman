using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Hero", menuName = "Configs/Entity/Hero")]
    public sealed class HeroConfig : EntityConfig
    {
        public AssetReferenceSprite Icon;

        public BombConfig BombConfig;

        public int BombBlastDamage = 1;
        public int BombBlastRadius = 1;

        public int2[] BombBlastDirections = {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1)
        };
    }
}
