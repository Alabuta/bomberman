using System;

namespace GameEntities
{
    public interface IEntity
    {
        bool IsAlive { get; }

        int Health { get; set; }
        int MaxHealth { get; }

        float Speed { get; set; }
        float MaxSpeed { get; }
    }
}
