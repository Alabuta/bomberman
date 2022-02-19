using System;
using Configs;
using Data;
using Entity.Hero;
using Infrastructure.Services.PersistentProgress;
using Input;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public event Action OnKillEvent;
        public event Action<fix2> BombPlantedEvent;

        public PlayerConfig PlayerConfig { get; }
        public Hero Hero { get; private set; }

        private IPlayerInput _playerInput;

        private Score _score;

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;
        }

        public void AttachPlayerInput(IPlayerInput playerInput)
        {
            UnsubscribeInputListeners();

            _playerInput = playerInput;

            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
        }

        public void AttachHero(Hero hero)
        {
            Hero = hero;
        }

        public void Kill()
        {
            Hero?.Kill();

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
            if (Hero == null)
                return;

            if (math.lengthsq(value) > 0)
            {
                Hero.Direction = (int2) math.round(value);
                Hero.Speed = Hero.InitialSpeed * Hero.SpeedMultiplier;
            }
            else
                Hero.Speed = 0;
        }

        private void OnBombPlant()
        {
            /*if (BombCapacity <= 0)
                return;

            --BombCapacity;*/

            if (Hero != null)
                BombPlantedEvent?.Invoke(Hero.WorldPosition);
        }
    }
}
