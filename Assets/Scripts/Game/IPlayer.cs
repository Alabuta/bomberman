using Configs;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        Hero.Hero Hero { get; }

        void AttachHero(Hero.Hero hero);

        void ApplyInputAction(PlayerInputAction inputAction);
    }
}
