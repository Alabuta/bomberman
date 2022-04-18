using Configs.Entity;
using Configs.Game.Colliders;
using Game.Colliders;
using Game.Components;
using Math.FixedPointMath;

namespace Game.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public BombConfig BombConfig { get; }

        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
            BombConfig = config.BombConfig;

            var collider = new CircleColliderComponent(config.Collider as CircleColliderComponentConfig);
            Components = new Component[]
            {
                collider
            };
        }

        public void UpdatePosition(fix deltaTime)
        {
            WorldPosition += (fix2) Direction * Speed * deltaTime;
        }
    }
}
