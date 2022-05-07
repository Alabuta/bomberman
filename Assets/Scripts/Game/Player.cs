using Configs;
using Data;
using Infrastructure.Services.PersistentProgress;
using Input;
using Level;
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

        public void ApplyInputAction(World world, PlayerInputAction inputAction)
        {
            if (Hero is not { IsAlive: true })
                return;

            OnMove(inputAction.MovementVector);

            if (inputAction.BombPlant)
                world.OnPlayerBombPlant(this, Hero.WorldPosition);

            if (inputAction.BombBlast)
                world.OnPlayerBombBlast(this);
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
