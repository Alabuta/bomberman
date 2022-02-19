using System;
using Configs.Entity;
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

        float3 WorldPosition { get; }

        void Kill();
    }
}
