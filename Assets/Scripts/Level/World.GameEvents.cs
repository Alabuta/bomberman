using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Items;
using Input;
using Items;
using Math.FixedPointMath;
using Unity.Mathematics;
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

        public void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
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

        public void OnPlayerBombBlast(IPlayer player)
        {
            if (!_playerBombs.TryGetValue(player, out var bombsQueue))
                return;

            if (!bombsQueue.TryDequeue(out var bombItem))
                return;

            var bombCoordinate = LevelModel.ToTileCoordinate(bombItem.Controller.WorldPosition);
            LevelModel.RemoveItem(bombCoordinate);

            var position = LevelModel.ToWorldPosition(bombCoordinate);

            bombItem.Controller.DestroyItem();

            var go = _gameFactory.InstantiatePrefab(bombItem.Config.BlastEffectConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var effectController = go.GetComponent<EffectController>();
            Assert.IsNotNull(effectController);

            const int blastRadius = 2;

            int2[] offsets =
            {
                new(1, 0),
                new(-1, 0),
                new(0, 1),
                new(0, -1)
            };

            var sizes = offsets
                .Select(v =>
                {
                    return Enumerable
                        .Range(1, blastRadius)
                        .Select(o => bombCoordinate + v * o)
                        .TakeWhile(c =>
                        {
                            if (!LevelModel.IsCoordinateInField(c))
                                return false;

                            var tile = LevelModel[c];
                            return tile.TileLoad == null;
                        })
                        .Count();
                })
                .ToArray();

            effectController.SetSize(new int4(sizes[0], sizes[1], sizes[2], sizes[3]));

            var effectAnimator = go.GetComponent<EffectAnimator>();
            Assert.IsNotNull(effectAnimator);

            effectAnimator.OnAnimationStateEnter += state =>
            {
                if (state == AnimatorState.Finished)
                    go.SetActive(false);
            };
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
