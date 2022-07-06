using UnityEngine;

namespace Configs.Behaviours
{
    [CreateAssetMenu(fileName = "SimpleAttackBehaviour", menuName = "Configs/Behaviour/Simple Attack Behaviour")]
    public class SimpleAttackBehaviourConfig : BehaviourConfig
    {
        public int DamageValue = 1;
    }
}
