using System.Collections.Generic;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Pool;

namespace Game.Systems
{
    public struct PrevFrameDataComponent
    {
        public fix2 LastWorldPosition;
    }

    public class CollisionsDetectionSystem : IEcsRunSystem
    {
        private const int IterationsCount = 1;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EntitiesAabbTree _entitiesAabbTree;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders; // :TODO: use AABBComponent

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent> _circleColliders;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, BoxColliderComponent> _boxColliders;

        private readonly EcsFilter<CollidersLinecastComponent> _lineCasters;

        private readonly HashSet<long> _processedPairs = new();
        private readonly HashSet<long> _collidedPairs = new();

        private readonly HashSet<int> _collidedEntities = new();

        public void Run()
        {
            for (var iterationIndex = 0; iterationIndex < IterationsCount; iterationIndex++)
            {
                var simulationSubStep = (fix) (iterationIndex + 1) / (fix) IterationsCount;

                _entitiesAabbTree.Build(_colliders, simulationSubStep);

                foreach (var entityIndex in _circleColliders)
                    DetectCollisions(simulationSubStep, _circleColliders, entityIndex);

                foreach (var entityIndex in _boxColliders)
                    DetectCollisions(simulationSubStep, _boxColliders, entityIndex);

                /*foreach (var entityIndex in _lineCasters)
                    TraceLine(simulationSubStep, filter: _lineCasters, entityIndex);*/

                _processedPairs.Clear();
            }

            _collidedEntities.Clear();
        }

        private void TraceLine(fix simulationSubStep, EcsFilter<CollidersLinecastComponent> filter, int entityIndex)
        {
            var entityA = filter.GetEntity(entityIndex);

            var linecastComponent = filter.Get1(entityIndex);

            using ( ListPool<(AABB Aabb, EcsEntity Entity)>.Get(out var result) )
            {
                _entitiesAabbTree.QueryByLine(linecastComponent.Start, linecastComponent.End, result);

                /*result.Sort((a, b) =>
                {
                    return 0;
                });*/

                foreach (var (aabb, entity) in result)
                {
                }
            }
        }

        private void DetectCollisions<T>(fix simulationSubStep, EcsFilter<TransformComponent, LayerMaskComponent, T> filter,
            int entityIndex)
            where T : struct
        {
            if (_collidedEntities.Contains(entityIndex))
                return;

            var entityA = filter.GetEntity(entityIndex);

            ref var transformComponentA = ref filter.Get1(entityIndex);
            var entityLayerMaskA = filter.Get2(entityIndex).Value;
            ref var colliderComponentA = ref filter.Get3(entityIndex);

            var hasPrevFrameDataComponentA = entityA.Has<PrevFrameDataComponent>();
            ref var prevFrameDataComponentA = ref entityA.Get<PrevFrameDataComponent>();

            if (!hasPrevFrameDataComponentA)
                prevFrameDataComponentA.LastWorldPosition = transformComponentA.WorldPosition;

            var entityLastPositionA = prevFrameDataComponentA.LastWorldPosition;
            var entityCurrentPositionA = transformComponentA.WorldPosition;
            var entityPositionA = entityLastPositionA + (entityCurrentPositionA - entityLastPositionA) * simulationSubStep;

            var aabbA = entityA.GetEntityColliderAABB(entityPositionA);

            using ( ListPool<(AABB Aabb, EcsEntity Entity)>.Get(out var result) )
            {
                _entitiesAabbTree.QueryByAabb(aabbA, result);

                var hasIntersection = false;
                foreach (var (aabbB, entityB) in result)
                {
                    if (entityA.Equals(entityB))
                        continue;

                    if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                        continue;

                    var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                    if (_processedPairs.Contains(hashedPair))
                        continue;

                    if (!fix.is_AABB_overlapped_by_AABB(aabbA, aabbB))
                    {
                        var isFinalSubStep = simulationSubStep == fix.one;
                        if (isFinalSubStep)
                        {
                            DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);
                            _collidedPairs.Remove(hashedPair);
                        }

                        continue;
                    }

                    ref var transformComponentB = ref entityB.Get<TransformComponent>();

                    var hasPrevFrameDataComponentB = entityB.Has<PrevFrameDataComponent>();
                    ref var prevFrameDataComponentB = ref entityB.Get<PrevFrameDataComponent>();

                    if (!hasPrevFrameDataComponentB)
                        prevFrameDataComponentB.LastWorldPosition = transformComponentB.WorldPosition;

                    var entityLastPositionB = prevFrameDataComponentB.LastWorldPosition;
                    var entityCurrentPositionB = transformComponentB.WorldPosition;

                    var positionIterationsStepB = (entityCurrentPositionB - entityLastPositionB) * simulationSubStep;
                    var entityPositionB = entityLastPositionB + positionIterationsStepB;

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
                    ref var eventComponentA = ref entityA.Get<OnCollisionStayEventComponent>();
                    if (eventComponentA.Entities != null)
                        eventComponentA.Entities.Add(entityB);
                    else
                        eventComponentA.Entities = new HashSet<EcsEntity> { entityB };

                    ref var eventComponentB = ref entityB.Get<OnCollisionStayEventComponent>();
                    if (eventComponentB.Entities != null)
                        eventComponentB.Entities.Add(entityA);
                    else
                        eventComponentB.Entities = new HashSet<EcsEntity> { entityA };

                    break;
                }

                case true when !_collidedPairs.Contains(hashedPair):
                {
                    ref var eventComponentA = ref entityA.Get<OnCollisionEnterEventComponent>();
                    if (eventComponentA.Entities != null)
                        eventComponentA.Entities.Add(entityB);
                    else
                        eventComponentA.Entities = new HashSet<EcsEntity> { entityB };

                    ref var eventComponentB = ref entityB.Get<OnCollisionEnterEventComponent>();
                    if (eventComponentB.Entities != null)
                        eventComponentB.Entities.Add(entityA);
                    else
                        eventComponentB.Entities = new HashSet<EcsEntity> { entityA };

                    break;
                }

                case false when _collidedPairs.Contains(hashedPair):
                {
                    ref var eventComponentA = ref entityA.Get<OnCollisionExitEventComponent>();
                    if (eventComponentA.Entities != null)
                        eventComponentA.Entities.Add(entityB);
                    else
                        eventComponentA.Entities = new HashSet<EcsEntity> { entityB };

                    ref var eventComponentB = ref entityB.Get<OnCollisionExitEventComponent>();
                    if (eventComponentB.Entities != null)
                        eventComponentB.Entities.Add(entityA);
                    else
                        eventComponentB.Entities = new HashSet<EcsEntity> { entityA };

                    break;
                }
            }
        }
    }
}
