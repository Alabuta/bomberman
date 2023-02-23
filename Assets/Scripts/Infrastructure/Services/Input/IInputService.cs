using Configs.Game;
using Input;
using JetBrains.Annotations;

namespace Infrastructure.Services.Input
{
    public interface IInputService : IService
    {
        // get specific player input
        // send event when a player connects to the game

        [CanBeNull]
        IPlayerInputProvider RegisterPlayerInputProvider(PlayerConfig playerConfig);

        bool TryGetRegisteredPlayerTag(IPlayerInputProvider playerInputProvider, out PlayerTagConfig playerTag);
    }
}
