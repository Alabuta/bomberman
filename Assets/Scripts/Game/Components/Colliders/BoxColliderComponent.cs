using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Colliders
{
    public readonly struct BoxColliderComponent
    {
        public readonly LayerMask InteractionLayerMask;

        public readonly fix2 Offset;
        public readonly fix2 Extent;

        public BoxColliderComponent(LayerMask interactionLayerMask, fix2 offset, fix2 extent)
        {
            InteractionLayerMask = interactionLayerMask;
            Offset = offset;
            Extent = extent;
        }
    }
}
