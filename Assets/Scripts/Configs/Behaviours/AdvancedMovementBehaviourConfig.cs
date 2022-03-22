using Core.Attributes;
using Entity;
using Entity.Behaviours;
using Entity.Behaviours.MovementBehaviours;
using UnityEngine;
using RangeInt = Core.Attributes.RangeInt;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AdvancedMovementBehaviour", menuName = "Configs/Behaviour/Advanced Movement Behaviour")]
    public class AdvancedMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [RangeIntAttribute(1, 10)]
        public RangeInt DirectionChangeFrequency;

        public override IBehaviourAgent Make(IEntity entity) =>
            new AdvancedMovementBehaviourAgent(this, entity);
    }
}
