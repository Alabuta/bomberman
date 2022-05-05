using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Hero", menuName = "Configs/Entity/Hero")]
    public sealed class HeroConfig : EntityConfig
    {
        public AssetReferenceSprite Icon;

        public BombConfig BombConfig;
    }
}
