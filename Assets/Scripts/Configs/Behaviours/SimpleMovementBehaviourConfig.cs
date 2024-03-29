using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "SimpleMovementBehaviour", menuName = "Configs/Behaviour/Simple Movement Behaviour")]
    public class SimpleMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [Range(0f, 1f)]
        public float DirectionChangeChance;
    }
}
