using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Entity.Hero
{
    public sealed class HeroController : EntityController
    {
        [SerializeField]
        private HeroAnimator HeroAnimator;

        private fix _speed;
        private int2 _direction;

        public override fix Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (Speed > fix.zero)
                    HeroAnimator.Move();
                else
                    HeroAnimator.StopMovement();

                // UpdatePlaybackSpeed(TODO);
            }
        }

        public override int2 Direction
        {
            get => _direction;
            set
            {
                _direction = value;

                HeroAnimator.UpdateDirection(Direction);
            }
        }

        protected override EntityAnimator EntityAnimator =>
            HeroAnimator;

        protected override void Start()
        {
            base.Start();

            HeroAnimator.UpdateDirection(Direction);
            HeroAnimator.StopMovement();
        }
    }
}
