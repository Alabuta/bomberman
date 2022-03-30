using Configs.Entity;

namespace Entity.Enemies
{
    public class Enemy : Entity<EnemyConfig>
    {
        public Enemy(EnemyConfig config, EnemyController entityController)
            : base(config, entityController)
        {
        }
    }
}
