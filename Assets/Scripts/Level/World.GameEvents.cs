using System.Linq;
using Configs.Effects;
using Configs.Entity;
using Game;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Level
{
    public partial class World
    {
        public void HeroHasDied(EcsEntity entity)
        {
            var (playerInputProvider, _) =
                _playerInputProviders.FirstOrDefault(pi => pi.Value.HeroEntity == entity);

            if (playerInputProvider != null)
                _playersInputHandlerSystem.UnsubscribePlayerInputProvider(playerInputProvider);
        }

        public void CreatePlayerBombPlantAction(IPlayer player)
        {
            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());
            Assert.IsTrue(heroEntity.Has<TransformComponent>());

            var heroComponent = heroEntity.Get<EntityComponent>();
            var heroConfig = heroComponent.Config as HeroConfig;
            Assert.IsNotNull(heroConfig);

            // :TODO: use player current parameters instead of static data
            var bombBlastDamage = (fix) heroConfig.BombBlastDamage;
            var bombBlastRadius = heroConfig.BombBlastRadius;
            var bombBlastDelay = player.HasRemoConBomb ? new fix(-1) : (fix) heroConfig.BombBlastDelay;

            ref var transformComponent = ref heroEntity.Get<TransformComponent>();

            var coordinate = LevelTiles.ToTileCoordinate(transformComponent.WorldPosition);
            var position = LevelTiles.ToWorldPosition(coordinate);

            var eventEntity = _ecsWorld.NewEntity();
            eventEntity.Replace(new OnBombPlantActionEventComponent(
                playerTag: player.PlayerTag,
                position: position,
                bombConfig: heroConfig.BombConfig,
                blastDelay: bombBlastDelay,
                bombBlastDamage: bombBlastDamage,
                bombBlastRadius: bombBlastRadius
            ));
        }

        public void CreatePlayerBombBlastAction(IPlayer player)
        {
            var eventEntity = _ecsWorld.NewEntity();
            eventEntity.Replace(new OnBombBlastActionEventComponent(playerTag: player.PlayerTag));
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

        private void InstantiateBlastEffect(int2[][] blastLines, int blastRadius, fix2 position,
            BlastEffectConfig blastEffectConfig)
        {
            var go = _gameFactory.InstantiatePrefab(blastEffectConfig.Prefab, fix2.ToXY(position));
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

            effectController.Construct(blastRadius, blastRadiusInDirections);*/

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
