using Configs;
using Input;
using Level;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig PlayerConfig { get; }

        Hero.Hero Hero { get; }

        void AttachHero(Hero.Hero hero);

        void ApplyInputAction(World world, PlayerInputAction inputAction);
    }
}
