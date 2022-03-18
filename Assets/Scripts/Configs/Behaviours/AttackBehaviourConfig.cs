using Entity;
using Entity.Behaviours;
using Entity.Behaviours.AttackBehaviours;
using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "AttackBehaviour", menuName = "Configs/Behaviour/Attack Behaviour")]
    public class AttackBehaviourConfig : BehaviourConfig
    {
        public int DamageValue = 1;

        public override IBehaviourAgent Make(IEntity entity) =>
            new SimpleAttackBehaviourAgent(this, entity);
    }
}
