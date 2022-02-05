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
        private readonly IGameFactory _gameFactory;

        private readonly Dictionary<PlayerTagConfig, IPlayerInput> _playerInputServices = new();

        public InputService(IGameFactory gameFactory)
        {
            _gameFactory = gameFactory;
        }

        public IPlayerInput RegisterPlayerInput(PlayerConfig player)
        {
            var playerIndex = _playerInputServices.Count;

            var component = _gameFactory.CreatePlayerInputHolder(player, playerIndex);
            Assert.IsNotNull(component);

            _playerInputServices.Add(player.PlayerTagConfig, component);

            return component;
        }

        public IPlayerInput GetPlayerInput(PlayerTagConfig playerTag)
        {
            return _playerInputServices.TryGetValue(playerTag, out var inputService) ? inputService : null;
        }
    }
}
