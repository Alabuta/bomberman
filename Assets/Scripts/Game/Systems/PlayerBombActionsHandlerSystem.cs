using System.Collections.Generic;
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
    internal readonly struct BombData
    {
        public readonly PlayerTagConfig PlayerTag;
        public readonly EcsEntity BombEntity;
        public readonly fix BombBlastDamage;
        public readonly int BombBlastRadius;

        public BombData(PlayerTagConfig playerTag, EcsEntity bombEntity, fix bombBlastDamage, int bombBlastRadius)
        {
            PlayerTag = playerTag;
            BombEntity = bombEntity;
            BombBlastDamage = bombBlastDamage;
            BombBlastRadius = bombBlastRadius;
        }
    }

    public class PlayerBombActionsHandlerSystem : IEcsRunSystem
    {
        private static readonly int2[] BlastDirections =
        {
            // :TODO: might be useful to get it from a config
            new(1, 0),
            new(0, 1),
            new(-1, 0),
            new(0, -1)
        };

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly SortedDictionary<fix, List<BombData>> _plantedTimeBombs = new();
        private readonly Dictionary<PlayerTagConfig, Queue<BombData>> _plantedRemoteBombs = new();

        private readonly EcsFilter<OnBombPlantActionComponent> _bombPlantActions;
        private readonly EcsFilter<OnBombBlastActionComponent> _bomBlastActions;

        public void Run()
        {
            ProcessPlantActions();
            ProcessBlastActions();
        }

        private void ProcessPlantActions()
        {
            if (_bombPlantActions.IsEmpty())
                return;

            using var pool = ListPool<Task<EcsEntity>>.Get(out var tasks);

            foreach (var index in _bombPlantActions)
            {
                ref var eventComponent = ref _bombPlantActions.Get1(index);

                var task = _world.CreateAndSpawnBomb(eventComponent.BombConfig,
                    eventComponent.Position); // :TODO: use object pools
                tasks.Add(task);
            }

            Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var bombEntity = task.Result;

                var playerTag = eventComponent.PlayerTag;

                var blastDelay = eventComponent.BlastDelay * (fix) _world.TickRate;
                var blastWorldTick = blastDelay + (fix) _world.Tick;

                if (!_plantedTimeBombs.TryGetValue(blastWorldTick, out var queue))
                {
                    queue = new List<BombData>();
                    _plantedTimeBombs.Add(blastWorldTick, queue);
                }

                queue.Add(new BombData(
                    playerTag: playerTag,
                    BombEntity: bombEntity,
                    bombBlastDamage: eventComponent.BombBlastDamage,
                    bombBlastRadius: eventComponent.BombBlastRadius
                ));
            }
        }

        private void ProcessBlastActions()
        {
            if (_bomBlastActions.IsEmpty())
                return;

            using var pool = ListPool<RTreeLeafEntry>.Get(out var entries);

            foreach (var index in _bomBlastActions)
            {
                ref var eventComponent = ref _bomBlastActions.Get1(index);

                var playerTag = eventComponent.PlayerTag;
                if (!_plantedRemoteBombs.TryGetValue(playerTag, out var bombsQueue))
                    return;

                if (!bombsQueue.TryDequeue(out var bombData))
                    return;

                var bombEntity = bombData.BombEntity;

                Assert.IsTrue(bombEntity.Has<BombTag>());
                Assert.IsTrue(bombEntity.Has<EntityComponent>());
                Assert.IsTrue(bombEntity.Has<TransformComponent>());

                ref var transformComponent = ref bombEntity.Get<TransformComponent>();
                var startPoint = transformComponent.WorldPosition;

                foreach (var blastDirection in BlastDirections)
                {
                    entries.Clear();

                    var blastSize = blastDirection * bombData.BombBlastRadius;
                    var endPoint = startPoint + (fix2) blastSize;

                    _world.EntitiesAabbTree.QueryByLine(startPoint, endPoint, entries);
                    Debug.LogWarning($"direction {blastDirection} {entries.Count}");
                }

                ref var entityComponent = ref bombEntity.Get<EntityComponent>();
                entityComponent.Controller.Kill();

                // bombEntity.Destroy(); :TODO: destroy right here
            }
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
