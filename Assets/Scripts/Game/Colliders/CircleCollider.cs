using Configs.Game.Colliders;

namespace Game.Colliders
{
    public class CircleCollider : ICollider
    {
        public int Radius { get; }

        public CircleCollider(CircleColliderComponentConfig config)
        {
            Radius = config.Radius;
        }
    }
}
