using Unity.Mathematics;
using UnityEngine;

namespace Configs.Game.Colliders
{
    [CreateAssetMenu(fileName = "BoxCollider", menuName = "Configs/Colliders/Box Collider")]
    public class BoxColliderConfig : ColliderConfig
    {
        public double2 Offset = double2.zero;
        public double2 Size = new(1);
    }
}
