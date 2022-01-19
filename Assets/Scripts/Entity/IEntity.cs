using System;
using Unity.Mathematics;

namespace Entity
{
    public interface IEntity
    {
        event Action OnKillEvent;

        bool IsAlive { get; }

        int Health { get; set; }
        int MaxHealth { get; }

        float Speed { get; set; }
        float InitialSpeed { get; }

        float3 WorldPosition { get; }
        float2 MovementVector { get; set; }

        void Kill();
    }
}
