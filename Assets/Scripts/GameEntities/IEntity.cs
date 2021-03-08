using System;

namespace GameEntities
{
    public interface IEntity
    {
        bool IsAlive { get; }

        IObservable<int> HealthPoints { get; set; }

        int Health { get; set; }
        int MaxHealth { get; }

        float Speed { get; set; }
        float MaxSpeed { get; }
    }
}
