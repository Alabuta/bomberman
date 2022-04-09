using Configs.Game.Colliders;
using UnityEngine;

namespace Configs.Items
{
    public abstract class ItemConfig : ConfigBase
    {
        public GameObject Prefab;

        public ColliderComponentConfig ColliderComponentConfig;
    }
}
