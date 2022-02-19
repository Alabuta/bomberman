using Unity.Mathematics;
using UnityEngine;

namespace Entity.Hero
{
    public sealed class HeroController : EntityController
    {
        [SerializeField]
        private HeroAnimator HeroAnimator;

        private float _speed;
        private float2 _direction;

        public override float Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (Speed > 0)
                    HeroAnimator.Move();
                else
                    HeroAnimator.StopMovement();

                // UpdatePlaybackSpeed(TODO);
            }
        }

        public override float2 Direction
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
