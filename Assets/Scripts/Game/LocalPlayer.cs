using Configs.Game;
using Entity;
using Input;

namespace Game
{
    public class LocalPlayer : IPlayer
    {
        public PlayerTagConfig PlayerTagConfig { get; }
        public HeroController HeroController { get; }
        public IPlayerInputForwarder PlayerInputForwarder { get; }
    }
}
