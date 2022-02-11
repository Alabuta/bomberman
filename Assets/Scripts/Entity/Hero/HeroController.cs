using Input;
using Unity.Mathematics;
using UnityEngine;

namespace Entity.Hero
{
    public sealed class HeroController : EntityController
    {
        // private const float MovementDeadZoneThreshold = .001f;
        private const float MovementThreshold = .5f;

        [SerializeField]
        private HeroAnimator HeroAnimator;

        private IPlayerInput _playerInput;

        private float _speed;
        private float2 _direction;

        /*public event Action<float2> BombPlantedEvent;

        public int BlastRadius { get; set; }

        public int BombCapacity { get; set; }*/

        public void AttachPlayerInput(IPlayerInput playerInput)
        {
            UnsubscribeInputListeners();

            _playerInput = playerInput;

            _playerInput.OnMoveEvent += OnMove;
            // _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        public override float Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (Speed > MovementThreshold)
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

        private new void Start()
        {
            base.Start();

            /*BlastRadius = EntityConfig.BlastRadius;
            BombCapacity = EntityConfig.BombCapacity;*/

            HeroAnimator.UpdateDirection(Direction);
            HeroAnimator.StopMovement();
        }

        private void OnMove(float2 value)
        {
            /*if (math.lengthsq(value) > MovementDeadZoneThreshold)
            {
                Direction = math.round(value);
                Speed = InitialSpeed * SpeedMultiplier;
            }
            else
                Speed = 0;*/

            /*HeroAnimator.UpdateDirection(Direction);

            if (Speed > MovementThreshold)
                HeroAnimator.Move();
            else
                HeroAnimator.StopMovement();

            HeroAnimator.UpdatePlaybackSpeed(Speed / InitialHealth);*/
        }

        /*private void OnBombPlant()
        {
            if (BombCapacity <= 0)
                return;

            --BombCapacity;

            BombPlantedEvent?.Invoke(WorldPosition.xy);
        }*/

        private void UnsubscribeInputListeners()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            // _playerInput.OnBombPlantEvent -= OnBombPlant;
        }

        private void OnDestroy()
        {
            UnsubscribeInputListeners();
        }
    }
}
