using Configs.Entity;
using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public struct EntityComponent
    {
        public EntityConfig Config;
        public EntityController Controller;

        public fix HitRadius;
        public fix HurtRadius;

        // public fix CurrentSpeed;
        public fix InitialSpeed;
        public fix SpeedMultiplier;

        public int LayerMask;
    }
}
