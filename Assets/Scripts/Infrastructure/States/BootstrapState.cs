using System.Linq;
using App;
using Configs.Level;
using Configs.Singletons;
using Infrastructure.AssetManagement;
using Infrastructure.Factory;
using Infrastructure.Services;
using Services.Input;
using UnityEngine;

namespace Infrastructure.States
{
    public class BootstrapState : IGameState
    {
        private const string InitialSceneName = "InitialScene";
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly ServiceLocator _serviceLocator;

        public BootstrapState(GameStateMachine gameStateMachine, SceneLoader sceneLoader, ServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;

            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;

            RegisterServices();
        }

        public void Enter()
        {
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
            _serviceLocator.RegisterSingle<IInputService>(new InputService());

            var assetProvider = _serviceLocator.RegisterSingle<IAssetProvider>(new AssetProvider());
            _serviceLocator.RegisterSingle<IGameFactory>(new GameFactory(assetProvider));
        }

        private void OnLoadLevel(LevelConfig levelConfig)
        {
            _gameStateMachine.Enter<LoadLevelState, LevelConfig>(levelConfig);
        }
    }
}
