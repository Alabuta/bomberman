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

        private readonly Dictionary<PlayerTagConfig, Queue<BombData>> _plantedRemoteBombs = new();

        private readonly EcsFilter<OnBombPlantActionEventComponent> _bombPlantEvents;
        private readonly EcsFilter<OnBombBlastActionEventComponent> _bomBlastEvents;

        private readonly EcsFilter<TimeBomb, TransformComponent, EntityComponent> _plantedTimeBombs;

        public void Run()
        {
            ProcessPlantActions();
            ProcessBlastActions();
            ProcessPlantedTimeBombs();
        }

        private void ProcessPlantedTimeBombs()
        {
            if (_plantedTimeBombs.IsEmpty())
                return;

            foreach (var index in _plantedTimeBombs)
            {
                ref var timeBomb = ref _plantedTimeBombs.Get1(index);

                var blastWorldTick = timeBomb.BlastWorldTick;
                if (blastWorldTick > _world.Tick)
                    continue;

                ref var transformComponent = ref _plantedTimeBombs.Get2(index);
                ref var entityComponent = ref _plantedTimeBombs.Get3(index);

                BlastBomb(transformComponent, in entityComponent, timeBomb.BlastRadius, timeBomb.BlastDamage);
            }
        }

        private void ProcessPlantActions()
        {
            if (_bombPlantEvents.IsEmpty())
                return;

            using var pool = ListPool<Task<EcsEntity>>.Get(out var tasks);

            foreach (var index in _bombPlantEvents)
            {
                ref var eventComponent = ref _bombPlantEvents.Get1(index);

                var task = _world.CreateAndSpawnBomb(
                    eventComponent.Position,
                    eventComponent.BombConfig,
                    eventComponent.BlastDelay,
                    eventComponent.BombBlastDamage,
                    eventComponent.BombBlastRadius
                );
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

                if (!_plantedRemoteBombs.TryGetValue(eventComponent.PlayerTag, out var bombsQueue))
                    continue;

                if (!bombsQueue.TryDequeue(out var bombData))
                    continue;

                var bombEntity = bombData.BombEntity;
                Assert.IsTrue(bombEntity.Has<BombTag>());
                Assert.IsTrue(bombEntity.Has<EntityComponent>());
                Assert.IsTrue(bombEntity.Has<TransformComponent>());

                ref var transformComponent = ref bombEntity.Get<TransformComponent>();
                ref var entityComponent = ref bombEntity.Get<EntityComponent>();

                BlastBomb(transformComponent, in entityComponent, bombData.BombBlastRadius, bombData.BombBlastDamage);
            }
        }

        private void BlastBomb(TransformComponent transformComponent, in EntityComponent entityComponent, int blastRadius,
            fix blastDamage)
        {
            var startPoint = transformComponent.WorldPosition;

            using var pool = ListPool<RTreeLeafEntry>.Get(out var entries);

            foreach (var blastDirection in BlastDirections)
            {
                entries.Clear();

                var blastSize = blastDirection * blastRadius;
                var endPoint = startPoint + (fix2) blastSize;

                _world.EntitiesAabbTree.QueryByLine(startPoint, endPoint, entries);
                Debug.LogWarning($"direction {blastDirection} {entries.Count}");
            }

            entityComponent.Controller.Kill();
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
