using Configs.Entity;
using Game.Enemies;
using Math.FixedPointMath;

namespace Game.Components
{
    public struct EnemyComponent
    {
        public EnemyConfig Config;
        public EnemyController Controller;

        public fix HitRadius;
        public fix HurtRadius;

        // public fix CurrentSpeed;
        public fix InitialSpeed;
        public fix SpeedMultiplier;

        public int InteractionLayerMask;
    }
}
