using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Colliders
{
    public struct QuadColliderComponent
    {
        public LayerMask InteractionLayerMask;

        public fix2 Offset;
        public fix2 Extent;
    }
}
