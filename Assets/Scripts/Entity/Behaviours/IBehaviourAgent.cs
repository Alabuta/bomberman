using Math.FixedPointMath;

namespace Entity.Behaviours
{
    public interface IBehaviourAgent
    {
        void Update(GameContext gameContext, IEntity entity, fix deltaTime);
    }
}
