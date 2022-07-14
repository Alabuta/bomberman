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

            foreach (var index in _boxColliders)
                ResolveCollisions(_boxColliders, index);

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

                var hasIntersection = CheckIntersection(colliderComponentA, entityPositionA,
                    entityB, transformComponentB.WorldPosition,
                    out var intersectionPoint);

                DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);

                if (!hasIntersection)
                {
                    _collidedPairs.Remove(hashedPair);
                    continue;
                }

                var isKinematicInteraction = entityA.Has<IsKinematicTag>() || entityB.Has<IsKinematicTag>();

                if (!transformComponentA.IsStatic && !isKinematicInteraction)
                    PopOutEntity(entityA, entityB, ref transformComponentA, intersectionPoint);

                if (!transformComponentB.IsStatic && !isKinematicInteraction)
                    PopOutEntity(entityB, entityA, ref transformComponentB, intersectionPoint);

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

        private static void PopOutEntity(EcsEntity entityA, EcsEntity entityB, ref TransformComponent transformComponentA,
            fix2 point)
        {
            if (entityA.Has<CircleColliderComponent>())
            {
                ref var colliderComponentA = ref entityA.Get<CircleColliderComponent>();

                if (entityB.Has<CircleColliderComponent>())
                    CircleCirclePopOut(ref transformComponentA, ref colliderComponentA, entityB);
                else if (entityB.Has<QuadColliderComponent>())
                    CircleQuadPopOut(ref transformComponentA, ref colliderComponentA, point);
                else
                    throw new NotImplementedException();
            }
            else if (entityA.Has<QuadColliderComponent>())
            {
                if (entityA.Has<CircleColliderComponent>())
                {
                    ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();
                    CircleQuadPopOut(ref transformComponentA, ref colliderComponentB, point);
                }
                else if (entityB.Has<CircleColliderComponent>())
                    throw new NotImplementedException();
                else
                    throw new NotImplementedException();
            }
            else
                throw new NotImplementedException();
        }

        private static void CircleCirclePopOut(ref TransformComponent transformComponentA,
            ref CircleColliderComponent colliderComponentA, EcsEntity entityB)
        {
            ref var transformComponentB = ref entityB.Get<TransformComponent>();
            ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

            var vector = transformComponentA.WorldPosition - transformComponentB.WorldPosition;
            var distance = colliderComponentA.Radius + colliderComponentB.Radius - fix2.length(vector);

            vector = fix2.normalize_safe(vector, fix2.zero);
            transformComponentA.WorldPosition += vector * distance;
        }

        private static void CircleQuadPopOut(ref TransformComponent transformComponentA,
            ref CircleColliderComponent colliderComponentA, fix2 point)
        {
            var vector = fix2.normalize_safe(transformComponentA.WorldPosition - point, fix2.zero);
            transformComponentA.WorldPosition = point + vector * colliderComponentA.Radius;
        }

        private static bool CheckIntersection<T>(T colliderComponentA, fix2 entityPositionA,
            EcsEntity entityB, fix2 entityPositionB, out fix2 intersectionPoint) where T : struct
        {
            var hasIntersection = false;
            intersectionPoint = default;

            if (entityB.Has<CircleColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    /*CircleColliderComponent circleColliderComponentA =>
                        CircleCircleIntersectionPoint(entityPositionA, ref circleColliderComponentA,
                            entityPositionB, ref colliderComponentB,
                            out intersectionPoint),*/
                    CircleColliderComponent circleColliderComponentA =>
                        fix.circle_and_circle_intersection(
                            entityPositionA, circleColliderComponentA.Radius,
                            entityPositionB, colliderComponentB.Radius,
                            out intersectionPoint),

                    /*QuadColliderComponent boxColliderComponentA => CircleBoxIntersectionPoint(
                        entityPositionB, ref colliderComponentB, entityPositionA,
                        ref boxColliderComponentA, out intersectionPoint),*/
                    QuadColliderComponent boxColliderComponentA =>
                        fix.circle_and_quad_intersection_point(
                            entityPositionB, colliderComponentB.Radius,
                            entityPositionA, boxColliderComponentA.Size,
                            out intersectionPoint),

                    _ => false
                };
            }
            else if (entityB.Has<QuadColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<QuadColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    /*CircleColliderComponent circleColliderComponentA => CircleBoxIntersectionPoint(entityPositionA,
                        ref circleColliderComponentA, entityPositionB, ref colliderComponentB,
                        out intersectionPoint),*/
                    CircleColliderComponent circleColliderComponentA =>
                        fix.circle_and_quad_intersection_point(
                            entityPositionA, circleColliderComponentA.Radius,
                            entityPositionB, colliderComponentB.Size,
                            out intersectionPoint),

                    QuadColliderComponent _ =>
                        throw new NotImplementedException(), // :TODO: implement

                    _ => false
                };
            }

            return hasIntersection;
        }
    }
}
