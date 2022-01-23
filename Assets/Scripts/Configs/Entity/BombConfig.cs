using UnityEngine;

namespace Configs.Entity
{
    [CreateAssetMenu(fileName = "Bomb", menuName = "Configs/Entity/Bomb")]
    public sealed class BombConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;

        public GameObject Prefab;

        public int LifetimeSec = 3;
    }
}
