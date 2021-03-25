using System;
using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityController<T> : MonoBehaviour, IEntity where T : EntityConfig
    {
        protected static readonly float2 HorizontalMovementMask = new float2(1, 0);
        protected static readonly float2 VerticalMovementMask = new float2(0, 1);

        private readonly int _verticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private readonly int _horizontalSpeedId = Animator.StringToHash("HorizontalSpeed");

        [SerializeField]
        protected T EntityConfig;

        protected Transform Transform;
        protected Animator Animator;

        protected float3 SpeedVector = float3.zero;

        protected void Start()
        {
            Transform = gameObject.GetComponent<Transform>();
            Animator = gameObject.GetComponent<Animator>();

            Speed = EntityConfig.Speed;
        }

        private void FixedUpdate()
        {
            Transform.Translate(SpeedVector * Time.fixedDeltaTime);
        }

        protected void Update()
        {
            Animator.SetFloat(_horizontalSpeedId, SpeedVector.x);
            Animator.SetFloat(_verticalSpeedId, SpeedVector.y);
        }

        public bool IsAlive => Health > 0;

        public abstract int Health { get; set; }
        public int MaxHealth => EntityConfig.Health;

        public abstract float Speed { get; set; }
        public float MaxSpeed => EntityConfig.Speed;

        public float3 WorldPosition => Transform.position;
    }
}
