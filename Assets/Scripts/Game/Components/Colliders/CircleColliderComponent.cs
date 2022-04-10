using Configs.Game.Colliders;
using Math.FixedPointMath;

namespace Game.Colliders
{
    public class CircleColliderComponent : ColliderComponent
    {
        public fix Radius { get; }

        public CircleColliderComponent(CircleColliderComponentConfig config)
        {
            Radius = (fix) config.Radius;
        }
    }
}
