using Configs.Game;
using Entity;
using Input;

namespace Game
{
    public interface IPlayer
    {
        PlayerTag PlayerTag { get; }
        HeroController HeroController { get; }
        IPlayerInputForwarder PlayerInputForwarder { get; }
    }
}
