using System;
using System.Collections.Generic;
using Game.Components;
using Game.Components.Colliders;
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

        private readonly EcsFilter<TransformComponent, OnCollisionEnterEventComponent, CircleColliderComponent> _circleEnters;
        private readonly EcsFilter<TransformComponent, OnCollisionEnterEventComponent, QuadColliderComponent> _boxEnters;

        private readonly EcsFilter<TransformComponent, OnCollisionStayEventComponent, CircleColliderComponent> _circleStays;
        private readonly EcsFilter<TransformComponent, OnCollisionStayEventComponent, QuadColliderComponent> _boxStays;

        public void Run()
        {
            foreach (var entityIndex in _circleEnters)
                ResolveCollisions(_circleEnters, entityIndex);

            foreach (var entityIndex in _boxEnters)
                ResolveCollisions(_boxEnters, entityIndex);

            foreach (var entityIndex in _circleStays)
                ResolveCollisions(_circleStays, entityIndex);

            foreach (var entityIndex in _boxStays)
                ResolveCollisions(_boxStays, entityIndex);
        }

        private static void ResolveCollisions<TCollider, TEvent>(EcsFilter<TransformComponent, TEvent, TCollider> filter,
            int entityIndex)
            where TCollider : struct where TEvent : struct
        {
            var entityA = filter.GetEntity(entityIndex);

            ref var transformComponentA = ref filter.Get1(entityIndex);
            ref var collisionEventComponent = ref filter.Get2(entityIndex);
            ref var colliderComponentA = ref filter.Get3(entityIndex);

            var entityPositionA = transformComponentA.WorldPosition;

            var popOutVector = fix2.zero;
            var count = 0;

            var entities = collisionEventComponent switch
            {
                OnCollisionEnterEventComponent eventComponent => eventComponent.Entities,
                OnCollisionStayEventComponent eventComponent => eventComponent.Entities,
                _ => new HashSet<EcsEntity>()
            };

            foreach (var entityB in entities)
            {
                ref var transformComponentB = ref entityB.Get<TransformComponent>();
                var entityPositionB = transformComponentB.WorldPosition;

                var hasIntersection = EcsExtensions.CheckEntitiesIntersection(colliderComponentA, entityPositionA,
                    entityB, entityPositionB, out var point);

                if (!hasIntersection)
                    continue;

                var isKinematicInteraction = entityA.Has<IsKinematicTag>() || entityB.Has<IsKinematicTag>();

                if (transformComponentA.IsStatic || isKinematicInteraction)
                    continue;

                popOutVector += GetPopOutVector(entityA, entityB, entityPositionA, entityPositionB, point);
                ++count;
            }

            if (count != 0)
                popOutVector /= (fix) count;

            transformComponentA.WorldPosition += popOutVector;
        }

        private static fix2 GetPopOutVector(EcsEntity entityA, EcsEntity entityB, fix2 positionA, fix2 positionB, fix2 point)
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

            return fix2.normalize_safe(vector, fix2.zero) * distance;
        }

        private static fix2 CircleQuadPopOut(fix2 positionA, fix radiusA, fix2 point)
        {
            var vector = positionA - point;
            return fix2.normalize_safe(vector, fix2.zero) * (radiusA - fix2.length(vector));
        }
    }
}
