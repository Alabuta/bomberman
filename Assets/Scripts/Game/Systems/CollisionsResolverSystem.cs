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

        public void Run()
        {
            if (!_circleColliders.IsEmpty())
                foreach (var index in _circleColliders)
                    ResolveCollisions(_circleColliders, index);

            if (!_boxColliders.IsEmpty())
                foreach (var index in _boxColliders)
                    ResolveCollisions(_boxColliders, index);
        }

        private void ResolveCollisions<T1>(EcsFilter<TransformComponent, LayerMaskComponent, T1> filter, int index)
            where T1 : struct
        {
            var levelModel = _world.LevelModel;

            ref var transformComponentA = ref filter.Get1(index);
            var entityLayerMaskA = filter.Get2(index).Value;
            ref var colliderComponentA = ref filter.Get3(index);

            var entityPositionA = transformComponentA.WorldPosition;
            var entityCoordinateA = levelModel.ToTileCoordinate(entityPositionA);

            var neighborTiles = levelModel
                .GetNeighborTiles(entityCoordinateA)
                .Where(t => t.Has<LevelTileComponent>());

            foreach (var entityB in neighborTiles)
            {
                if ((entityLayerMaskA & entityB.GetCollidersInteractionMask()) == 0)
                    continue;

                ref var transformComponentB = ref entityB.Get<TransformComponent>();

                var hasIntersection = GetIntersection(colliderComponentA, entityPositionA,
                    entityB, transformComponentB.WorldPosition,
                    out var intersectionPoint);

                if (!hasIntersection)
                    continue;

                var entityA = _circleColliders.GetEntity(index);
                entityA.Replace(new OnCollisionEnterEventComponent
                {
                    CollidedEntity = entityB
                });

                entityB.Replace(new OnCollisionEnterEventComponent
                {
                    CollidedEntity = entityA
                });

                if (entityA.Has<PositionControlledBySystemsTag>())
                    continue;

                var travelledPath = transformComponentA.Speed / (fix) _world.TickRate;
                var prevPosition = entityPositionA - (fix2) transformComponentA.Direction * travelledPath;

                var prevDistance = fix2.distance(prevPosition, transformComponentB.WorldPosition);

                var colliderRadiusA = colliderComponentA switch
                {
                    CircleColliderComponent component => component.Radius,
                    QuadColliderComponent component => component.InnerRadius,
                    _ => fix.zero
                };

                var minDistance = colliderRadiusA;

                if (entityB.Has<CircleColliderComponent>())
                {
                    ref var colliderB = ref entityB.Get<CircleColliderComponent>();
                    minDistance += colliderB.Radius;
                }
                else if (entityB.Has<QuadColliderComponent>())
                {
                    ref var colliderB = ref entityB.Get<QuadColliderComponent>();
                    minDistance += colliderB.InnerRadius;
                }

                if (minDistance >= prevDistance)
                    continue;

                var vector = fix2.normalize_safe(entityPositionA - intersectionPoint, fix2.zero);
                transformComponentA.WorldPosition = intersectionPoint + vector * colliderRadiusA;
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
