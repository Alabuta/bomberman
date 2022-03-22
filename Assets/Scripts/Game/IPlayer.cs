using System;
using Configs;
using Entity.Hero;
using Input;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game
{
    public interface IPlayer
    {
        event Action<IPlayer, int2, fix> HeroMoveEvent;
        event Action<IPlayer, fix2> BombPlantEvent;
        event Action<IPlayer> BombBlastEvent;

        PlayerConfig PlayerConfig { get; }

        Hero Hero { get; }

        void AttachHero(Hero hero);

        void AttachPlayerInput(IPlayerInput playerInput);
    }
}
