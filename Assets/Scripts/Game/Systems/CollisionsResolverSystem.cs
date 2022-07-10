﻿using System.Collections.Generic;
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
            if (!_circleColliders.IsEmpty())
                foreach (var index in _circleColliders)
                    ResolveCollisions(_circleColliders, index);

            if (!_boxColliders.IsEmpty())
                foreach (var index in _boxColliders)
                    ResolveCollisions(_boxColliders, index);

            _processedPairs.Clear();
        }

        private void ResolveCollisions<T1>(EcsFilter<TransformComponent, LayerMaskComponent, T1> filter, int index)
            where T1 : struct
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
                .Where(t => t.Has<LevelTileComponent>())
                .SelectMany(t =>
                {
                    ref var levelTileComponent = ref t.Get<LevelTileComponent>();
                    return levelTileComponent.Entities != null ? levelTileComponent.Entities.Append(t) : new[] { t };
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

                var hasIntersection = GetIntersection(colliderComponentA, entityPositionA,
                    entityB, transformComponentB.WorldPosition,
                    out var intersectionPoint);

                DispatchCollisionEvents(entityA, entityB, hashedPair, hasIntersection);

                if (!hasIntersection)
                {
                    _collidedPairs.Remove(hashedPair);
                    continue;
                }

                /*var wasCollided = _collidedPairs.Contains(hashedPair);
                if (wasCollided)
                    continue;*/

                if (!transformComponentA.IsStatic && !entityA.Has<IsKinematicTag>())
                {
                    PopOutEntity(ref transformComponentA, ref colliderComponentA, intersectionPoint);
                }

                if (!transformComponentB.IsStatic && !entityB.Has<IsKinematicTag>())
                {
                    if (entityB.Has<CircleColliderComponent>())
                    {
                        ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();
                        PopOutEntity(ref transformComponentB, ref colliderComponentB, intersectionPoint);
                    }
                    else if (entityB.Has<QuadColliderComponent>())
                    {
                        ref var colliderComponentB = ref entityB.Get<QuadColliderComponent>();
                        PopOutEntity(ref transformComponentB, ref colliderComponentB, intersectionPoint);
                    }
                }

                _collidedPairs.Add(hashedPair);
            }
        }

        private void PopOutEntity<T>(ref TransformComponent transform, ref T collider, fix2 intersectionPoint) where T : struct
        {
            var colliderRadiusA = collider switch
            {
                CircleColliderComponent component => component.Radius,
                QuadColliderComponent component => component.InnerRadius,
                _ => fix.zero
            };

            var vector = fix2.normalize_safe(transform.WorldPosition - intersectionPoint, fix2.zero);
            transform.WorldPosition = intersectionPoint + vector * colliderRadiusA;
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

        private static bool GetIntersection<T1>(T1 colliderComponentA, fix2 entityPositionA,
            EcsEntity entityB, fix2 entityPositionB, out fix2 intersectionPoint) where T1 : struct
        {
            var hasIntersection = false;
            intersectionPoint = default;

            if (entityB.Has<CircleColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    CircleColliderComponent circleColliderComponentA => CircleCircleIntersectionPoint(entityPositionA,
                        ref circleColliderComponentA, entityPositionB, ref colliderComponentB,
                        out intersectionPoint),

                    QuadColliderComponent boxColliderComponentA => CircleBoxIntersectionPoint(
                        entityPositionB, ref colliderComponentB, entityPositionA,
                        ref boxColliderComponentA, out intersectionPoint),

                    _ => false
                };
            }
            else if (entityB.Has<QuadColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<QuadColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    CircleColliderComponent circleColliderComponentA => CircleBoxIntersectionPoint(entityPositionA,
                        ref circleColliderComponentA, entityPositionB, ref colliderComponentB,
                        out intersectionPoint),

                    QuadColliderComponent boxColliderComponentA => BoxBoxIntersectionPoint(entityPositionB,
                        ref colliderComponentB, entityPositionA, ref boxColliderComponentA, out intersectionPoint),

                    _ => false
                };
            }

            return hasIntersection;
        }

        private static bool CircleCircleIntersectionPoint(fix2 positionA, ref CircleColliderComponent colliderA,
            fix2 positionB, ref CircleColliderComponent colliderB, out fix2 intersection)
        {
            return fix.circle_and_circle_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.Radius,
                out intersection);
        }

        private static bool BoxBoxIntersectionPoint(fix2 positionA, ref QuadColliderComponent colliderA,
            fix2 positionB, ref QuadColliderComponent colliderB, out fix2 intersection)
        {
            throw new System.NotImplementedException(); // :TODO: implement

            /*return fix.box_and_box_intersection_point(centerA, colliderA.InnerRadius, centerB, boxCollider.InnerRadius,
                out intersection);*/
        }

        private static bool CircleBoxIntersectionPoint(fix2 positionA, ref CircleColliderComponent colliderA,
            fix2 positionB, ref QuadColliderComponent colliderB, out fix2 intersection)
        {
            return fix.circle_and_box_intersection_point(
                positionA, colliderA.Radius,
                positionB, colliderB.InnerRadius,
                out intersection);
        }
    }
}
