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
        public event Action<fix2> BombPlantEvent;
        public event Action OnBombBlastEvent;

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

            SubscribeInputListeners();
        }

        public void AttachHero(Hero hero)
        {
            Hero = hero;
            Hero.KillEvent += OnHeroKill;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = _score;
        }

        private void SubscribeInputListeners()
        {
            _playerInput.OnMoveEvent += OnMove;
            _playerInput.OnBombPlantEvent += OnBombPlant;
            _playerInput.OnBombBlastEvent += OnBombBlast;
        }

        private void UnsubscribeInputListeners()
        {
            if (_playerInput == null)
                return;

            _playerInput.OnMoveEvent -= OnMove;
            _playerInput.OnBombPlantEvent -= OnBombPlant;
            _playerInput.OnBombBlastEvent -= OnBombBlast;
        }

        private void OnMove(float2 value)
        {
            if (Hero is not { IsAlive: true })
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
            if (Hero is not { IsAlive: true })
                BombPlantEvent?.Invoke(Hero.WorldPosition);
        }

        private void OnBombBlast()
        {
            if (Hero is not { IsAlive: true })
                OnBombBlastEvent?.Invoke();
        }

        private void OnHeroKill()
        {
            Hero.KillEvent -= OnHeroKill;

            UnsubscribeInputListeners();
        }
    }
}
