using Configs;
using Entity.Hero;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        Hero Hero { get; }

        void AttachHero(Hero hero);

        void ApplyInputAction(PlayerInputAction inputAction);
    }
}
