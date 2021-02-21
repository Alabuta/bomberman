using UnityEngine;

namespace Configs.Entity
{
    public abstract class EntityConfig : ScriptableObject
    {
        [Header("General Parameters")]
        public string Name;

        [Header("Movement Parameters"), Range(0f, 10f)]
        public float Speed;

        [Header("Health Parameters"), Range(0, 5)]
        public int Health;
    }
}
