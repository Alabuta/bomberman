using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs.Entity;
using Game;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Tags;
using Game.Systems;
using Game.Systems.RTree;
using Input;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

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

        public async Task OnPlayerBombPlant(IPlayer player, fix2 worldPosition)
        {
            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());

            var heroComponent = heroEntity.Get<EntityComponent>();
            var heroConfig = heroComponent.Config as HeroConfig;
            Assert.IsNotNull(heroConfig);

            var coordinate = LevelTiles.ToTileCoordinate(worldPosition);
            var position = LevelTiles.ToWorldPosition(coordinate);

            var task = CreateAndSpawnBomb(heroConfig.BombConfig, position);
            var entity = await task;

            /*LevelModel.AddItem(entity);

            var bombItem = _ecsWorld.NewEntity(); // _gameFactory.CreateItem(bombConfig.ItemConfig, itemController); :TODO: fix

            LevelModel.AddItem(bombItem, bombCoordinate);*/

            if (!_playerBombs.ContainsKey(player))
                _playerBombs.Add(player, new Queue<EcsEntity>());

            _playerBombs[player].Enqueue(entity);
        }

        public void OnPlayerBombBlast(IPlayer player)
        {
            if (!_playerBombs.TryGetValue(player, out var bombsQueue))
                return;

            if (!bombsQueue.TryDequeue(out var bombEntity))
                return;

            Assert.IsTrue(bombEntity.Has<BombTag>());
            Assert.IsTrue(bombEntity.Has<EntityComponent>());
            Assert.IsTrue(bombEntity.Has<TransformComponent>());

            ref var entityComponent = ref bombEntity.Get<EntityComponent>();
            entityComponent.Controller.Kill();

            ref var transformComponent = ref bombEntity.Get<TransformComponent>();
            var bombPosition = transformComponent.WorldPosition;
            /*var bombCoordinate = LevelModel.ToTileCoordinate(bombEntity.Controller.WorldPosition);
            LevelModel.RemoveItem(bombCoordinate);*/

            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<HeroTag>());
            Assert.IsTrue(heroEntity.Has<EntityComponent>());

            ref var heroComponent = ref heroEntity.Get<EntityComponent>();

            var heroConfig = (HeroConfig) heroComponent.Config;
            var bombBlastDamage = heroConfig.BombBlastDamage; // :TODO: use hero parameters
            var bombBlastRadius = 10; //heroConfig.BombBlastRadius;
            var blastDirections = heroConfig.BombBlastDirections;

            var start = bombPosition;
            var end = bombPosition - new fix2(bombBlastRadius, 0);
            var line = end - start;
            var direction = fix2.normalize_safe(line, fix2.zero);

            using ( ListPool<RTreeLeafEntry>.Get(out var result) )
            {
                _entitiesAabbTree.QueryByLine(start, end, result);

                result.Sort((a, b) =>
                {
                    var vectorA = a.Aabb.GetCenter() - start;
                    var vectorB = b.Aabb.GetCenter() - start;

                    var distanceA = fix2.dot(vectorA, direction);
                    var distanceB = fix2.dot(vectorB, direction);

                    return distanceA.CompareTo(distanceB);
                });

                /*var wallIndex = result.FindIndex(p =>
                {
                    if (p.Index.Has<LevelTileComponent>() &&
                        p.Index.Get<LevelTileComponent>().Type == LevelTileType.HardBlock)
                        return true;

                    return p.Index.Has<WallTag>();
                });*/

                foreach (var entry in result)
                    Debug.LogWarning(entry.Index);
            }

            // var blastLines = GetBombBlastAabbs(blastDirections, bombBlastRadius, bombPosition);

            // InstantiateBlastEffect(blastLines, bombBlastRadius, bombPosition, bombEntity);

            /*ApplyDamageToEntities(blastLines, bombBlastDamage);

            ApplyDamageToBlocks(blastLines);*/

            bombEntity.Destroy();
        }

        /*private IEnumerable<AABB> GetBombBlastAabbs(IReadOnlyList<int2> blastDirections, int blastRadius, fix2 blastPosition)
        {
            return blastDirections
                .Select(blastDirection =>
                {
                    var size = (fix2) (blastDirection * blastRadius) * LevelTiles.TileSize;
                    AABB.CreateFromPositionAndSize(blastPosition, size);
                    return Enumerable
                        .Range(1, blastRadius)
                        .Select(offset => blastPosition + blastDirection * offset)
                        .ToArray();
                })
                .Append(new[] { blastCoordinate });
        }*/

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

        /*private void InstantiateBlastEffect(int2[][] blastLines, int blastRadius, fix2 position, BombItem bombItem)
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

        private void OnEntityHealthChangedEvent(EcsEntity ecsEntity)
        {
            if (ecsEntity.Has<HealthComponent>())
            {
                ref var healthComponent = ref ecsEntity.Get<HealthComponent>();
                if (healthComponent.IsAlive()) // :TODO: refactor
                {
                    if (ecsEntity.Has<EntityComponent>())
                    {
                        ref var entityComponent = ref ecsEntity.Get<EntityComponent>();
                        entityComponent.Controller.Kill();
                    }

                    if (ecsEntity.Has<MovementComponent>())
                    {
                        ref var transformComponent = ref ecsEntity.Get<MovementComponent>();
                        transformComponent.Speed = fix.zero;
                    }

                    if (ecsEntity.Has<HeroTag>())
                    {
                        var (playerInput, _) = _playerInputs.FirstOrDefault(pi => pi.Value.HeroEntity == ecsEntity);

                        if (playerInput != null)
                            playerInput.OnInputActionEvent -= OnPlayerInputAction;
                    }

                    if (ecsEntity.Has<HasColliderTag>())
                    {
                        ecsEntity.Del<CircleColliderComponent>();
                        ecsEntity.Del<BoxColliderComponent>();
                        ecsEntity.Del<HasColliderTag>();
                    }

                    // DeathEvent?.Invoke(this); // :TODO:

                    ecsEntity.Replace(new DeadTag());
                }
                else
                {
                    if (ecsEntity.Has<EntityComponent>())
                    {
                        ref var entityComponent = ref ecsEntity.Get<EntityComponent>();
                        entityComponent.Controller.TakeDamage();
                    }

                    // DamageEvent?.Invoke(this, damage); // :TODO:
                }
            }
        }

        // :TODO: add an item pick up event handler
        // :TODO: add an entity death event handler
    }
}
