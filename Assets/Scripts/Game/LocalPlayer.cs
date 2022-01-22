using Configs.Game;
using Entity;
using Input;

namespace Game
{
    public class LocalPlayer : IPlayer
    {
        public PlayerTag PlayerTag { get; }
        public HeroController HeroController { get; }
        public IPlayerInputForwarder PlayerInputForwarder { get; }
    }
}
