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

        public abstract void Update(GameContext2 gameContext2, IEntity entity, fix deltaTime);
    }

    public class GameContext2
    {
        public World World { get; }
        public LevelModel LevelModel { get; }

        // public IReadOnlyCollection<Hero.Hero> Heroes { get; } :TODO: fix

        public GameContext2(World world, LevelModel levelModel /*, IReadOnlyCollection<Hero.Hero> heroes*/) // :TODO: fix
        {
            World = world;
            LevelModel = levelModel;
            // Heroes = heroes;
        }
    }

    public class GridGraph
    {
        public int2 Size { get; set; }
    }
}
