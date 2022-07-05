using Configs.Game.Colliders;
using Game.Colliders;
using Math.FixedPointMath;
using UnityEngine;

namespace Game.Components.Colliders
{
    public struct BoxColliderComponent
    {
        public LayerMask InteractionLayerMask;
        public fix InnerRadius;
    }

    public class BoxColliderComponent2 : ColliderComponent2
    {
        public fix InnerRadius { get; }

        public BoxColliderComponent2(BoxColliderComponentConfig config)
            : base(config)
        {
            InnerRadius = (fix) config.InnerRadius;
        }
    }
}
