using Configs.Entity;
using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public struct EntityComponent
    {
        public EntityConfig Config;
        public EntityController Controller;

        public fix InitialSpeed;
        public fix SpeedMultiplier;
    }
}
