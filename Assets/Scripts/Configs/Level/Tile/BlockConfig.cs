using Configs.Game;
using Configs.Game.Colliders;
using UnityEngine;

namespace Configs.Level.Tile
{
    public abstract class BlockConfig : ConfigBase
    {
        public GameTagConfig GameTag;
        public GameObject Prefab;

        public ColliderComponentConfig Collider;
    }
}
