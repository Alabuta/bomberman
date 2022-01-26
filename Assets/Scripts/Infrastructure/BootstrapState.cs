using System.Linq;
using App;
using Configs.Level;
using Configs.Singletons;
using Level;
using Services.Input;
using UnityEngine;

namespace Infrastructure
{
    public class BootstrapState : IGameState
    {
        private const string InitialSceneName = "InitialScene";
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;

        public BootstrapState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
        }

        public void Enter()
        {
            RegisterServices();

            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            var gameMode = applicationConfig.GameModePvE;
            var levelConfig = gameMode.Levels.First();

            _sceneLoader.Load(InitialSceneName, () => OnLoadLevel(levelConfig));
        }

        public void Exit()
        {
        }

        private void RegisterServices()
        {
            Game.InputService = RegisterInputService();
            Game.LevelManager = new GameLevelManager();
        }

        private static IInputService RegisterInputService()
        {
            return new InputService();
        }

        private void OnLoadLevel(LevelConfig levelConfig)
        {
            _gameStateMachine.Enter<LoadLevelState, LevelConfig>(levelConfig);
        }
    }
}
