using Unity.Mathematics;

namespace Configs.Behaviours
{
    public abstract class MovementBehaviourBaseConfig : BehaviourConfig
    {
        public int2[] MovementDirections;
        public bool TryToSelectNewTile;
    }
}
