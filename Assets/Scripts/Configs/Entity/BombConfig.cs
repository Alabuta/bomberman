using UnityEngine;

namespace Configs.Entity
{
    public abstract class BombConfig : ConfigBase
    {
        [Header("General Parameters")]
        public GameObject Prefab;
    }
}
