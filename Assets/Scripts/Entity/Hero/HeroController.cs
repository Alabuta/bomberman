using System;
using Configs;
using Configs.Entity;
using Infrastructure.Services;
using Infrastructure.Services.Input;
using Input;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace Entity.Hero
{
    public sealed class HeroController : EntityController<HeroConfig>, IHero
    {
        private const float Threshold = .001f;
        public event EventHandler<BombPlantEventData> BombPlantedEvent;

        [SerializeField]
        private PlayerConfig PlayerConfig;

        private IPlayerInput _playerInput;

        public override int Health { get; set; }

        public override float CurrentSpeed { get; protected set; }

        public int BlastRadius { get; set; }

        public int BombCapacity { get; set; }

        [Inject]
        private void Construct(IInputService inputService)
        {
            _playerInput = inputService.GetPlayerInput(PlayerConfig.PlayerTagConfig);

            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        private void Awake()
        {
            Construct(ServiceLocator.Container.Single<IInputService>());// :TODO: replace by injection in ctor
        }

        private new void Start()
        {
            base.Start();

            BlastRadius = EntityConfig.BlastRadius;
            BombCapacity = EntityConfig.BombCapacity;
        }

        private void OnMove(float2 value)
        {
            if (math.lengthsq(value) > Threshold)
            {
                DirectionVector = math.round(value);
                CurrentSpeed = InitialSpeed * SpeedMultiplier;
            }
            else
                CurrentSpeed = 0;

            EntityAnimator.UpdateDirection(DirectionVector);
            EntityAnimator.UpdateSpeed(CurrentSpeed);
            EntityAnimator.UpdatePlaybackSpeed(CurrentSpeed / InitialHealth);
        }

        private void OnBombPlant()
        {
            if (BombCapacity <= 0)
                return;

            --BombCapacity;

            BombPlantedEvent?.Invoke(this, new BombPlantEventData(BlastRadius, WorldPosition.xy));
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
