using Configs.Game.Colliders;
using Core.Attributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Configs.Entity
{
    public abstract class EntityConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;

        [Layer]
        public int Layer;

        public AssetReferenceGameObject Prefab;

        [Header("Movement Parameters"), Range(.01f, 10f)]
        public float Speed;
        public int2 StartDirection = int2.zero;

        [Header("Health Parameters"), Range(1, 5)]
        public int Health;

        public double HitRadius = .1;
        public double HurtRadius = .1;
        public double ColliderRadius = .47;

        public ColliderComponentConfig Collider;

        public int LayerMask => 1 << Layer;
    }
}
