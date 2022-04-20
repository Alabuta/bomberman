using Configs.Game;
using Configs.Game.Colliders;
using Core.Attributes;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfig : ConfigBase
    {
        [Layer]
        public int Layer;

        public GameTagConfig GameTag;
        public GameObject Prefab;

        public ColliderComponentConfig Collider;
    }
}
