using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public abstract class EntityController : MonoBehaviour, IEntityController
    {
        [SerializeField]
        protected Transform Transform;

        public abstract fix Speed { get; set; }
        public float PlaybackSpeed => EntityAnimator.PlaybackSpeed;

        public abstract int2 Direction { get; set; }

        public fix2 WorldPosition
        {
            get => (fix2) Transform.position;
            set => Transform.position = fix2.ToXY(value);
        }

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
    }
}
