using System;
using Configs.Entity;
using Entity.Hero;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntity
    {
        event Action KillEvent;

        EntityConfig EntityConfig { get; }

        IEntityController EntityController { get; }

        bool IsAlive { get; }

        public Health Health { get; }

        float Speed { get; set; }
        float InitialSpeed { get; }
        float SpeedMultiplier { get; set; }

        int2 Direction { get; set; }

        fix2 WorldPosition { get; set; }

        fix HitRadius { get; }
        fix HurtRadius { get; }

        void Kill();
    }
}
