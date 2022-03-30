using System;
using System.Collections.Generic;
using Game;
using Input;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        private readonly Dictionary<ulong, List<PlayerInputAction>> _playersInputActions = new();

        private void OnPlayerInputAction(PlayerInputAction inputActions)
        {
            if (!_playersInputActions.ContainsKey(Tick))
                _playersInputActions.Add(Tick, new List<PlayerInputAction>());

            _playersInputActions[Tick].Add(inputActions);
        }

        private void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var bombConfig = player.Hero.BombConfig;
            var bombCoordinate = LevelModel.ToTileCoordinate(worldPosition);
            var position = LevelModel.ToWorldPosition(bombCoordinate);

            var go = _gameFactory.InstantiatePrefab(bombConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var bombItem = _gameFactory.CreateItem(bombConfig.ItemConfig);
            Assert.IsNotNull(bombItem);

            LevelModel.AddItem(bombItem, bombCoordinate);
        }

        private void OnPlayerBombBlast(IPlayer player)
        {
            throw new NotImplementedException();
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
