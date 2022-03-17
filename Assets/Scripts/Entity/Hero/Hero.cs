using Configs.Entity;

namespace Entity.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public HeroHealth HeroHealth { get; }

        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
            HeroHealth = new HeroHealth(config.Health);
            HeroHealth.HealthChangedEvent += OnHealthChanged;
        }

        private void OnHealthChanged(int health)
        {
            if (health < 1)
                Kill();
        }
    }
}
