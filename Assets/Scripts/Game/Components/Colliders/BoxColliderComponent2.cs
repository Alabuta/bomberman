using Configs.Game.Colliders;
using Math.FixedPointMath;

namespace Game.Colliders
{
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
