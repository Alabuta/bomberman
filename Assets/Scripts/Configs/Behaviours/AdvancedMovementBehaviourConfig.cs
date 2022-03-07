using Core.Attributes;
using UnityEngine;
using RangeInt = Core.Attributes.RangeInt;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AdvancedMovementBehaviour", menuName = "Configs/Behaviour/Advanced Movement Behaviour")]
    public class AdvancedMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [RangeIntAttribute(1, 10)]
        public RangeInt DirectionChangeFrequency;
    }
}
