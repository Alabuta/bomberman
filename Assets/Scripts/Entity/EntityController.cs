using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EntityController : MonoBehaviour, IEntity
    {
        protected static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        protected static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");

        protected static readonly float2 HorizontalMovementMask = new float2(1, 0);
        protected static readonly float2 VerticalMovementMask = new float2(0, 1);

        protected Animator Animator;

        protected float3 SpeedVector = float3.zero;

        [SerializeField]
        protected EntityConfig EntityConfig;

        protected void Start()
        {
            Animator = gameObject.GetComponent<Animator>();

            Speed = EntityConfig.Speed;
        }

        private void FixedUpdate()
        {
            transform.Translate(SpeedVector * Time.fixedDeltaTime);
        }

        protected abstract void Update();

        protected abstract void OnTriggerEnter2D(Collider2D otherCollider);

        public bool IsAlive => Health > 0;

        public abstract int Health { get; set; }
        public int MaxHealth => EntityConfig.Health;

        public abstract float Speed { get; set; }
        public float MaxSpeed => EntityConfig.Speed;
    }
}
