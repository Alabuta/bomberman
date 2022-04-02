using System.Collections.Generic;
using Configs.Game;
using Game;
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

        public IPlayerInput RegisterPlayerInput(IPlayer player)
        {
            var playerIndex = _playerInputs.Count;
            var playerConfig = player.PlayerConfig;

            var component = _gameFactory.CreatePlayerInputHolder(playerConfig, playerIndex);
            Assert.IsNotNull(component);

            if (_playerInputs.ContainsKey(playerConfig.PlayerTagConfig))
                _playerInputs[playerConfig.PlayerTagConfig] = component;

            else
                _playerInputs.Add(playerConfig.PlayerTagConfig, component);

            return component;
        }

        public IPlayerInput GetPlayerInput(PlayerTagConfig playerTag)
        {
            return _playerInputs.TryGetValue(playerTag, out var inputService) ? inputService : null;
        }
    }
}
