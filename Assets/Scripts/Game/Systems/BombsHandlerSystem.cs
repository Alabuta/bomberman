using System;
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
    public class BombsHandlerSystem : IEcsRunSystem
    {
        private static readonly fix2[] BlastDirections =
        {
            // :TODO: might be better to get it from a config
            new(1, 0),
            new(0, 1)
        };

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly Dictionary<PlayerTagConfig, Queue<EcsEntity>> _plantedBombsQueue = new();

        private readonly EcsFilter<OnBombPlantActionEventComponent> _bombPlantEvents;
        private readonly EcsFilter<OnBombBlastActionEventComponent> _bomBlastEvents;

        private readonly EcsFilter<DamageApplyEventComponent, TransformComponent, BombComponent> _attackingBombEvents;

        private readonly EcsFilter<BombComponent, TransformComponent, EntityComponent> _plantedBombs;

        public void Run()
        {
            ProcessPlantActions();
            ProcessBlastActions();
            ProcessPlantedTimeBombs();
            ProcessAttackEvents();
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

                    BlastBomb(transformComponent, in bombComponent);
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

                ref var transformComponent = ref _plantedBombs.Get2(index);
                ref var entityComponent = ref _plantedBombs.Get3(index);

                BlastBomb(transformComponent, in bombComponent);
            }
        }

        private void ProcessAttackEvents()
        {
            if (_attackingBombEvents.IsEmpty())
                return;

            foreach (var index in _attackingBombEvents)
            {
                var bombEntity = _attackingBombEvents.GetEntity(index);
                if (!bombEntity.IsAlive())
                    continue;

                ref var transformComponent = ref _attackingBombEvents.Get2(index);
                ref var bombComponent = ref _attackingBombEvents.Get3(index);

                BlastBomb(transformComponent, in bombComponent);
            }
        }

        private void BlastBomb(TransformComponent transformComponent, in BombComponent bombComponent)
        {
            var entries = ListPool<RTreeLeafEntry>.Get();
            var processedEntries = HashSetPool<int>.Get();

            var blastRadius = bombComponent.BlastRadius;

            var blastOrigin = transformComponent.WorldPosition;
            var radius = (fix) blastRadius;

            var blastRadiusInDirections = ListPool<int>.Get();
            var blastTileSize = new fix2(_world.LevelTiles.TileSize * new fix(.5f));

            foreach (var blastDirection in BlastDirections)
            {
                entries.Clear();

                var blastSize = blastDirection * radius;
                var startPoint = blastOrigin - blastSize;
                var endPoint = blastOrigin + blastSize;

                _world.EntitiesAabbTree.QueryByLine(startPoint, endPoint, entries);

                var min = startPoint;
                var max = endPoint;

                for (var i = 0; i < entries.Count; i++)
                {
                    var leafEntry = entries[i];

                    var entity = _world.EntitiesMap[leafEntry.Index];
                    if (!entity.Has<BombBlastStopEntityTag>())
                        continue;

                    var aabbCenter = leafEntry.Aabb.GetCenter();

                    if (math.any(aabbCenter < blastOrigin))
                    {
                        min = fix2.max(min, aabbCenter);
                        continue;
                    }

                    max = fix2.min(max, aabbCenter);
                }

                blastRadiusInDirections.Add((int) fix2.distance(blastOrigin, max));
                blastRadiusInDirections.Add((int) fix2.distance(blastOrigin, min));

                var size = blastDirection * blastTileSize;
                var blastAabb = new AABB(min + size, max + size);

                foreach (var entry in entries)
                {
                    if (!fix.is_AABB_overlapped_by_AABB(blastAabb, entry.Aabb))
                        continue;

                    var entryIndex = entry.Index;
                    if (processedEntries.Contains(entryIndex))
                        continue;

                    processedEntries.Add(entryIndex);

                    var targetEntity = _world.EntitiesMap[entryIndex];
                    if (!targetEntity.IsAlive())
                        continue;

                    targetEntity.Replace(new DamageApplyEventComponent(bombComponent.BlastDamage));
                }
            }

            // :TODO: refactor as an action apply operation
            _world.InstantiateBlastEffect(blastRadiusInDirections, blastRadius, blastOrigin, bombComponent.BlastEffect);

            // entityComponent.Controller.Kill(); // :TODO: refactor

            ListPool<int>.Release(blastRadiusInDirections);
            ListPool<RTreeLeafEntry>.Release(entries);
            HashSetPool<int>.Release(processedEntries);
        }
    }
}
