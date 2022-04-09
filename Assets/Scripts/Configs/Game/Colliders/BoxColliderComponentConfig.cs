using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "BoxCollider", menuName = "Configs/Colliders/Box Collider")]
    public class BoxColliderComponentConfig : ColliderComponentConfig
    {
        public int InnerRadius;
    }
}
