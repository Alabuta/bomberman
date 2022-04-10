using Configs.Game.Colliders;
using Math.FixedPointMath;

namespace Game.Colliders
{
    public class BoxColliderComponent : ColliderComponent
    {
        public fix InnerRadius { get; }

        public BoxColliderComponent(BoxColliderComponentConfig config)
        {
            InnerRadius = (fix) config.InnerRadius;
        }
    }
}
