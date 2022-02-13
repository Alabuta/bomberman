using Configs.Entity;

namespace Entity.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
        }
    }
}
