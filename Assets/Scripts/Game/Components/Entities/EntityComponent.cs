using Configs.Entity;
using Math.FixedPointMath;

namespace Game.Components.Entities
{
    public readonly struct EntityComponent
    {
        public readonly EntityConfig Config;
        public readonly EntityController Controller;

        public readonly fix InitialSpeed;
        public readonly fix SpeedMultiplier;

        public EntityComponent(EntityConfig config, EntityController controller, fix initialSpeed, fix speedMultiplier)
        {
            Config = config;
            Controller = controller;
            InitialSpeed = initialSpeed;
            SpeedMultiplier = speedMultiplier;
        }
    }
}
