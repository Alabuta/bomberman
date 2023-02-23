using System.Collections.Generic;
using Configs.Game;
using Input;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace Infrastructure.Services.Input
{
    public class InputService : IInputService
    {
        private readonly InputDevice[] _inputDevices = { Keyboard.current }; // :TODO: make it configurable

        private const string ControlScheme = "Keyboard";

        private readonly Dictionary<IPlayerInputProvider, PlayerTagConfig> _playerInputProviders = new();

        [Preserve]
        public InputService()
        {
        }

        public IPlayerInputProvider RegisterPlayerInputProvider(PlayerConfig playerConfig)
        {
            var playerIndex = _playerInputProviders.Count;

            var playerInputProvider = CreatePlayerInputProvider(playerConfig, playerIndex);
            if (playerInputProvider == null)
                return null;

            _playerInputProviders[playerInputProvider] = playerConfig.PlayerTagConfig;

            return playerInputProvider;
        }

        public bool TryGetRegisteredPlayerTag(IPlayerInputProvider playerInputProvider, out PlayerTagConfig playerTag) =>
            _playerInputProviders.TryGetValue(playerInputProvider, out playerTag);

        private IPlayerInputProvider CreatePlayerInputProvider(PlayerConfig playerConfig, int playerIndex)
        {
            var playerInput = PlayerInput.Instantiate(
                playerConfig.PlayerInputHolder,
                playerIndex,
                ControlScheme,
                -1,
                _inputDevices);

            return playerInput != null ? playerInput.GetComponent<IPlayerInputProvider>() : null;
        }
    }
}
