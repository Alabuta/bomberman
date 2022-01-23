using Configs.Game;
using Input;
using UnityEngine;

namespace Services.Input
{
    public interface IInputService
    {
        // get specific player input
        // send event when a player connects to the game

        /*event Action<float2> OnMoveEvent;
        event Action OnBombPlantEvent;*/

        IPlayerInputForwarder RegisterPlayerInput(PlayerTagConfig playerTag, int playerIndex, GameObject playerPrefab);

        IPlayerInputForwarder GetPlayerInputService(PlayerTagConfig playerTag);
    }
}
