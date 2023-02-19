using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Colliders
{
    public readonly struct CircleColliderComponent
    {
        public readonly LayerMask InteractionLayerMask;
        public readonly fix Radius;

        public CircleColliderComponent(LayerMask interactionLayerMask, fix radius)
        {
            InteractionLayerMask = interactionLayerMask;
            Radius = radius;
        }
    }
}
