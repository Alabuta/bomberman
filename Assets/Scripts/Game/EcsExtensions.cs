using System;
using System.Collections.Generic;
using Configs.Behaviours;
using Configs.Entity;
using Configs.Game.Colliders;
using Game.Colliders;
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

                        ecsEntity.Replace(new NonPlayerPositionControlTag());

                        break;

                    case SimpleAttackBehaviourConfig config:
                        ecsEntity.Replace(new SimpleAttackBehaviourComponent
                        {
                            InteractionLayerMask = config.InteractionLayerMask,
                            DamageValue = config.DamageValue,
                            HitRadius = (fix) enemyConfig.HitRadius // :TODO: refactor
                        });
                        break;
                }
            }
        }

        public static void AddColliderComponents(this EcsEntity entity,
            IEnumerable<ColliderComponentConfig> colliderComponentConfigs)
        {
            foreach (var colliderComponentConfig in colliderComponentConfigs)
            {
                switch (colliderComponentConfig)
                {
                    case BoxColliderComponentConfig config:
                        entity.Replace(new BoxColliderComponent
                        {
                            InteractionLayerMask = config.InteractionLayerMask,
                            InnerRadius = (fix) config.InnerRadius
                        });
                        entity.Replace(new HasColliderTag());
                        break;

                    case CircleColliderComponentConfig config:
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
    }
}
