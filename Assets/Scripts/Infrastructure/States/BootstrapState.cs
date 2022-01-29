using System.Linq;
using App;
using Configs.Level;
using Configs.Singletons;
using Infrastructure.AssetManagement;
using Infrastructure.Factory;
using Infrastructure.Services;
using Level;
using Services.Input;
using UnityEngine;

namespace Infrastructure.States
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
            var assetProvider = ServiceLocator.Container.Single<IAssetProvider>();

            ServiceLocator.Container.RegisterSingle<IInputService>(new InputService());
            ServiceLocator.Container.RegisterSingle<IGameFactory>(new GameFactory(assetProvider));

            Game.LevelManager = new GameLevelManager();
        }

        private void OnLoadLevel(LevelConfig levelConfig)
        {
            _gameStateMachine.Enter<LoadLevelState, LevelConfig>(levelConfig);
        }
    }
}
