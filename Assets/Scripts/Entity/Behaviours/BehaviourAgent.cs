using System.Collections.Generic;
using Level;
using Unity.Mathematics;

namespace Entity.Behaviours
{
    public abstract class BehaviourAgent : IBehaviourAgent//<TConfig> where TConfig : BehaviourConfig
    {
        /*public TConfig Config { get; private set; }

        protected BehaviourAgent(TConfig config)
        {
            Config = config;
        }*/

        public abstract void Update(GameContext gameContext, IEntity entity);
    }

    public class GameContext
    {
        public GameLevelGridModel LevelGridModel { get; }

        public GameContext(GameLevelGridModel levelGridModel)
        {
            LevelGridModel = levelGridModel;
        }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
