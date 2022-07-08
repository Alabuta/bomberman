using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Behaviours
{
    public struct SimpleAttackBehaviourComponent
    {
        public LayerMask InteractionLayerMask;
        public int DamageValue; // :TODO: get damage value from actual entity parameters
        public fix HitRadius;
    }
}
