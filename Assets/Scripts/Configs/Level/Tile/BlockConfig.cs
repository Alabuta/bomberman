using Configs.Game;
using Configs.Game.Colliders;
using Core.Attributes;
using UnityEngine;

namespace Configs.Level.Tile
{
    public abstract class BlockConfig : ConfigBase
    {
        [Layer]
        public int Layer;

        public GameTagConfig GameTag;
        public GameObject Prefab;

        public ColliderComponentConfig Collider;
    }
}
