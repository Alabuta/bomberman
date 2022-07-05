using System;
using Configs.Game.Colliders;
using Leopotam.Ecs;
using Math.FixedPointMath;
using UnityEngine;
using Component = Game.Components.Component;

namespace Game.Colliders
{
    public struct ColliderComponent
    {
    }

    public struct HasColliderTag : IEcsIgnoreInFilter
    {
    }

    public abstract class ColliderComponent2 : Component
    {
        public LayerMask InteractionLayerMask;

        protected ColliderComponent2(ColliderComponentConfig config)
        {
            InteractionLayerMask = config.InteractionLayerMask;
        }
    }

    public static class ColliderExtensions
    {
        public static bool CircleIntersectionPoint(this CircleColliderComponent2 colliderA, fix2 centerA, Component colliderB,
            fix2 centerB, out fix2 intersection)
        {
            switch (colliderB)
            {
                case CircleColliderComponent2 circleCollider:
                {
                    return fix.circle_and_circle_intersection_point(centerA, colliderA.Radius, centerB, circleCollider.Radius,
                        out intersection);
                }

                case BoxColliderComponent2 boxCollider:
                    return fix.circle_and_box_intersection_point(centerA, colliderA.Radius, centerB, boxCollider.InnerRadius,
                        out intersection);

                default:
                    throw new ArgumentOutOfRangeException(nameof(colliderB));
            }
        }

        public static bool BoxIntersectionPoint(this BoxColliderComponent2 colliderA, fix2 centerA, Component colliderB,
            fix2 centerB, out fix2 intersection)
        {
            switch (colliderB)
            {
                case CircleColliderComponent2 circleCollider:
                    return fix.circle_and_box_intersection_point(centerB, circleCollider.Radius, centerA, colliderA.InnerRadius,
                        out intersection);

                /*case BoxColliderComponent boxCollider:
                    return fix.box_and_box_intersection_point(centerA, colliderA.InnerRadius, centerB, boxCollider.InnerRadius,
                        out intersection);*/

                default:
                    throw new ArgumentOutOfRangeException(nameof(colliderB));
            }
        }
    }
}
