using System;
using Configs.Entity;
using Configs.Game;
using Infrastructure.Services;
using Input;
using Services.Input;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace Entity
{
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

    public sealed class HeroController : EntityController<HeroConfig>, IHero
    {
        [SerializeField]
        private PlayerTagConfig PlayerTagConfig;

        [SerializeField]
        private GameObject Forwarder;

        [SerializeField]
        private int PlayerIndex;

        private IPlayerInputForwarder _playerInput;

        private int _bombCapacity;

        private float2 _directionVector = float2.zero;

        public event EventHandler<BombPlantEventData> BombPlantedEvent;

        public event Action<int> BombCapacityChangedEvent;

        [Inject]
        private void Construct(IInputService inputService)
        {
            // var playerInput = inputService.GetPlayerInputService(PlayerTag);
            _playerInput = inputService.RegisterPlayerInput(PlayerTagConfig, PlayerIndex, Forwarder.gameObject);

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

        private void OnDestroy()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            _playerInput.OnBombPlantEvent -= OnBombPlant;
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

        public override int Health { get; set; }

        public override float Speed { get; set; }

        public int BlastRadius { get; set; }

        public int BombCapacity
        {
            get => _bombCapacity;
            set
            {
                _bombCapacity = value;

                BombCapacityChangedEvent?.Invoke(_bombCapacity);
            }
        }
    }
}
