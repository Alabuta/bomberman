using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "CircleCollider", menuName = "Configs/Colliders/Circle Collider")]
    public class CircleColliderComponentConfig : ColliderComponentConfig
    {
        public double Radius;
    }
}
