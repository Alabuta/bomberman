using Core.Attributes;
using UnityEngine;
using RangeInt = UnityEngine.RangeInt;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AdvancedMovementBehaviour", menuName = "Configs/Behaviour/Advanced Movement Behaviour")]
    public class AdvancedMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [RangeIntAttribute(0, 10)]
        public RangeInt X;
    }
}
