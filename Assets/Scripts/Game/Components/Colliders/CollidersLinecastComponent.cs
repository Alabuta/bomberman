using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Colliders
{
    public struct CollidersLinecastComponent
    {
        public LayerMask InteractionLayerMask;

        public fix2 Start;
        public fix2 End;
    }
}
