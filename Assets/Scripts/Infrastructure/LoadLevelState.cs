using App;
using Configs.Level;
using Configs.Singletons;
using Infrastructure.States;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelConfig>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;

        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
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
            Game.LevelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig);

            // Camera follow
        }
    }
}
