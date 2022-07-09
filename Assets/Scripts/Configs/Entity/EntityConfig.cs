using System;
using Configs.Game.Colliders;
using Core.Attributes;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Configs.Entity
{
    [Serializable]
    public class DamageParameters
    {
        public double HitRadius = .1;
        public double HurtRadius = .1;
    }

    [Serializable]
    public class MovementParameters
    {
        [Range(.01f, 10f)]
        public float Speed;
    }

    [Serializable]
    public class HealthParameters
    {
        [Range(1, 5)]
        public int Health;
    }

    public abstract class EntityConfig : ConfigBase
    {
        [Header("General Parameters")]
        public string Name;

        [Layer]
        public int Layer;

        public AssetReferenceGameObject Prefab;

        [Header("Movement Parameters")]
        public int2 StartDirection = int2.zero;

        public ColliderConfig Collider;

        public int LayerMask => 1 << Layer;
    }
}
