using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Colliders;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems
{
    public class CollisionsResolverSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, LayerMaskComponent, CircleColliderComponent> _circleColliders;
        private readonly EcsFilter<TransformComponent, LayerMaskComponent, QuadColliderComponent> _boxColliders;

        private readonly HashSet<long> _processedPairs = new();
        private readonly HashSet<long> _collidedPairs = new();

        public void Run()
        {
            foreach (var index in _circleColliders)
                ResolveCollisions(_circleColliders, index);

                foreach (var entityIndex in _boxColliders)
                    ResolveCollisions(_boxColliders, entityIndex, stepIndex);

                _processedPairs.Clear();
            }

        private void ResolveCollisions<T>(EcsFilter<TransformComponent, LayerMaskComponent, T> filter, int index)
            where T : struct
        {
            var levelTiles = _world.LevelTiles;

            var entityA = _circleColliders.GetEntity(index);

            ref var transformComponentA = ref filter.Get1(index);
            var entityLayerMaskA = filter.Get2(index).Value;
            ref var colliderComponentA = ref filter.Get3(index);

            var entityPositionA = transformComponentA.WorldPosition;
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
                if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                    continue;

                var hashedPair = EcsExtensions.GetEntitiesPairHash(entityA, entityB);
                if (_processedPairs.Contains(hashedPair))
                    continue;

                _processedPairs.Add(hashedPair);

                ref var transformComponentB = ref entityB.Get<TransformComponent>();
                var entityPositionB = transformComponentB.WorldPosition;

                var hasIntersection = EcsExtensions.CheckEntitiesIntersection(colliderComponentA, entityPositionA,
                    entityB, entityPositionB, out var intersectionPoint);

                DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);

                if (!hasIntersection)
                {
                    _collidedPairs.Remove(hashedPair);
                    continue;
                }

                var isKinematicInteraction = entityA.Has<IsKinematicTag>() || entityB.Has<IsKinematicTag>();

                if (!transformComponentA.IsStatic && !isKinematicInteraction)
                {
                    transformComponentA.WorldPosition = PopOutEntity(entityA, entityB, entityPositionA,
                        entityPositionB, intersectionPoint);
                }

                if (!transformComponentB.IsStatic && !isKinematicInteraction)
                {
                    transformComponentB.WorldPosition = PopOutEntity(entityB, entityA, entityPositionB,
                        entityPositionA, intersectionPoint);
                }

                _collidedPairs.Add(hashedPair);
            }
        }

        private void DispatchCollisionEvents(EcsEntity entityA, EcsEntity entityB, long hashedPair, bool hasIntersection)
        {
            switch (hasIntersection)
            {
                case true when !_collidedPairs.Contains(hashedPair):
                {
                    ref var collisionEnterEventComponentA = ref entityA.Get<OnCollisionEnterEventComponent>();
                    if (collisionEnterEventComponentA.Entities != null)
                        collisionEnterEventComponentA.Entities.Add(entityB);
                    else
                        collisionEnterEventComponentA.Entities = new HashSet<EcsEntity> { entityB };

                    ref var collisionEnterEventComponentB = ref entityB.Get<OnCollisionEnterEventComponent>();
                    if (collisionEnterEventComponentB.Entities != null)
                        collisionEnterEventComponentB.Entities.Add(entityA);
                    else
                        collisionEnterEventComponentB.Entities = new HashSet<EcsEntity> { entityA };
                    break;
                }

                case false when _collidedPairs.Contains(hashedPair):
                {
                    ref var collisionExitEventComponentA = ref entityA.Get<OnCollisionExitEventComponent>();
                    if (collisionExitEventComponentA.Entities != null)
                        collisionExitEventComponentA.Entities.Add(entityB);
                    else
                        collisionExitEventComponentA.Entities = new HashSet<EcsEntity> { entityB };

                    ref var collisionExitEventComponentB = ref entityB.Get<OnCollisionExitEventComponent>();
                    if (collisionExitEventComponentB.Entities != null)
                        collisionExitEventComponentB.Entities.Add(entityA);
                    else
                        collisionExitEventComponentB.Entities = new HashSet<EcsEntity> { entityA };
                    break;
                }
            }
        }

        private static fix2 PopOutEntity(EcsEntity entityA, EcsEntity entityB, fix2 positionA, fix2 positionB, fix2 point)
        {
            if (entityA.Has<CircleColliderComponent>())
            {
                ref var colliderComponentA = ref entityA.Get<CircleColliderComponent>();

                if (entityB.Has<CircleColliderComponent>())
                {
                    ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

                    return CircleCirclePopOut(positionA, colliderComponentA.Radius,
                        positionB, colliderComponentB.Radius);
                }

                if (entityB.Has<QuadColliderComponent>())
                    return CircleQuadPopOut(positionA, colliderComponentA.Radius, point);
            }

            if (entityA.Has<QuadColliderComponent>())
            {
                if (entityB.Has<CircleColliderComponent>())
                {
                    ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();
                    return CircleQuadPopOut(positionA, colliderComponentB.Radius, point);
                }

                if (entityB.Has<QuadColliderComponent>())
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private static fix2 CircleCirclePopOut(fix2 positionA, fix radiusA, fix2 positionB, fix radiusB)
        {
            var vector = positionA - positionB;
            var distance = radiusA + radiusB - fix2.length(vector);

            return positionA + fix2.normalize_safe(vector, fix2.zero) * distance;
        }

        private static fix2 CircleQuadPopOut(fix2 positionA, fix radiusA, fix2 point)
        {
            var vector = fix2.normalize_safe(positionA - point, fix2.zero);
            return point + vector * radiusA;
        }
    }
}
