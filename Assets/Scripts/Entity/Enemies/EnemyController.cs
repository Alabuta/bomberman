using Configs.Entity;

namespace Entity.Enemies
{
    public class EnemyController : EntityController<EnemyConfig>, IEnemy
    {
        public override int Health { get; set; }
        public override float Speed { get; set; }
    }
}
