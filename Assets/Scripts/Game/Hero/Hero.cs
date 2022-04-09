using Configs.Entity;
using Math.FixedPointMath;

namespace Game.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public BombConfig BombConfig { get; }

        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
            BombConfig = config.BombConfig;
        }

        public void UpdatePosition(fix deltaTime)
        {
            WorldPosition += (fix2) Direction * Speed * deltaTime;
        }
    }
}
