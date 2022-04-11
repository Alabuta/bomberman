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
                    Debug.LogWarning(circleCollider);
                    return fix.circle_and_circle_intersection_point(centerA, colliderA.Radius, centerB, circleCollider.Radius,
                        out intersection);

                case BoxColliderComponent boxCollider:
                    return fix.circle_and_box_intersection_point(centerA, colliderA.Radius, centerB, boxCollider.InnerRadius,
                        out intersection);

                default:
                    throw new ArgumentOutOfRangeException(nameof(colliderB));
            }
        }
    }
}
