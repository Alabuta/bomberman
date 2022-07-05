using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
    public abstract class EntityController : MonoBehaviour, IEntityController
    {
        [SerializeField]
        protected Transform Transform;

        public abstract fix Speed { protected get; set; }
        public float PlaybackSpeed => EntityAnimator.PlaybackSpeed;

        public abstract int2 Direction { protected get; set; }

        public fix2 WorldPosition
        {
            // protected get => (fix2) Transform.position;
            set => Transform.position = fix2.ToXY(value);
        }

        protected abstract EntityAnimator EntityAnimator { get; }

        private void Revive()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetAlive();
        }

        public void Die()
        {
            EntityAnimator.PlaybackSpeed = 1;
            EntityAnimator.SetDead();

            // :TODO: call Destroy() after a while
        }

        public void TakeDamage(int damage)
        {
            EntityAnimator.PlayHit();
        }

        protected virtual void Start()
        {
            Revive();
        }
    }
}
