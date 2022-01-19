using System;
using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityController<T> : MonoBehaviour, IEntity where T : EntityConfig
    {
        [SerializeField]
        [HideInInspector]
        protected float2 HorizontalMovementMask = new float2(1, 0);

        [SerializeField]
        [HideInInspector]
        protected float2 VerticalMovementMask = new float2(0, 1);

        [SerializeField]
        [HideInInspector]
        private float3 MovementConvertMask = new float3(1, 1, 0);

        [SerializeField]
        protected T EntityConfig;

        [SerializeField]
        protected Transform Transform;

        public float2 MovementVector { get; set; }

        protected void Start()
        {
            Speed = EntityConfig.Speed;
        }

        private void FixedUpdate()
        {
            Transform.Translate(MovementVector.xyy * MovementConvertMask * Time.fixedDeltaTime);
        }

        public event Action OnKillEvent;

        public bool IsAlive => Health > 0;

        public abstract int Health { get; set; }
        public int MaxHealth => EntityConfig.Health;

        public abstract float Speed { get; set; }
        public float InitialSpeed => EntityConfig.Speed;

        public float3 WorldPosition => Transform.position;

        public void Kill()
        {
            Health = 0;
            Speed = InitialSpeed;
            MovementVector = float2.zero;

            OnKillEvent?.Invoke();
        }
    }
}
