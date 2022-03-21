using Configs.Entity;

namespace Entity.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public BombConfig BombConfig { get; }

        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
            BombConfig = config.BombConfig;
        }
    }
}
