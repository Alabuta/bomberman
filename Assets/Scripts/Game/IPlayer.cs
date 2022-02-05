using Configs;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerConfig Config { get; }

        // HeroController HeroController { get; }
        IPlayerInput PlayerInput { get; }
    }
}
