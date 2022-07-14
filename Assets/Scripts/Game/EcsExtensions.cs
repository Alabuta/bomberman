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
                case QuadColliderConfig config:
                    entity.Replace(new QuadColliderComponent
                    {
                        InteractionLayerMask = config.InteractionLayerMask,
                        Size = (fix) config.Size
                    });
                    entity.Replace(new HasColliderTag());
                    break;

                case CircleColliderConfig config:
                    entity.Replace(new CircleColliderComponent
                    {
                        InteractionLayerMask = config.InteractionLayerMask,
                        Radius = (fix) config.Radius
                    });
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

            else if (ecsEntity.Has<QuadColliderComponent>())
                interactionMask = ecsEntity.Get<QuadColliderComponent>().InteractionLayerMask;

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
    }
}
