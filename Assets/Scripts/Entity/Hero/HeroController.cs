using System;
using Configs;
using Configs.Entity;
using Infrastructure.Services;
using Infrastructure.Services.Input;
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
        private PlayerConfig PlayerConfig;

        private IPlayerInput _playerInput;

        public event Action<BombPlantEventData> BombPlantedEvent;

        public override int Health { get; set; }

        public override float CurrentSpeed { get; protected set; }

        public int BlastRadius { get; set; }

        public int BombCapacity { get; set; }

        private void Construct(IInputService inputService)
        {
            _playerInput = inputService.GetPlayerInput(PlayerConfig.PlayerTagConfig);

            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        private void Awake()
        {
            Construct(ServiceLocator.Container.Single<IInputService>());// :TODO: replace by injection in Construct
        }

        private new void Start()
        {
            base.Start();

            BlastRadius = EntityConfig.BlastRadius;
            BombCapacity = EntityConfig.BombCapacity;
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

            BombPlantedEvent?.Invoke(new BombPlantEventData(BlastRadius, WorldPosition.xy));
        }

        private void OnDestroy()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            _playerInput.OnBombPlantEvent -= OnBombPlant;
        }
    }

    public struct BombPlantEventData
    {
        public readonly int BlastRadius;
        public readonly float2 WorldPosition;

        public BombPlantEventData(int blastRadius, float2 worldPosition)
        {
            BlastRadius = blastRadius;
            WorldPosition = worldPosition;
        }
    }
}
