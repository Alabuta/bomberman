using Entity;
using Entity.Behaviours;
using Entity.Behaviours.AttackBehaviours;
using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "SimpleAttackBehaviour", menuName = "Configs/Behaviour/Simple Attack Behaviour")]
    public class SimpleAttackBehaviourConfig : BehaviourConfig
    {
        public int DamageValue = 1;

        public override IBehaviourAgent Make(IEntity entity) =>
            new SimpleAttackBehaviourAgent(this, entity);
    }
}
