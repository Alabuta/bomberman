using Math.FixedPointMath;

namespace Game.Behaviours
{
    public interface IBehaviourAgent
    {
        void Update(GameContext2 gameContext2, IEntity entity, fix deltaTime);
    }
}
