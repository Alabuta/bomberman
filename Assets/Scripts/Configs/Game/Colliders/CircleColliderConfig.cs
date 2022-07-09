using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "CircleCollider", menuName = "Configs/Colliders/Circle Collider")]
    public class CircleColliderConfig : ColliderConfig
    {
        public double Radius;
    }
}
