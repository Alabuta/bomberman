using System;
using Configs;
using Entity.Hero;
using Input;
using Unity.Mathematics;

namespace Game
{
    public interface IPlayer
    {
        event Action<float2> BombPlantedEvent;

        PlayerConfig PlayerConfig { get; }

        HeroController HeroController { get; }

        bool IsAlive { get; }

        int Health { get; set; }
        int InitialHealth { get; }

        float Speed { get; }
        float InitialSpeed { get; }
        float SpeedMultiplier { get; set; }

        float2 Direction { get; set; }

        float3 WorldPosition { get; }

        void AttachHero(HeroController heroController);

        void AttachPlayerInput(IPlayerInput playerInput);
    }
}
