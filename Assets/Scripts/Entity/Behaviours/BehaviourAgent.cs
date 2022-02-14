using Level;
using Unity.Mathematics;

namespace Entity.Behaviours
{
    public abstract class BehaviourAgent
    {
        public abstract void Update(GameContext gameContext, IEntity entity);
    }

    public class MovementBehaviourAgent : BehaviourAgent
    {
        public int2 DestinationCell { get; protected set; }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var direction = entity.Direction;

            var levelGridModel = gameContext.LevelGridModel;

            // levelGridModel.WorldPositionToCellCoordinate()
        }
    }

    public class GameContext
    {
        public GameLevelGridModel LevelGridModel { get; }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
