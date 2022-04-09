using Game;
using Game.Behaviours;

namespace Configs.Behaviours
{
    public abstract class BehaviourConfig : ConfigBase
    {
        public abstract IBehaviourAgent Make(IEntity entity);
    }
}
