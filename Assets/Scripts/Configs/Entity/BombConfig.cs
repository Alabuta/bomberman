using Configs.Items;
using UnityEngine;

namespace Configs.Entity
{
    public abstract class BombConfig : ConfigBase
    {
        public GameObject Prefab;

        public BombItemConfig ItemConfig;
    }
}
