using UnityEngine;

namespace Configs.Entity
{
    public abstract class EntityConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;

        public GameObject Prefab;

        [Header("Movement Parameters"), Range(0f, 10f)]
        public float Speed;

        [Header("Health Parameters"), Range(0, 5)]
        public int Health;
    }
}
