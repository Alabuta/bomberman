using System;
using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityController<TConfig> : MonoBehaviour, IEntity where TConfig : EntityConfig
    {
        [SerializeField]
        [HideInInspector]
        private float3 MovementConvertMask = new(1, 1, 0);

        [SerializeField]
        protected TConfig EntityConfig;

        [SerializeField]
        protected EntityAnimator EntityAnimator;

        [SerializeField]
        protected Transform Transform;

        public float2 DirectionVector { get; protected set; } = new(0, -1);

        public event Action OnKillEvent;

        public bool IsAlive => Health > 0;

        public abstract int Health { get; set; }

        public int InitialHealth => EntityConfig.Health;

        public abstract float CurrentSpeed { get; protected set; }

        public float InitialSpeed => EntityConfig.Speed;

        public float SpeedMultiplier { get; set; }

        public float3 WorldPosition => Transform.position;

        public void Kill()
        {
            Health = 0;
            CurrentSpeed = 0;
            SpeedMultiplier = 1;

            EntityAnimator.UpdatePlaybackSpeed(1);
            EntityAnimator.SetDead();

            OnKillEvent?.Invoke();
        }

        protected void Start()
        {
            CurrentSpeed = 0;
            SpeedMultiplier = 1;

            Health = EntityConfig.Health;
            DirectionVector = EntityConfig.StartDirection;

            EntityAnimator.UpdatePlaybackSpeed(1);
            EntityAnimator.SetAlive();
            EntityAnimator.UpdateDirection(DirectionVector);
            EntityAnimator.UpdateSpeed(CurrentSpeed);
        }

        private void FixedUpdate()
        {
            Transform.Translate(DirectionVector.xyy * CurrentSpeed * MovementConvertMask * Time.fixedDeltaTime);
        }
    }
}
