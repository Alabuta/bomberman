using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Items;
using Input;
using Items;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
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
            bombItem.Controller.DestroyItem();

            const int blastRadius = 5;
            const int bombBlastDamage = 1;

            int2[] blastDirections =
            {
                new(1, 0),
                new(-1, 0),
                new(0, 1),
                new(0, -1)
            };

            var blastLines = blastDirections
                .Select(direction =>
                {
                    return Enumerable
                        .Range(1, blastRadius)
                        .Select(o => bombCoordinate + direction * o)
                        .ToArray();
                })
                .Append(new[] { bombCoordinate })
                .ToArray();

            InstantiateBlastEffect(blastLines, blastRadius, bombCoordinate, bombItem);

            var entitiesToKill = blastLines
                .SelectMany(blastLine =>
                {
                    return blastLine
                        .TakeWhile(c =>
                        {
                            if (!LevelModel.IsCoordinateInField(c))
                                return false;

                            var tileLoad = LevelModel[c].TileLoad;
                            return tileLoad is not (HardBlock or SoftBlock);
                        })
                        .SelectMany(c =>
                        {
                            return _enemies
                                .Where(e => math.all(LevelModel.ToTileCoordinate(e.WorldPosition) == c));
                        });
                });

            foreach (var entity in entitiesToKill)
                entity.Health.ApplyDamage(bombBlastDamage);

            var blocksToDestroy = blastLines
                .Select(blastLine =>
                {
                    return blastLine
                        .TakeWhile(c =>
                        {
                            if (!LevelModel.IsCoordinateInField(c))
                                return false;

                            return LevelModel[c].TileLoad is not HardBlock;
                        })
                        .Select(c =>
                        {
                            var tileLoad = LevelModel[c].TileLoad;
                            return tileLoad is SoftBlock ? LevelModel[c] : null;
                        })
                        .FirstOrDefault(c => c != null);
                })
                .Where(b => b != null);

            foreach (var tileLoad in blocksToDestroy)
            {
                var blockCoordinate = tileLoad.Coordinate;
                var effectPosition = LevelModel.ToWorldPosition(blockCoordinate);

                var levelTile = LevelModel[blockCoordinate];
                var destroyEffectPrefab = levelTile.TileLoad.DestroyEffectPrefab;

                DestroyBlockPrefab(blockCoordinate);
                LevelModel.ClearTile(blockCoordinate);

                var effectGameObject = _gameFactory.InstantiatePrefab(destroyEffectPrefab, fix2.ToXY(effectPosition));
                Assert.IsNotNull(effectGameObject);

                var effectAnimator2 = effectGameObject.GetComponent<EffectAnimator>();
                Assert.IsNotNull(effectAnimator2);

                effectAnimator2.OnAnimationStateEnter += state =>
                {
                    if (state == AnimatorState.Finish)
                        effectGameObject.SetActive(false);
                };
            }
        }

        private void InstantiateBlastEffect(int2[][] blastLines, int blastRadius, int2 bombCoordinate, BombItem bombItem)
        {
            var position = LevelModel.ToWorldPosition(bombCoordinate);

            var go = _gameFactory.InstantiatePrefab(bombItem.Config.BlastEffectConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var effectController = go.GetComponent<BlastEffectController>();
            Assert.IsNotNull(effectController);

            var blastRadiusInDirections = blastLines
                .Select(blastLine =>
                {
                    return blastLine
                        .TakeWhile(c =>
                        {
                            if (!LevelModel.IsCoordinateInField(c))
                                return false;

                            return LevelModel[c].TileLoad == null;
                        })
                        .Count();
                });

            effectController.Construct(blastRadius, blastRadiusInDirections);

            var effectAnimator = go.GetComponent<EffectAnimator>();
            Assert.IsNotNull(effectAnimator);

            effectAnimator.OnAnimationStateEnter += state =>
            {
                if (state == AnimatorState.Finish)
                    go.SetActive(false);
            };
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
