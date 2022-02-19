using Unity.Mathematics;
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

        public int2 StartDirection = new(0, -1);

        [Header("Health Parameters"), Range(0, 5)]
        public int Health;
    }
}
