using System;
using Configs;
using Entity.Hero;
using Input;
using Math.FixedPointMath;

namespace Game
{
    public interface IPlayer
    {
        event Action<fix2> BombPlantedEvent;

        PlayerConfig PlayerConfig { get; }

        Hero Hero { get; }

        void AttachHero(Hero hero);

        void AttachPlayerInput(IPlayerInput playerInput);
    }
}
