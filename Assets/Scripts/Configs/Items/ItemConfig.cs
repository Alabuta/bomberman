using Configs.Game.Colliders;
using Core.Attributes;
using UnityEngine.AddressableAssets;

namespace Configs.Items
{
    public abstract class ItemConfig : ConfigBase
    {
        [Layer]
        public int Layer;

        public AssetReferenceGameObject Prefab;

        public ColliderConfig Collider;

        public int LayerMask => 1 << Layer;
    }
}
