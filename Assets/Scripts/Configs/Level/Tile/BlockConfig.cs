using Configs.Game.Colliders;
using Core.Attributes;
using UnityEngine.AddressableAssets;

namespace Configs.Level.Tile
{
    public abstract class BlockConfig : ConfigBase
    {
        [Layer]
        public int Layer;

        public AssetReferenceGameObject Prefab;

        public ComponentConfig[] Components;

        public int LayerMask => 1 << Layer;
    }
}
