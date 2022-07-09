using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "QuadCollider", menuName = "Configs/Colliders/Quad Collider")]
    public class QuadColliderConfig : ColliderConfig
    {
        public double InnerRadius = 0.5;
    }
}
