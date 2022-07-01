using System.Collections.Generic;
using Configs.Entity;
using Configs.Game.Colliders;
using Game.Colliders;
using Game.Components;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Hero
{
    public class Hero : Entity<HeroConfig>
    {
        public BombConfig BombConfig { get; }

        public int BombBlastDamage { get; }

        public int BombBlastRadius { get; }

        public int2[] BombBlastDirections { get; }

        public Hero(HeroConfig config, HeroController entityController)
            : base(config, entityController)
        {
            BombConfig = config.BombConfig;

            BombBlastDamage = config.BombBlastDamage;
            BombBlastRadius = config.BombBlastRadius;

            BombBlastDirections = config.BombBlastDirections;

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
