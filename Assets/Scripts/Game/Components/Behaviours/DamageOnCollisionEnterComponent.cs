using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Behaviours
{
    public struct DamageOnCollisionEnterComponent
    {
        public LayerMask InteractionLayerMask;
        public fix DamageValue; // :TODO: get damage value from actual entity parameters
        public fix HitRadius;
    }
}
