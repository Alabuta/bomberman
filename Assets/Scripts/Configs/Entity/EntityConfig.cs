using Configs.Game;
using Unity.Mathematics;
using UnityEngine;

namespace Configs.Entity
{
    public abstract class EntityConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;
        public GameTagConfig GameTag;

        public GameTagConfig[] ExcludeInteractionTags;

        public GameObject Prefab;

        [Header("Movement Parameters"), Range(.01f, 10f)]
        public float Speed;
        public int2 StartDirection = int2.zero;

        [Header("Health Parameters"), Range(1, 5)]
        public int Health;

        public double HitRadius = .1;
        public double HurtRadius = .1;
        public double ColliderRadius = .45;
    }
}
