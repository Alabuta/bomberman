using Level;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Entity
{
    public abstract class EntityConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;

        public GameObject Prefab;

        public LevelTileType[] FordableTileTypes;

        [Header("Movement Parameters"), Range(0f, 10f)]
        public float Speed;

        public int2 StartDirection = int2.zero;

        [Header("Health Parameters"), Range(0, 5)]
        public int Health;
    }
}
