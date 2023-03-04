using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game
{
    public abstract class EntityController : MonoBehaviour
    {
        [SerializeField]
        protected Transform Transform;

        public abstract fix Speed { protected get; set; }
        public abstract int2 Direction { protected get; set; }

        public fix2 WorldPosition
        {
            set => Transform.position = fix2.ToXY(value);
        }

        public float PlaybackSpeed => EntityAnimator.PlaybackSpeed;

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

            // :TODO: push back to ObjectsPool
        }

        public void TakeDamage()
        {
            EntityAnimator.PlayHit();
        }

        protected virtual void Start()
        {
            Revive();
        }
    }
}
