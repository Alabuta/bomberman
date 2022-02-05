using Configs;
using Configs.Game;
using Input;

namespace Infrastructure.Services.Input
{
    public interface IInputService : IService
    {
        // get specific player input
        // send event when a player connects to the game

        IPlayerInput RegisterPlayerInput(PlayerConfig player);

        IPlayerInput GetPlayerInput(PlayerTagConfig playerTag);
    }
}
