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
using UnityEngine.Pool;

namespace Game.Systems.Collisions
{
    public struct PrevFrameDataComponent
    {
        public fix2 LastWorldPosition;
    }

    /*public class CollidersAabbUpdateSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders;

        public void Init()
        {
        }

        public void Run()
        {
            using var _ = Profiling.CollidersAabbsUpdate.Auto();

            foreach (var index in _colliders)
            {
                ref var entity = ref _colliders.GetEntity(index);
                ref var transformComponent = ref _colliders.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                entity.Replace(new AabbComponent(aabb, index));
            }
        }

        public void Destroy()
        {
        }
    }*/

    public class CollisionsDetectionSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem
    {
        private const int InputEntriesStartCount = 256;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IRTree _entitiesAabbTree;
        private readonly List<EcsEntity> _entitiesMap;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders; // :TODO: use AABBComponent

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
            var entityLayerMaskA = filter.Get2(entityIndex).Value;
            ref var colliderComponentA = ref filter.Get3(entityIndex);

            var entityPositionA = transformComponentA.WorldPosition;

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

                    if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                        continue;

                    var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                    if (_processedPairs.Contains(hashedPair))
                        continue;

                    ref var transformComponentB = ref entityB.Get<TransformComponent>();
                    var aabbB = entityB.GetEntityColliderAABB(transformComponentB.WorldPosition);

                    if (!fix.is_AABB_overlapped_by_AABB(in aabbA, in aabbB))
                    {
                        DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);
                        _collidedPairs.Remove(hashedPair);

                        continue;
                    }

                    var entityPositionB = transformComponentB.WorldPosition;

                    hasIntersection = EcsExtensions.CheckEntitiesIntersection(colliderComponentA, entityPositionA,
                        entityB, entityPositionB, out _);

                    DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);

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

        private void DispatchCollisionEvents(EcsEntity entityA, EcsEntity entityB, long hashedPair, bool hasIntersection)
        {
            switch (hasIntersection)
            {
                case true when _collidedPairs.Contains(hashedPair):
                {
                    UpdateCollisionStayEventComponent(entityA, entityB);
                    UpdateCollisionStayEventComponent(entityB, entityA);
                    break;
                }

                case true when !_collidedPairs.Contains(hashedPair):
                {
                    UpdateCollisionEnterEventComponent(entityA, entityB);
                    UpdateCollisionEnterEventComponent(entityB, entityA);
                    break;
                }

                case false when _collidedPairs.Contains(hashedPair):
                {
                    UpdateCollisionExitEventComponent(entityA, entityB);
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
