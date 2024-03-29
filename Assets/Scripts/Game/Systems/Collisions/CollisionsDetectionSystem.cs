﻿using System;
using System.Collections.Generic;
using App;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Events;
using Game.Components.Tags;
using Game.Systems.RTree;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Systems.Collisions
{
    public struct PrevFrameDataComponent
    {
        public fix2 LastWorldPosition;
    }

    public class CollisionsDetectionSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem
    {
        private const int InputEntriesStartCount = 256;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IRTree _entitiesAabbTree;
        private readonly List<EcsEntity> _entitiesMap;

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, HasColliderTag>
            _colliders; // :TODO: use AABBComponent

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent> _circleColliders;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, BoxColliderComponent> _boxColliders;

        private readonly EcsFilter<CollidersLinecastComponent> _lineCasters;

        private readonly HashSet<long> _processedPairs = new();
        private readonly HashSet<long> _collidedPairs = new();

        private readonly HashSet<int> _collidedEntities = new();

        private NativeList<RTreeLeafEntry> _treeLeafEntries;

        public void Init()
        {
            _treeLeafEntries = new NativeList<RTreeLeafEntry>(InputEntriesStartCount, Allocator.Persistent);
        }

        public void Run()
        {
            using var _ = Profiling.CollisionsDetection.Auto();

            Profiling.RTreeNativeArrayFill.Begin();

            var entitiesCount = _colliders.GetEntitiesCount();
            if (!_treeLeafEntries.IsCreated || _treeLeafEntries.Length < entitiesCount)
            {
                if (_treeLeafEntries.IsCreated)
                    _treeLeafEntries.Dispose();

                _treeLeafEntries = new NativeList<RTreeLeafEntry>(entitiesCount, Allocator.Persistent);
            }

            else
                _treeLeafEntries.Clear();

            _entitiesMap.Clear();
            if (entitiesCount > _entitiesMap.Capacity)
                _entitiesMap.Capacity = entitiesCount;

            foreach (var entityIndex in _colliders)
            {
                ref var entity = ref _colliders.GetEntity(entityIndex);
                ref var transformComponent = ref _colliders.Get1(entityIndex);

                var index = _entitiesMap.Count;
                _entitiesMap.Add(entity);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _treeLeafEntries.Add(new RTreeLeafEntry(aabb, index));
            }

            Profiling.RTreeNativeArrayFill.End();

            _entitiesAabbTree.Build(_treeLeafEntries.AsArray());

            foreach (var entityIndex in _circleColliders)
                DetectCollisions(_circleColliders, entityIndex);

            foreach (var entityIndex in _boxColliders)
                DetectCollisions(_boxColliders, entityIndex);

            _processedPairs.Clear();
            _collidedEntities.Clear();
        }

        public void Destroy()
        {
            _treeLeafEntries.Dispose();
        }

        private void DetectCollisions<T>(EcsFilter<TransformComponent, LayerMaskComponent, T> filter, int entityIndex)
            where T : struct
        {
            if (_collidedEntities.Contains(entityIndex))
                return;

            var entityA = filter.GetEntity(entityIndex);

            ref var transformComponentA = ref filter.Get1(entityIndex);
            var entityPositionA = transformComponentA.WorldPosition;

            var entityLayerMaskA = filter.Get2(entityIndex).Value;

            ref var colliderComponentA = ref filter.Get3(entityIndex);
            var interactionLayerMaskA = EcsExtensions.GetColliderInteractionLayerMask(in colliderComponentA);

            var aabbA = entityA.GetEntityColliderAABB(entityPositionA);

            using ( ListPool<RTreeLeafEntry>.Get(out var result) )
            {
                _entitiesAabbTree.QueryByAabb(aabbA, result);

                var hasIntersection = false;
                foreach (var entry in result)
                {
                    ref var entityB = ref _colliders.GetEntity(entry.Index);
                    if (entityA.Equals(entityB))
                        continue;

                    var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                    if (_processedPairs.Contains(hashedPair))
                        continue;

                    ref var transformComponentB = ref _colliders.Get1(entityIndex);
                    var aabbB = entityB.GetEntityColliderAABB(transformComponentB.WorldPosition);

                    var entityLayerMaskB = _colliders.Get2(entry.Index).Value;
                    var interactionLayerMaskB = entityB.GetColliderInteractionLayerMask();

                    if (!fix.is_AABB_overlapped_by_AABB(in aabbA, in aabbB))
                    {
                        DispatchCollisionEvents(
                            entityA,
                            entityB,
                            entityLayerMaskA,
                            entityLayerMaskB,
                            interactionLayerMaskA,
                            interactionLayerMaskB,
                            hashedPair,
                            hasIntersection);

                        _collidedPairs.Remove(hashedPair);

                        continue;
                    }

                    var entityPositionB = transformComponentB.WorldPosition;

                    hasIntersection = EcsExtensions.CheckEntitiesIntersection(
                        colliderComponentA,
                        entityPositionA,
                        entityB,
                        entityPositionB, out _);

                    DispatchCollisionEvents(
                        entityA,
                        entityB,
                        entityLayerMaskA,
                        entityLayerMaskB,
                        interactionLayerMaskA,
                        interactionLayerMaskB,
                        hashedPair,
                        hasIntersection);

                    if (hasIntersection)
                    {
                        _collidedPairs.Add(hashedPair);
                        _processedPairs.Add(hashedPair);
                    }
                    else
                        _collidedPairs.Remove(hashedPair);
                }

                if (hasIntersection)
                    _collidedEntities.Add(entityIndex);
            }
        }

        private void DispatchCollisionEvents(EcsEntity entityA,
            EcsEntity entityB,
            int entityLayerMaskA,
            int entityLayerMaskB,
            LayerMask interactionLayerMaskA,
            LayerMask interactionLayerMaskB,
            long hashedPair, // :TODO: use register of collisions
            bool hasIntersection)
        {
            switch (hasIntersection)
            {
                case true when _collidedPairs.Contains(hashedPair):
                {
                    if ((interactionLayerMaskA & entityLayerMaskB) != 0)
                        UpdateCollisionStayEventComponent(entityA, entityB);

                    if ((interactionLayerMaskB & entityLayerMaskA) != 0)
                        UpdateCollisionStayEventComponent(entityB, entityA);

                    break;
                }

                case true when !_collidedPairs.Contains(hashedPair):
                {
                    if ((interactionLayerMaskA & entityLayerMaskB) != 0)
                        UpdateCollisionEnterEventComponent(entityA, entityB);

                    if ((interactionLayerMaskB & entityLayerMaskA) != 0)
                        UpdateCollisionEnterEventComponent(entityB, entityA);

                    break;
                }

                case false when _collidedPairs.Contains(hashedPair):
                {
                    if ((interactionLayerMaskA & entityLayerMaskB) != 0)
                        UpdateCollisionExitEventComponent(entityA, entityB);

                    if ((interactionLayerMaskB & entityLayerMaskA) != 0)
                        UpdateCollisionExitEventComponent(entityB, entityA);

                    break;
                }
            }
        }

        private static void UpdateCollisionEnterEventComponent(EcsEntity entityA, EcsEntity entityB)
        {
            if (entityA.Has<CollisionEnterEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<CollisionEnterEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new CollisionEnterEventComponent(new HashSet<EcsEntity> { entityB }));
        }

        private static void UpdateCollisionExitEventComponent(EcsEntity entityA, EcsEntity entityB)
        {
            if (entityA.Has<CollisionExitEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<CollisionExitEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new CollisionExitEventComponent(new HashSet<EcsEntity> { entityB }));
        }

        private static void UpdateCollisionStayEventComponent(EcsEntity entityA, EcsEntity entityB)
        {
            if (entityA.Has<CollisionStayEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<CollisionStayEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new CollisionStayEventComponent(new HashSet<EcsEntity> { entityB }));
        }
    }
}
