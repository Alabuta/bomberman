using Configs.Game;
using Entity;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerTagConfig PlayerTagConfig { get; }
        HeroController HeroController { get; }
        IPlayerInputForwarder PlayerInputForwarder { get; }
    }
}
