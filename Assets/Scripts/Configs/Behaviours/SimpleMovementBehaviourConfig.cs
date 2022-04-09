using Game;
using Game.Behaviours;
using Game.Behaviours.MovementBehaviours;
using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "SimpleMovementBehaviour", menuName = "Configs/Behaviour/Simple Movement Behaviour")]
    public class SimpleMovementBehaviourConfig : MovementBehaviourBaseConfig
    {
        public override IBehaviourAgent Make(IEntity entity) =>
            new SimpleMovementBehaviourAgent(this, entity);
    }
}
