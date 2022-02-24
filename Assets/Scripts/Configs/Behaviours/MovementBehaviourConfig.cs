using Unity.Mathematics;
using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "MovementBehaviour", menuName = "Configs/Behaviour/Movement Behaviour")]
    public class MovementBehaviourConfig : BehaviourConfig
    {
        public int2[] MovementDirections;
    }
}
