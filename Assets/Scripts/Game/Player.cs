using System;
using Configs;
using Data;
using Entity.Hero;
using Infrastructure.Services.PersistentProgress;
using Input;
using Unity.Mathematics;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public event Action OnKillEvent;
        public event Action<float2> BombPlantedEvent;

        public PlayerConfig PlayerConfig { get; }

        public HeroController HeroController { get; private set; }

        public bool IsAlive => Health > 0;

        public int Health { get; set; }
        public int InitialHealth => PlayerConfig.HeroConfig.Health;

        public float Speed
        {
            get => _speed;
            private set
            {
                _speed = value;

                if (HeroController != null)
                    HeroController.Speed = value;
            }
        }

        public float InitialSpeed => PlayerConfig.HeroConfig.Speed;
        public float SpeedMultiplier { get; set; }

        public float2 Direction
        {
            get => _direction;
            set
            {
                _direction = value;

                if (HeroController != null)
                    HeroController.Direction = value;
            }
        }

        public float3 WorldPosition => HeroController.WorldPosition;

        private IPlayerInput _playerInput;

        private Score _score;
        private float _speed;
        private float2 _direction;

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;

            Speed = 0;
            SpeedMultiplier = 1;

            Health = PlayerConfig.HeroConfig.Health;
            Direction = PlayerConfig.HeroConfig.StartDirection;
        }

        public void AttachPlayerInput(IPlayerInput playerInput)
        {
            UnsubscribeInputListeners();

            _playerInput = playerInput;

            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        public void AttachHero(HeroController heroController)
        {
            HeroController = heroController;
        }

        public void Kill()
        {
            Health = 0;
            Speed = 0;
            SpeedMultiplier = 1;

            HeroController.Kill();

            OnKillEvent?.Invoke();
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = _score;
        }

        private void UnsubscribeInputListeners()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            _playerInput.OnBombPlantEvent -= OnBombPlant;
        }

        private void OnMove(float2 value)
        {
            if (math.lengthsq(value) > 0)
            {
                Direction = math.round(value);
                Speed = InitialSpeed * SpeedMultiplier;
            }
            else
                Speed = 0;
        }

        private void OnBombPlant()
        {
            /*if (BombCapacity <= 0)
                return;

            --BombCapacity;*/

            BombPlantedEvent?.Invoke(WorldPosition.xy);
        }
    }
}
