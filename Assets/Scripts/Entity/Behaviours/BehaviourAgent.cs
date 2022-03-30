using System.Collections.Generic;
using Level;
using Math.FixedPointMath;
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

        public abstract void Update(GameContext gameContext, IEntity entity, fix deltaTime);
    }

    public class GameContext
    {
        public LevelModel LevelModel { get; }

        public IReadOnlyCollection<Hero.Hero> Heroes { get; }

        public GameContext(LevelModel levelModel, IReadOnlyCollection<Hero.Hero> heroes)
        {
            LevelModel = levelModel;
            Heroes = heroes;
        }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
