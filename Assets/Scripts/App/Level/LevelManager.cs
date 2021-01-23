using Configs.Level;
using App;
using App.Level;

namespace Level
{
    public interface ILevelManager
    {
        // void GenerateLevelModel(LevelConfig levelConfig);
    }

    public class LevelManager : ILevelManager
    {
        public LevelState CurrentLevelState { get; }

        public LevelManager(/*IGameObjectInstantiateManager manager*/)
        {
            throw new System.NotImplementedException();
        }

        public void GenerateLevel(LevelConfig levelConfig)
        {
            /*var levelMode = */GenerateLevelModel(levelConfig);

            /*if (!(ApplicationHolder.Instance.Container.Get<ISomeInterface>(out var someManager)))
                throw System.NotSupportedException;

            PrefabsManager.Instantiate();

            EnemiesManager.PopulateLevel(levelConfig, levelMode);
            PlayersManager.PopulateLevel(levelConfig, levelMode);

            someManager.Xdsdasd*/
        }

        private void GenerateLevelModel(LevelConfig levelConfig)
        {
            /*
             * Create LevelGrid
             * Create LevelState
             *
             *
             */
        }
    }
}
