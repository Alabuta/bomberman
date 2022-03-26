using System;
using System.Collections.Generic;
using Game;
using Input;
using Math.FixedPointMath;

namespace Level
{
    public partial class World
    {
        private readonly Dictionary<ulong, List<PlayerInputAction>> _playerInputActions = new();

        private void OnPlayerInputAction(PlayerInputAction inputActions)
        {
            if (!_playerInputActions.ContainsKey(Tick))
                _playerInputActions.Add(Tick, new List<PlayerInputAction>());

            _playerInputActions[Tick].Add(inputActions);
        }

        private void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var bombConfig = player.Hero.BombConfig;
            var bombCoordinate = LevelGridModel.ToTileCoordinate(worldPosition, true);
            var position = LevelGridModel.ToWorldPosition(bombCoordinate);

            _gameFactory.InstantiatePrefab(bombConfig.Prefab, fix2.ToXY(position));
        }

        private void OnPlayerBombBlast(IPlayer player)
        {
            throw new NotImplementedException();
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
