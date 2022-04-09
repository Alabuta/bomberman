using Configs.Entity;

namespace Game.Enemies
{
    public class Enemy : Entity<EnemyConfig>
    {
        public Enemy(EnemyConfig config, EnemyController entityController)
            : base(config, entityController)
        {
        }
    }
}
