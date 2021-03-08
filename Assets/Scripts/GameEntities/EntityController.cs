using System;
using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace GameEntities
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityController<T> : MonoBehaviour, IEntity where T : EntityConfig
    {
        private readonly int _verticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private readonly int _horizontalSpeedId = Animator.StringToHash("HorizontalSpeed");

        protected Animator Animator;

        protected float3 SpeedVector = float3.zero;

        [SerializeField]
        protected T EntityConfig;

        protected void Start()
        {
            Animator = gameObject.GetComponent<Animator>();

            Speed = EntityConfig.Speed;
        }

        private void FixedUpdate()
        {
            transform.Translate(SpeedVector * Time.fixedDeltaTime);
        }

        protected void Update()
        {
            Animator.SetFloat(_horizontalSpeedId, SpeedVector.x);
            Animator.SetFloat(_verticalSpeedId, SpeedVector.y);
        }

        protected abstract void OnTriggerEnter2D(Collider2D otherCollider);

        public bool IsAlive => Health > 0;

        public IObservable<int> HealthPoints { get; set; }

        public abstract int Health { get; set; }
        public int MaxHealth => EntityConfig.Health;

        public abstract float Speed { get; set; }
        public float MaxSpeed => EntityConfig.Speed;
    }
}
