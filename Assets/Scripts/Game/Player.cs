using Configs;
using Data;
using Infrastructure.Services.PersistentProgress;
using Input;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig PlayerConfig { get; }
        public Hero.Hero Hero { get; private set; }

        private Score _score;

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;
        }

        public void ApplyInputAction(PlayerInputAction inputAction)
        {
            if (Hero is not { IsAlive: true })
                return;

            OnMove(inputAction.MovementVector);
        }

        public void AttachHero(Hero.Hero hero)
        {
            Hero = hero;
            Hero.DeathEvent += OnHeroDeath;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = _score;
        }

        private void OnMove(float2 value)
        {
            if (math.lengthsq(value) > 0)
            {
                Hero.Direction = (int2) math.round(value);
                Hero.Speed = Hero.InitialSpeed * Hero.SpeedMultiplier;
            }
            else
                Hero.Speed = fix.zero;
        }

        private void OnHeroDeath(IEntity entity)
        {
            Hero.DeathEvent -= OnHeroDeath;
        }
    }
}
