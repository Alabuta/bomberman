using System;
using Configs.Entity;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntity
    {
        event Action OnKillEvent;

        EntityConfig EntityConfig { get; }

        IEntityController EntityController { get; }

        bool IsAlive { get; }

        int Health { get; set; }
        int InitialHealth { get; }

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
