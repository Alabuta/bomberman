using System;
using Configs;
using Entity.Hero;
using Input;
using Math.FixedPointMath;

namespace Game
{
    public interface IPlayer
    {
        event Action<fix2> BombPlantEvent;
        event Action OnBombBlastEvent;

        PlayerConfig PlayerConfig { get; }

        Hero Hero { get; }

        void AttachHero(Hero hero);

        void AttachPlayerInput(IPlayerInput playerInput);
    }
}
