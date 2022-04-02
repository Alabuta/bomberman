using System;
using Configs.Entity;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntity
    {
        event Action<IEntity> DeathEvent;
        event Action<IEntity, int> DamageEvent;

        EntityConfig EntityConfig { get; }

        IEntityController EntityController { get; }

        bool IsAlive { get; }

        public Health Health { get; }

        fix Speed { get; set; }
        fix InitialSpeed { get; }
        fix SpeedMultiplier { get; set; }

        int2 Direction { get; set; }

        fix2 WorldPosition { get; set; }

        fix HitRadius { get; }
        fix HurtRadius { get; }
        fix ColliderRadius { get; }

        void Die();
    }
}
