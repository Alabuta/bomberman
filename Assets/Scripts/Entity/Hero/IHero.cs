using System;

namespace Entity.Hero
{
    public interface IHero : IEntity
    {
        event Action<BombPlantEventData> BombPlantedEvent;

        int BlastRadius { get; set; }

        int BombCapacity { get; set; }
    }
}
