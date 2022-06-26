using System.Collections.Generic;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game.Behaviours
{
    public abstract class BehaviourAgent : IBehaviourAgent //<TConfig> where TConfig : BehaviourConfig
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
        public World World { get; }
        public LevelModel LevelModel { get; }

        public IReadOnlyCollection<Hero.Hero> Heroes { get; }

        public GameContext(World world, LevelModel levelModel, IReadOnlyCollection<Hero.Hero> heroes)
        {
            World = world;
            LevelModel = levelModel;
            Heroes = heroes;
        }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
