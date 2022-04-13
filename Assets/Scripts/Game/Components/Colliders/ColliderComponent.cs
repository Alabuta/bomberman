using System;
using Math.FixedPointMath;
using UnityEngine;
using Component = Game.Components.Component;

namespace Game.Colliders
{
    public abstract class ColliderComponent : Component
    {
    }

    public static class ColliderExtensions
    {
        public static bool CircleIntersectionPoint(this CircleColliderComponent colliderA, fix2 centerA, Component colliderB,
            fix2 centerB, out fix2 intersection)
        {
            switch (colliderB)
            {
                case CircleColliderComponent circleCollider:
                {
                    var x = fix.circle_and_circle_intersection_point(centerA, colliderA.Radius, centerB, circleCollider.Radius,
                        out intersection);
                    Debug.LogWarning($"{x} {intersection}");
                    return x;
                }

                case BoxColliderComponent boxCollider:
                    return fix.circle_and_box_intersection_point(centerA, colliderA.Radius, centerB, boxCollider.InnerRadius,
                        out intersection);

                default:
                    throw new ArgumentOutOfRangeException(nameof(colliderB));
            }
        }

        public static bool BoxIntersectionPoint(this BoxColliderComponent colliderA, fix2 centerA, Component colliderB,
            fix2 centerB, out fix2 intersection)
        {
            switch (colliderB)
            {
                case CircleColliderComponent circleCollider:
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
