using Core.Attributes;
using Game;
using Game.Behaviours;
using Game.Behaviours.MovementBehaviours;
using UnityEngine;
using RangeInt = Core.Attributes.RangeInt;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AdvancedMovementBehaviour", menuName = "Configs/Behaviour/Advanced Movement Behaviour")]
    public class AdvancedMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        [Range(0f, 1f)]
        public float DirectionChangeChance;

        public override IBehaviourAgent Make(IEntity entity) =>
            new AdvancedMovementBehaviourAgent(this, entity);
    }
}
