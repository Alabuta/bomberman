using System.Collections.Generic;
using Game;
using Game.Components.Entities;
using Game.Components.Tags;
using Input;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        private readonly Dictionary<ulong, List<PlayerInputAction>> _playersInputActions = new();
        private readonly Dictionary<IPlayer, Queue<EcsEntity>> _playerBombs = new();

        private void OnPlayerInputAction(PlayerInputAction inputActions)
        {
            if (!_playersInputActions.ContainsKey(Tick))
                _playersInputActions.Add(Tick, new List<PlayerInputAction>());

            _playersInputActions[Tick].Add(inputActions);
        }

        public void OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());

            ref var heroComponent = ref heroEntity.Get<EntityComponent>();

            /*var bombConfig = ((HeroConfig) heroComponent.Config).BombConfig;
            var bombCoordinate = LevelModel.ToTileCoordinate(worldPosition);
            var position = LevelModel.ToWorldPosition(bombCoordinate);

            var go = _gameFactory.InstantiatePrefab(bombConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var itemController = go.GetComponent<ItemController>();
            Assert.IsNotNull(itemController);

            var bombItem = _ecsWorld.NewEntity(); // _gameFactory.CreateItem(bombConfig.ItemConfig, itemController); :TODO: fix

            // LevelModel.AddItem(bombItem, bombCoordinate);

            if (!_playerBombs.ContainsKey(player))
                _playerBombs.Add(player, new Queue<EcsEntity>());

            _playerBombs[player].Enqueue(bombItem);*/
        }

        public void OnPlayerBombBlast(IPlayer player)
        {
            if (!_playerBombs.TryGetValue(player, out var bombsQueue))
                return;

            if (!bombsQueue.TryDequeue(out var bombItem))
                return;

            /*var bombCoordinate = LevelModel.ToTileCoordinate(bombItem.Controller.WorldPosition); :TODO: fix

            LevelModel.RemoveItem(bombCoordinate);
            bombItem.Controller.DestroyItem();

            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());

            ref var heroComponent = ref heroEntity.Get<EntityComponent>();

            var heroConfig = (HeroConfig) heroComponent.Config;
            var bombBlastDamage = heroConfig.BombBlastDamage; // :TODO: use hero parameters
            var bombBlastRadius = heroConfig.BombBlastRadius;
            var blastDirections = heroConfig.BombBlastDirections;

            var blastLines = GetBombBlastTileLines(blastDirections, bombBlastRadius, bombCoordinate).ToArray();

            var bombPosition = LevelModel.ToWorldPosition(bombCoordinate);
            InstantiateBlastEffect(blastLines, bombBlastRadius, bombPosition, bombItem);

            ApplyDamageToEntities(blastLines, bombBlastDamage);

            ApplyDamageToBlocks(blastLines);*/
        }

        private void ApplyDamageToBlocks(int2[][] blastLines)
        {
            /*var blocksToDestroy = blastLines //:TODO: fix
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

                var effectAnimator = effectGameObject.GetComponent<EffectAnimator>();
                Assert.IsNotNull(effectAnimator);

                effectAnimator.OnAnimationStateEnter += state =>
                {
                    if (state == AnimatorState.Finish)
                        effectGameObject.SetActive(false);
                };
            }*/
        }

        private void ApplyDamageToEntities(int2[][] blastLines, int bombBlastDamage)
        {
            /*var entitiesToBeDamaged = blastLines // :TODO: fix
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
                        .SelectMany(GetEnemiesByCoordinate);
                });*/

            /*foreach (var entity in entitiesToBeDamaged) :TODO: refactor
                entity.Health.ApplyDamage(bombBlastDamage);*/
        }

        /*private void InstantiateBlastEffect(int2[][] blastLines, int blastRadius, fix2 position, BombItem bombItem) :TODO: fix
        {
            var go = _gameFactory.InstantiatePrefab(bombItem.Config.BlastEffectConfig.Prefab, fix2.ToXY(position));
            Assert.IsNotNull(go);

            var effectController = go.GetComponent<BlastEffectController>();
            Assert.IsNotNull(effectController);

            /*var blastRadiusInDirections = blastLines // :TODO: fix
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

            effectController.Construct(blastRadius, blastRadiusInDirections);#1#

            var effectAnimator = go.GetComponent<EffectAnimator>();
            Assert.IsNotNull(effectAnimator);

            effectAnimator.OnAnimationStateEnter += state =>
            {
                if (state == AnimatorState.Finish)
                    go.SetActive(false);
            };
        }*/

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
