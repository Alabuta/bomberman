using Configs.Game.Colliders;
using Math.FixedPointMath;

namespace Game.Colliders
{
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
