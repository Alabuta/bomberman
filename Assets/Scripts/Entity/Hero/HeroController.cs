using System;
using Configs.Entity;
using Input;
using Unity.Mathematics;
using UnityEngine;

namespace Entity.Hero
{
    public sealed class HeroController : EntityController<HeroConfig>, IHero
    {
        private const float MovementDeadZoneThreshold = .001f;
        private const float MovementThreshold = .5f;

        [SerializeField]
        private new HeroAnimator EntityAnimator;

        private IPlayerInput _playerInput;

        public event Action<float2> BombPlantedEvent;

        public override int Health { get; set; }

        public override float CurrentSpeed { get; protected set; }

        public int BlastRadius { get; set; }

        public int BombCapacity { get; set; }

        public void AttachPlayerInput(IPlayerInput playerInput)
        {
            UnsubscribeInputListeners();

            _playerInput = playerInput;

            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        private new void Start()
        {
            base.Start();

            BlastRadius = EntityConfig.BlastRadius;
            BombCapacity = EntityConfig.BombCapacity;

            EntityAnimator.UpdateDirection(DirectionVector);
            EntityAnimator.StopMovement();
        }

        private void OnMove(float2 value)
        {
            if (math.lengthsq(value) > MovementDeadZoneThreshold)
            {
                DirectionVector = math.round(value);
                CurrentSpeed = InitialSpeed * SpeedMultiplier;
            }
            else
                CurrentSpeed = 0;

            EntityAnimator.UpdateDirection(DirectionVector);

            if (CurrentSpeed > MovementThreshold)
                EntityAnimator.Move();
            else
                EntityAnimator.StopMovement();

            EntityAnimator.UpdatePlaybackSpeed(CurrentSpeed / InitialHealth);
        }

        private void OnBombPlant()
        {
            if (BombCapacity <= 0)
                return;

            --BombCapacity;

            BombPlantedEvent?.Invoke(WorldPosition.xy);
        }

        private void UnsubscribeInputListeners()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            _playerInput.OnBombPlantEvent -= OnBombPlant;
        }

        private void OnDestroy()
        {
            UnsubscribeInputListeners();
        }
    }
}
