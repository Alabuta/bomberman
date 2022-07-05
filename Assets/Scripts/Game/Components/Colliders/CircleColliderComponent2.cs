using Configs.Game.Colliders;
using Math.FixedPointMath;
using UnityEngine;

namespace Game.Colliders
{
    public struct CircleColliderComponent
    {
        public LayerMask InteractionLayerMask;
        public fix Radius;
    }

    public class CircleColliderComponent2 : ColliderComponent2
    {
        public fix Radius { get; }

        public CircleColliderComponent2(CircleColliderComponentConfig config)
            : base(config)
        {
            Radius = (fix) config.Radius;
        }
    }
}
