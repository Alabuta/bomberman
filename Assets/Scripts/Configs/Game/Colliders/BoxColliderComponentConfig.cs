using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "BoxCollider", menuName = "Configs/Colliders/Box Collider")]
    public class BoxColliderComponentConfig : ColliderComponentConfig
    {
        public double InnerRadius = 0.5;
    }
}
