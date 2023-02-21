using Configs.Game;
using Game;
using Input;

namespace Infrastructure.Services.Input
{
    public interface IInputService : IService
    {
        // get specific player input
        // send event when a player connects to the game

        IPlayerInputProvider RegisterPlayerInputProvider(PlayerConfig playerConfig);

        IPlayerInputProvider GetPlayerInputProvider(PlayerTagConfig playerTag);
    }
}
