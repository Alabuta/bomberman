using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Hero
{
    public sealed class HeroController : EntityController
    {
        [SerializeField]
        private HeroAnimator HeroAnimator;

        private fix _speed;
        private int2 _direction = new(0, -1);

        public override fix Speed
        {
            protected get => _speed;
            set
            {
                _speed = value;

                if (_speed > fix.zero)
                    HeroAnimator.Move();
                else
                    HeroAnimator.StopMovement();

                // UpdatePlaybackSpeed(TODO);
            }
        }

        public override int2 Direction
        {
            protected get => _direction;
            set
            {
                _direction = value;

                HeroAnimator.UpdateDirection(_direction);
            }
        }

        protected override EntityAnimator EntityAnimator =>
            HeroAnimator;

        protected override void Start()
        {
            base.Start();

            HeroAnimator.UpdateDirection(_direction);
            HeroAnimator.StopMovement();
        }
    }
}
