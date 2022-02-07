using System;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntity
    {
        event Action OnKillEvent;

        bool IsAlive { get; }

        int Health { get; set; }
        int InitialHealth { get; }

        float CurrentSpeed { get; }
        float InitialSpeed { get; }
        float SpeedMultiplier { get; set; }

        float3 WorldPosition { get; }
        float2 DirectionVector { get; }

        void Kill();
    }
}
