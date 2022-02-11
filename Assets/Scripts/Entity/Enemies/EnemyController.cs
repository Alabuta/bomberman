using Unity.Mathematics;

namespace Entity.Enemies
{
    public class EnemyController : EntityController
    {
        public int Health { get; set; }
        public float CurrentSpeed { get; protected set; }
        public override float Speed { get; set; }
        public override float2 Direction { get; set; }
        protected override EntityAnimator EntityAnimator { get; }
    }
}
