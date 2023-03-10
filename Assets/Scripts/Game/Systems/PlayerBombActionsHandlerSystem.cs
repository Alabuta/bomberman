using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Configs.Game;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Game.Systems.RTree;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Game.Systems
{
    public class PlayerBombActionsHandlerSystem : IEcsRunSystem
    {
        private static readonly fix2[] BlastDirections =
        {
            // :TODO: might be better to get it from a config
            new(1, 0),
            new(0, 1) /*,
            new(-1, 0),
            new(0, -1)*/
        };

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly Dictionary<PlayerTagConfig, Queue<EcsEntity>> _plantedBombsQueue = new();

        private readonly EcsFilter<OnBombPlantActionEventComponent> _bombPlantEvents;
        private readonly EcsFilter<OnBombBlastActionEventComponent> _bomBlastEvents;

        private readonly EcsFilter<BombComponent, TransformComponent, EntityComponent> _plantedBombs;

        public void Run()
        {
            ProcessPlantActions();
            ProcessBlastActions();
            ProcessPlantedTimeBombs();
        }

        private void ProcessPlantActions()
        {
            if (_bombPlantEvents.IsEmpty())
                return;

            using var _ = ListPool<Task<EcsEntity>>.Get(out var tasks);

            foreach (var index in _bombPlantEvents)
            {
                var eventComponent = _bombPlantEvents.Get1(index);

                var task = _world.CreateAndSpawnBomb(
                    eventComponent.Position,
                    eventComponent.BombConfig,
                    eventComponent.BlastDelay,
                    eventComponent.BombBlastDamage,
                    eventComponent.BombBlastRadius
                );
                task.ContinueWith(t => // :TODO: refactor
                {
                    var playerTag = eventComponent.PlayerTag;
                    if (!_plantedBombsQueue.TryGetValue(playerTag, out var queue))
                    {
                        queue = new Queue<EcsEntity>();
                        _plantedBombsQueue.Add(playerTag, queue);
                    }

                    queue.Enqueue(t.Result);
                });
                tasks.Add(task);
            }

            Task.WhenAll(tasks);
        }

        private void ProcessBlastActions()
        {
            if (_bomBlastEvents.IsEmpty())
                return;

            foreach (var index in _bomBlastEvents)
            {
                ref var eventComponent = ref _bomBlastEvents.Get1(index);

                if (!_plantedBombsQueue.TryGetValue(eventComponent.PlayerTag, out var bombsQueue))
                    continue;

                while (bombsQueue.Count > 0)
                {
                    if (!bombsQueue.TryDequeue(out var bombEntity))
                        continue;

                    if (!bombEntity.IsAlive())
                        break;

                    Assert.IsTrue(bombEntity.Has<BombComponent>());
                    Assert.IsTrue(bombEntity.Has<EntityComponent>());
                    Assert.IsTrue(bombEntity.Has<TransformComponent>());

                    ref var bombComponent = ref bombEntity.Get<BombComponent>();
                    ref var entityComponent = ref bombEntity.Get<EntityComponent>();
                    ref var transformComponent = ref bombEntity.Get<TransformComponent>();

                    BlastBomb(transformComponent, in entityComponent, in bombComponent);

                    // bombEntity.Destroy(); // :TODO: remove
                }
            }
        }

        private void ProcessPlantedTimeBombs()
        {
            if (_plantedBombs.IsEmpty())
                return;

            foreach (var index in _plantedBombs)
            {
                ref var bombComponent = ref _plantedBombs.Get1(index);
                if (bombComponent.BlastWorldTick > _world.Tick)
                    continue;

                var bombEntity = _plantedBombs.GetEntity(index);

                ref var transformComponent = ref _plantedBombs.Get2(index);
                ref var entityComponent = ref _plantedBombs.Get3(index);

                BlastBomb(transformComponent, in entityComponent, in bombComponent);

                // bombEntity.Destroy(); // :TODO: remove
            }
        }

        private void BlastBomb(
            TransformComponent transformComponent,
            in EntityComponent entityComponent,
            in BombComponent bombComponent)
        {
            var entries = ListPool<RTreeLeafEntry>.Get();
            var processedEntries = HashSetPool<int>.Get();

            var blastRadius = 1; //bombComponent.BlastRadius;

            var bombPosition = transformComponent.WorldPosition;
            var radius = (fix) blastRadius;

            var blastRadiusInDirections = ListPool<int>.Get();

            foreach (var blastDirection in BlastDirections)
            {
                entries.Clear();

                var blastSize = blastDirection * radius;
                var startPoint = bombPosition - blastSize;
                var endPoint = bombPosition + blastSize;

                _world.EntitiesAabbTree.QueryByLine(startPoint, endPoint, entries);
                entries.Sort((a, b) =>
                {
                    var vectorA = a.Aabb.GetCenter() - bombPosition;
                    var vectorB = b.Aabb.GetCenter() - bombPosition;

                    var distanceA = fix2.dot(vectorA, blastDirection);
                    var distanceB = fix2.dot(vectorB, blastDirection);

                    return distanceA.CompareTo(distanceB);
                });

                var mask = blastDirection != fix2.zero;

                var leftWallEntityIndex = -1;
                var rightWallEntityIndex = -1;
                for (var i = 0; i < entries.Count; i++)
                {
                    var leafEntry = entries[i];
                    var entity = _world.EntitiesMap[leafEntry.Index];
                    if (!entity.Has<HardBlockComponent>())
                        continue;

                    var aabbCenter = leafEntry.Aabb.GetCenter();
                    if (math.any((aabbCenter < bombPosition) & mask))
                    {
                        leftWallEntityIndex = i;
                        continue;
                    }

                    rightWallEntityIndex = i;
                    break;
                }

                foreach (var entry in entries)
                {
                    var entryIndex = entry.Index;
                    if (processedEntries.Contains(entryIndex))
                        continue;

                    processedEntries.Add(entryIndex);

                    var targetEntity = _world.EntitiesMap[entryIndex];
                    if (!targetEntity.IsAlive())
                        continue;

                    var eventEntity = _ecsWorld.NewEntity();
                    eventEntity.Replace(new AttackEventComponent(targetEntity, bombComponent.BlastDamage));
                }

                /*var wallIndex = entries.FindIndex(p =>
                {
                    if (p.Index.Has<LevelTileComponent>() &&
                        p.Index.Get<LevelTileComponent>().Type == LevelTileType.HardBlock)
                        return true;

                    return p.Index.Has<WallTag>();
                });*/
            }

            // :TODO: refactor as an action apply operation
            _world.InstantiateBlastEffect(blastRadiusInDirections, blastRadius, bombPosition, bombComponent.BlastEffect);

            entityComponent.Controller.Kill();

            ListPool<int>.Release(blastRadiusInDirections);
            ListPool<RTreeLeafEntry>.Release(entries);
            HashSetPool<int>.Release(processedEntries);
        }

#if false
            ref var entityComponent = ref bombEntity.Get<EntityComponent>();
            entityComponent.Controller.Kill();
            /*var bombCoordinate = LevelModel.ToTileCoordinate(bombEntity.Controller.WorldPosition);
            LevelModel.RemoveItem(bombCoordinate);*/

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

            bombEntity.Destroy(); // :TODO: remove
#endif

        /*private IEnumerable<AABB> GetBombBlastAabbs(IReadOnlyList<int2> blastDirections, int blastRadius, fix2 blastPosition)
        {
            return blastDirections
                .Select(blastDirection =>
                {
                    var size = (fix2) (blastDirection * blastRadius) * LevelTiles.TileSize;
                    AABBExtensions.CreateFromPositionAndSize(blastPosition, size);
                    return Enumerable
                        .Range(1, blastRadius)
                        .Select(offset => blastPosition + blastDirection * offset)
                        .ToArray();
                })
                .Append(new[] { blastCoordinate });
        }*/
    }
}
