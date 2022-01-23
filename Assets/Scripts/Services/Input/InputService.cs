using System.Collections.Generic;
using Configs.Game;
using Input;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Services.Input
{
    public class InputService : IInputService
    {
        /*public event Action<float2> OnMoveEvent;
        public event Action OnBombPlantEvent;*/

        private readonly Dictionary<PlayerTagConfig, IPlayerInputForwarder> _playerInputServices =
            new Dictionary<PlayerTagConfig, IPlayerInputForwarder>();

        public IPlayerInputForwarder RegisterPlayerInput(PlayerTagConfig playerTag, int playerIndex, GameObject playerPrefab)
        {
            var controlScheme = "Keyboard";
            var playerInput = PlayerInput.Instantiate(playerPrefab, playerIndex, controlScheme, -1,
                new InputDevice[] { Keyboard.current });
            var component = playerInput.GetComponent<IPlayerInputForwarder>();
            Assert.IsNotNull(component);

            _playerInputServices.Add(playerTag, component);

            return component;
        }

        public IPlayerInputForwarder GetPlayerInputService(PlayerTagConfig playerTag)
        {
            return _playerInputServices.TryGetValue(playerTag, out var inputService) ? inputService : null;
        }
    }
}
