using System;
using Configs.Game.Colliders;
using Game.Colliders;
using Game.Components.Colliders;
using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game
{
    public static class EcsExtensions
    {
        public static void AddColliderComponent(this EcsEntity entity, ColliderComponentConfig colliderConfig)
        {
            switch (colliderConfig)
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

        public static int GetCollidersInteractionMask(this EcsEntity ecsEntity)
        {
            var interactionMask = 0;

            if (ecsEntity.Has<CircleColliderComponent>())
                interactionMask = ecsEntity.Get<CircleColliderComponent>().InteractionLayerMask;

            else if (ecsEntity.Has<BoxColliderComponent>())
                interactionMask = ecsEntity.Get<BoxColliderComponent>().InteractionLayerMask;

            return interactionMask;
        }
    }
}
