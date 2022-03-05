using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AdvancedMovementBehaviour", menuName = "Configs/Behaviour/Advanced Movement Behaviour")]
    public class AdvancedMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [Range(1, 10)]
        public int DirectionChangeFrequency = 1;
    }
}
