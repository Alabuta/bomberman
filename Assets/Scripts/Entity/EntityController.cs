using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public abstract class EntityController : MonoBehaviour
    {
        protected static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        protected static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");

        protected static readonly float2 HorizontalMovementMask = new float2(1, 0);
        protected static readonly float2 VerticalMovementMask = new float2(0, 1);

        protected Animator Animator;

        protected float3 MovementVector = float3.zero;

        public EntityConfig EntityConfig;

        protected void Start()
        {
            Animator = gameObject.GetComponent<Animator>();
        }

        private void FixedUpdate()
        {
            transform.Translate(MovementVector * Time.fixedDeltaTime);
        }

        protected abstract void Update();

        protected abstract void OnTriggerEnter(Collider otherCollider);
    }
}
