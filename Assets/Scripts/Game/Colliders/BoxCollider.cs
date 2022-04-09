using Configs.Game.Colliders;

namespace Game.Colliders
{
    public class BoxCollider : ICollider
    {
        public int InnerRadius { get; }

        public BoxCollider(BoxColliderComponentConfig config)
        {
            InnerRadius = config.InnerRadius;
        }
    }
}
