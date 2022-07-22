using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Events;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems
{
    public struct PrevFrameDataComponent
    {
        public fix2 LastWorldPosition;
    }

    public class CollisionsDetectionSystem : IEcsRunSystem
    {
        private const int IterationsCount = 8;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent> _circleColliders;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, BoxColliderComponent> _boxColliders;

        private readonly HashSet<long> _processedPairs = new();
        private readonly HashSet<long> _collidedPairs = new();

        public void Run()
        {
            foreach (var entityIndex in _circleColliders)
                DetectCollisions(_circleColliders, entityIndex);

            foreach (var entityIndex in _boxColliders)
                DetectCollisions(_boxColliders, entityIndex);

            _processedPairs.Clear();
        }

        private void DetectCollisions<T>(EcsFilter<TransformComponent, LayerMaskComponent, T> filter, int entityIndex)
            where T : struct
        {
            var levelTiles = _world.LevelTiles;

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

            var positionIterationsStepA = (entityCurrentPositionA - entityLastPositionA) / (fix) IterationsCount;

            for (var stepIndex = 0; stepIndex < IterationsCount; stepIndex++)
            {
                var entityPositionA = entityLastPositionA + positionIterationsStepA * (fix) (stepIndex + 1);
                var entityCoordinateA = levelTiles.ToTileCoordinate(entityPositionA);

                var neighborTiles = levelTiles
                    .GetNeighborTiles(entityCoordinateA)
                    .SelectMany(t =>
                    {
                        ref var levelTileComponent = ref t.Get<LevelTileComponent>();
                        return levelTileComponent.EntitiesHolder != null
                            ? levelTileComponent.EntitiesHolder.Append(t)
                            : new[] { t };
                    });

                var hasIntersection = false;

                foreach (var entityB in neighborTiles)
                {
                    if (entityA.Equals(entityB))
                        continue;

                    if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                        continue;

                    var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                    if (_processedPairs.Contains(hashedPair))
                        continue;

                    ref var transformComponentB = ref entityB.Get<TransformComponent>();
                    // var entityPositionB = transformComponentB.WorldPosition;

                    var hasPrevFrameDataComponentB = entityB.Has<PrevFrameDataComponent>();
                    ref var prevFrameDataComponentB = ref entityB.Get<PrevFrameDataComponent>();

                    if (!hasPrevFrameDataComponentB)
                        prevFrameDataComponentB.LastWorldPosition = transformComponentB.WorldPosition;

                    var entityLastPositionB = prevFrameDataComponentB.LastWorldPosition;
                    var entityCurrentPositionB = transformComponentB.WorldPosition;

                    var positionIterationsStepB =
                        fix2.length(entityCurrentPositionB - entityLastPositionB) / (fix) IterationsCount;
                    var entityPositionB = entityLastPositionB + positionIterationsStepB * (fix) stepIndex;

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
                    break;
            }

            /*var entityPositionA = transformComponentA.WorldPosition;
            var entityCoordinateA = levelTiles.ToTileCoordinate(entityPositionA);

            var neighborTiles = levelTiles
                .GetNeighborTiles(entityCoordinateA)
                .SelectMany(t =>
                {
                    ref var levelTileComponent = ref t.Get<LevelTileComponent>();
                    return levelTileComponent.EntitiesHolder != null
                        ? levelTileComponent.EntitiesHolder.Append(t)
                        : new[] { t };
                });

            foreach (var entityB in neighborTiles)
            {
                if (entityA.Equals(entityB))
                    continue;

                if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                    continue;

                var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                if (_processedPairs.Contains(hashedPair))
                    continue;

                _processedPairs.Add(hashedPair);

                ref var transformComponentB = ref entityB.Get<TransformComponent>();
                var entityPositionB = transformComponentB.WorldPosition;

                var hasIntersection = EcsExtensions.CheckEntitiesIntersection(colliderComponentA, entityPositionA,
                    entityB, entityPositionB, out _);

                DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);

                if (hasIntersection)
                    _collidedPairs.Add(hashedPair);

                else
                    _collidedPairs.Remove(hashedPair);
            }*/
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
