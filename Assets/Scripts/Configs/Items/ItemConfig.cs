using Configs.Game;
using Configs.Game.Colliders;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfig : ConfigBase
    {
        public GameTagConfig GameTag;
        public GameObject Prefab;

        public ColliderComponentConfig Collider;
    }
}
