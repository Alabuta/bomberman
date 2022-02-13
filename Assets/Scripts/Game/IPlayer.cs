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

        Hero Hero { get; }

        void AttachHero(Hero hero);

        void AttachPlayerInput(IPlayerInput playerInput);
    }
}
