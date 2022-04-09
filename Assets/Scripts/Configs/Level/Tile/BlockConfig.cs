using Configs.Game.Colliders;
using UnityEngine;

namespace Configs.Level.Tile
{
    public abstract class BlockConfig : ConfigBase
    {
        public GameObject Prefab;

        public ColliderComponentConfig ColliderComponent;
    }
}
