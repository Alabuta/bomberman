using App;
using Configs.Level;
using Configs.Singletons;
using Infrastructure.Factory;
using Infrastructure.States;
using Level;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelConfig>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly IGameFactory _gameFactory;

        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader, IGameFactory gameFactory)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _gameFactory = gameFactory;
        }

        public void Enter(LevelConfig levelConfig)
        {
            _sceneLoader.Load(levelConfig.SceneName, () => OnSceneLoaded(levelConfig));
        }

        public void Exit()
        {
        }

        private void OnSceneLoaded(LevelConfig levelConfig)
        {
            var applicationConfig = ApplicationConfig.Instance;

            Game.LevelManager = new GameLevelManager();
            Game.LevelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig, _gameFactory);

            // Camera follow
        }
    }
}
