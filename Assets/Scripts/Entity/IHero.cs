using System;

namespace Entity
{
    public interface IHero : IEntity
    {
        event EventHandler<BombPlantEventData> BombPlantedEvent;

        int BlastRadius { get; set; }

        int BombCapacity { get; set; }
    }
}
