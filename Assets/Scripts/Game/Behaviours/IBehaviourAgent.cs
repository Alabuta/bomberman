using Math.FixedPointMath;

namespace Game.Behaviours
{
    public interface IBehaviourAgent
    {
        void Update(GameContext gameContext, IEntity entity, fix deltaTime);
    }
}
