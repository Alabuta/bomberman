using Configs;
using Entity.Hero;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig Config { get; }

        IHero Hero { get; }

        void AttachHero(IHero hero, IPlayerInput playerInput);
    }
}
