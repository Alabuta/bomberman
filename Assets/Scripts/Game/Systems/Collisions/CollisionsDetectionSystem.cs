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

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders; // :TODO: use AABBComponent

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent> _circleColliders;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, BoxColliderComponent> _boxColliders;

        private readonly EcsFilter<CollidersLinecastComponent> _lineCasters;

        private readonly HashSet<long> _processedPairs = new();
        private readonly HashSet<long> _collidedPairs = new();

        private readonly HashSet<int> _collidedEntities = new();

        private NativeList<RTreeLeafEntry> _entries;

        public void Init()
        {
            _entries = new NativeList<RTreeLeafEntry>(InputEntriesStartCount, Allocator.Persistent);
        }

        public void Run()
        {
            using var _ = Profiling.CollisionsDetection.Auto();

            Profiling.RTreeNativeArrayFill.Begin();

            var entitiesCount = _colliders.GetEntitiesCount();
            if (!_entries.IsCreated || _entries.Length < entitiesCount)
            {
                if (_entries.IsCreated)
                    _entries.Dispose();

                _entries = new NativeList<RTreeLeafEntry>(entitiesCount, Allocator.Persistent);
            }

            else
                _entries.Clear();

            foreach (var index in _colliders)
            {
                ref var entity = ref _colliders.GetEntity(index);
                ref var transformComponent = ref _colliders.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _entries.Add(new RTreeLeafEntry(aabb, index));
            }

            Profiling.RTreeNativeArrayFill.End();

            _entitiesAabbTree.Build(_entries.AsArray());

            foreach (var entityIndex in _circleColliders)
                DetectCollisions(_circleColliders, entityIndex);

            foreach (var entityIndex in _boxColliders)
                DetectCollisions(_boxColliders, entityIndex);

            _processedPairs.Clear();
            _collidedEntities.Clear();
        }

        public void Destroy()
        {
            _entries.Dispose();
        }

        private void DetectCollisions<T>(EcsFilter<TransformComponent, LayerMaskComponent, T> filter,
            int entityIndex)
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
            if (entityA.Has<OnCollisionEnterEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<OnCollisionEnterEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new OnCollisionEnterEventComponent(new HashSet<EcsEntity> { entityB }));
        }

        private static void UpdateCollisionExitEventComponent(EcsEntity entityA, EcsEntity entityB)
        {
            if (entityA.Has<OnCollisionExitEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<OnCollisionExitEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new OnCollisionExitEventComponent(new HashSet<EcsEntity> { entityB }));
        }

        private static void UpdateCollisionStayEventComponent(EcsEntity entityA, EcsEntity entityB)
        {
            if (entityA.Has<OnCollisionStayEventComponent>())
            {
                ref var eventComponent = ref entityA.Get<OnCollisionStayEventComponent>();
                eventComponent.Entities.Add(entityB);
            }
            else
                entityA.Replace(new OnCollisionStayEventComponent(new HashSet<EcsEntity> { entityB }));
        }
    }
}
