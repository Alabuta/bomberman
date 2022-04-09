using System.Collections.Generic;
using Game;
using Game.Items;
using Input;
using Items;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        private readonly Dictionary<ulong, List<PlayerInputAction>> _playersInputActions = new();
        private readonly Dictionary<IPlayer, Queue<BombItem>> _playerBombs = new();

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

            var itemController = go.GetComponent<ItemController>();
            Assert.IsNotNull(itemController);

            var bombItem = _gameFactory.CreateItem(bombConfig.ItemConfig, itemController);
            Assert.IsNotNull(bombItem);

            LevelModel.AddItem(bombItem, bombCoordinate);

            if (!_playerBombs.ContainsKey(player))
                _playerBombs.Add(player, new Queue<BombItem>());

            _playerBombs[player].Enqueue(bombItem);
        }

        private void OnPlayerBombBlast(IPlayer player)
        {
            if (!_playerBombs.TryGetValue(player, out var bombsQueue))
                return;

            if (!bombsQueue.TryDequeue(out var bombItem))
                return;

            LevelModel.RemoveItem(LevelModel.ToTileCoordinate(bombItem.Controller.WorldPosition));

            bombItem.Controller.DestroyItem();
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
