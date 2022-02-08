using System;
using Unity.Mathematics;

namespace Entity.Hero
{
    public interface IHero : IEntity
    {
        event Action<float2> BombPlantedEvent;

        int BlastRadius { get; set; }

        int BombCapacity { get; set; }
    }
}
