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
        public event EventHandler<BombPlantEventData> BombPlantedEvent;

        [SerializeField]
        private PlayerConfig PlayerConfig;

        private IPlayerInput _playerInput;

        private int _bombCapacity;

        private float2 _directionVector = float2.zero;

        public override int Health { get; set; }

        public override float Speed { get; set; }

        public int BlastRadius { get; set; }

        public int BombCapacity
        {
            get => _bombCapacity;
            set
            {
                _bombCapacity = value;

                // BombCapacityChangedEvent?.Invoke(_bombCapacity);
            }
        }

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
            Health = EntityConfig.Health;
        }

        private void Update()
        {
            MovementVector = math.round(_directionVector) * Speed;
        }

        private void OnMove(float2 value)
        {
            _directionVector = value;
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

    public class BombPlantEventData : EventArgs
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
