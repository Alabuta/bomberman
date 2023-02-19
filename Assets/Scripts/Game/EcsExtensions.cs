using System;
using System.Collections.Generic;
using Configs.Behaviours;
using Configs.Entity;
using Configs.Game.Colliders;
using Game.Components;
using Game.Components.Behaviours;
using Game.Components.Colliders;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game
{
    public static class EcsExtensions
    {
        public static void AddBehaviourComponents(this EcsEntity ecsEntity,
            EnemyConfig enemyConfig, IEnumerable<BehaviourConfig> behaviourConfigs)
        {
            foreach (var behaviourConfig in behaviourConfigs)
            {
                switch (behaviourConfig)
                {
                    case SimpleMovementBehaviourConfig config:
                        var transformComponent = ecsEntity.Get<TransformComponent>();

                        ecsEntity.Replace(new SimpleMovementBehaviourComponent
                        {
                            MovementDirections = config.MovementDirections,

                            TryToSelectNewTile = config.TryToSelectNewTile,
                            DirectionChangeChance = (fix) config.DirectionChangeChance,

                            FromWorldPosition = transformComponent.WorldPosition,
                            ToWorldPosition = transformComponent.WorldPosition
                        });

                        ecsEntity.Replace(new IsKinematicTag());

                        break;

                    case SimpleAttackBehaviourConfig config:
                        ecsEntity.Replace(new SimpleAttackBehaviourComponent
                        {
                            InteractionLayerMask = config.InteractionLayerMask,
                            DamageValue = config.DamageValue,
                            HitRadius = (fix) enemyConfig.DamageParameters.HitRadius // :TODO: refactor
                        });
                        break;
                }
            }
        }

        public static void AddCollider(this EcsEntity entity, ColliderConfig colliderConfig)
        {
            switch (colliderConfig)
            {
                case BoxColliderConfig config:
                    entity.Replace(new BoxColliderComponent
                    (
                        interactionLayerMask: config.InteractionLayerMask,
                        offset: (fix2) config.Offset,
                        extent: (fix2) (config.Size / 2.0)
                    ));
                    entity.Replace(new HasColliderTag());
                    break;

                case CircleColliderConfig config:
                    entity.Replace(new CircleColliderComponent
                    (
                        interactionLayerMask: config.InteractionLayerMask,
                        radius: (fix) config.Radius
                    ));
                    entity.Replace(new HasColliderTag());
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int GetCollidersInteractionMask(this EcsEntity ecsEntity)
        {
            var interactionMask = 0;

            if (ecsEntity.Has<CircleColliderComponent>())
                interactionMask = ecsEntity.Get<CircleColliderComponent>().InteractionLayerMask;

            else if (ecsEntity.Has<BoxColliderComponent>())
                interactionMask = ecsEntity.Get<BoxColliderComponent>().InteractionLayerMask;

            return interactionMask;
        }

        public static bool IsAlive(this ref HealthComponent healthComponent)
        {
            return healthComponent.CurrentHealth < 1;
        }

        public static long GetEntitiesPairHash(EcsEntity entityA, EcsEntity entityB)
        {
            var a = entityA.GetInternalId();
            var b = entityB.GetInternalId();
            return a > b
                ? ((long) a << 32) | (uint) b
                : ((long) b << 32) | (uint) a;
        }

        public static bool CheckEntitiesIntersection<T>(T colliderComponentA, fix2 entityPositionA,
            EcsEntity entityB, fix2 entityPositionB, out fix2 intersectionPoint)
            where T : struct
        {
            var hasIntersection = false;
            intersectionPoint = default;

            if (entityB.Has<CircleColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<CircleColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    CircleColliderComponent circleColliderComponentA =>
                        fix.circle_and_circle_intersection(
                            entityPositionA, circleColliderComponentA.Radius,
                            entityPositionB, colliderComponentB.Radius,
                            out intersectionPoint),

                    BoxColliderComponent boxColliderComponentA =>
                        fix.circle_and_box_intersection_point(
                            entityPositionB, colliderComponentB.Radius,
                            entityPositionA, boxColliderComponentA.Offset, boxColliderComponentA.Extent,
                            out intersectionPoint),

                    _ => false
                };
            }
            else if (entityB.Has<BoxColliderComponent>())
            {
                ref var colliderComponentB = ref entityB.Get<BoxColliderComponent>();

                hasIntersection = colliderComponentA switch
                {
                    CircleColliderComponent circleColliderComponentA =>
                        fix.circle_and_box_intersection_point(
                            entityPositionA, circleColliderComponentA.Radius,
                            entityPositionB, colliderComponentB.Offset, colliderComponentB.Extent,
                            out intersectionPoint),

                    BoxColliderComponent =>
                        throw new NotImplementedException(), // :TODO: implement

                    _ => false
                };
            }

            return hasIntersection;
        }

        public static (fix2 extent, fix2 offset) GetEntityColliderExtentAndOffset(this EcsEntity entity)
        {
            var extent = fix2.zero;
            var offset = fix2.zero;

            if (entity.Has<CircleColliderComponent>())
            {
                ref var colliderComponent = ref entity.Get<CircleColliderComponent>();
                extent = colliderComponent.Radius;
            }
            else if (entity.Has<BoxColliderComponent>())
            {
                ref var colliderComponent = ref entity.Get<BoxColliderComponent>();
                extent = colliderComponent.Extent;
                offset = colliderComponent.Offset;
            }

            return (extent, offset);
        }

        public static AABB GetEntityColliderAABB(this EcsEntity entity, fix2 position)
        {
            var (extent, offset) = entity.GetEntityColliderExtentAndOffset();

            var min = position + offset - extent;
            var max = position + offset + extent;

            return new AABB(min, max);
        }
    }
}
