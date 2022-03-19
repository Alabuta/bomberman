using System.Collections.Generic;
using Configs;
using Configs.Game;
using Infrastructure.Factory;
using Input;
using UnityEngine.Assertions;

namespace Infrastructure.Services.Input
{
    public class InputService : IInputService
    {
        public const string ControlScheme = "Keyboard";

        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<PlayerTagConfig, IPlayerInput> _playerInputs = new();


        public InputService(IGameFactory gameFactory)
        {
            _gameFactory = gameFactory;
        }

        public IPlayerInput RegisterPlayerInput(PlayerConfig player)
        {
            var playerIndex = _playerInputs.Count;

            var component = _gameFactory.CreatePlayerInputHolder(player, playerIndex);
            Assert.IsNotNull(component);

            _playerInputs.Add(player.PlayerTagConfig, component);

            return component;
        }

        public IPlayerInput GetPlayerInput(PlayerTagConfig playerTag)
        {
            return _playerInputs.TryGetValue(playerTag, out var inputService) ? inputService : null;
        }
    }
}
