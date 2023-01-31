using System;
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

namespace Game.Systems
{
    public class CollisionsResolverSystem : IEcsRunSystem
    {
        private static readonly fix VectorsAngleThreshold = fix.one / fix.sqrt(new fix(2));

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IRTree _entitiesAabbTree;

        private readonly EcsFilter<TransformComponent, OnCollisionEnterEventComponent, CircleColliderComponent> _circleEnters;
        private readonly EcsFilter<TransformComponent, OnCollisionStayEventComponent, CircleColliderComponent> _circleStays;

        private readonly EcsFilter<TransformComponent, OnCollisionEnterEventComponent, BoxColliderComponent> _boxEnters;
        private readonly EcsFilter<TransformComponent, OnCollisionStayEventComponent, BoxColliderComponent> _boxStays;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders; // :TODO: use AABBComponent

        public void Run()
        {
            using ( Profiling.CollisionsResolver.Auto() )
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

            _entitiesAabbTree.Build(_colliders);
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
            var entityLastPositionA = entityPositionA;

            if (entityA.Has<PrevFrameDataComponent>())
            {
                ref var prevFrameDataComponentA = ref entityA.Get<PrevFrameDataComponent>();
                entityLastPositionA = prevFrameDataComponentA.LastWorldPosition;
            }

            var entities = collisionEventComponent switch
            {
                OnCollisionEnterEventComponent eventComponent => eventComponent.Entities,
                OnCollisionStayEventComponent eventComponent => eventComponent.Entities,
                _ => new HashSet<EcsEntity>()
            };

            var popOutVector = fix2.zero;

            foreach (var entityB in entities)
            {
                ref var transformComponentB = ref entityB.Get<TransformComponent>();

                var entityPositionB = transformComponentB.WorldPosition;
                var entityLastPositionB = entityPositionB;

                if (entityB.Has<PrevFrameDataComponent>())
                {
                    ref var prevFrameDataComponentB = ref entityB.Get<PrevFrameDataComponent>();
                    entityLastPositionB = prevFrameDataComponentB.LastWorldPosition;
                }

                var hasIntersection = EcsExtensions.CheckEntitiesIntersection(colliderComponentA, entityPositionA,
                    entityB, entityPositionB, out var point);

                if (!hasIntersection)
                    continue;

                var isKinematicInteraction = entityA.Has<IsKinematicTag>() || entityB.Has<IsKinematicTag>();

                if (transformComponentA.IsStatic || isKinematicInteraction)
                    continue;

                var vector = GetPopOutVector(entityA, entityB, entityPositionA, entityLastPositionA, entityPositionB,
                    entityLastPositionB, point);

                if (fix2.dot(popOutVector, vector) < VectorsAngleThreshold)
                    popOutVector += vector;
            }

            transformComponentA.WorldPosition += popOutVector;
        }

        private static fix2 GetPopOutVector(EcsEntity entityA, EcsEntity entityB, fix2 positionA, fix2 lastPositionA,
            fix2 positionB, fix2 lastPositionB, fix2 point)
        {
            if (entityA.Has<CircleColliderComponent>())
            {
                ref var colliderComponentA = ref entityA.Get<CircleColliderComponent>();

                if (entityB.Has<CircleColliderComponent>())
                {
                    ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

                    return GetCircleFromCirclePopOutVector(positionA, colliderComponentA.Radius,
                        positionB, colliderComponentB.Radius);
                }

                if (entityB.Has<BoxColliderComponent>())
                    return GetCircleFromBoxPopOutVector(positionA, lastPositionA, colliderComponentA.Radius, point);
            }

            if (entityA.Has<BoxColliderComponent>())
            {
                if (entityB.Has<CircleColliderComponent>())
                {
                    ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();
                    return -GetCircleFromBoxPopOutVector(positionB, lastPositionB, colliderComponentB.Radius, point);
                }

                if (entityB.Has<BoxColliderComponent>())
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        private static fix2 GetCircleFromCirclePopOutVector(fix2 positionA, fix radiusA, fix2 positionB, fix radiusB)
        {
            var vector = positionA - positionB;
            var distance = radiusA + radiusB - fix2.length(vector);

            return fix2.normalize_safe(vector, fix2.zero) * distance;
        }

        private static fix2 GetCircleFromBoxPopOutVector(fix2 positionCircle, fix2 lastPositionCircle, fix circleRadius,
            fix2 point)
        {
            var vector = positionCircle - point;

            var length = fix2.length(vector);
            if (length == fix.zero)
                return lastPositionCircle - positionCircle;

            vector = length != fix.zero ? vector / length : fix2.zero;
            return vector * (circleRadius - length);
        }
    }
}
