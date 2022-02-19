using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public abstract class EntityController : MonoBehaviour, IEntityController
    {
        [SerializeField]
        [HideInInspector]
        private float3 MovementConvertMask = new(1, 1, 0);

        [SerializeField]
        protected Transform Transform;

        public abstract float Speed { get; set; }
        public float PlaybackSpeed => EntityAnimator.PlaybackSpeed;

        public abstract float2 Direction { get; set; }

        public fix2 WorldPosition => (fix2) Transform.position;

        protected abstract EntityAnimator EntityAnimator { get; }

        private void Revive()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetAlive();
        }

        public void Kill()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetDead();
        }

        protected virtual void Start()
        {
            Revive();
        }

        private void FixedUpdate()
        {
            Transform.Translate(Direction.xyy * Speed * MovementConvertMask * Time.fixedDeltaTime);
        }
    }
}
