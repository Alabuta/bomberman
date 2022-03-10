using App;
using Configs.Singletons;
using Infrastructure.AssetManagement;
using Infrastructure.Factory;
using Infrastructure.Services;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.Services.SaveLoad;
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

            _sceneLoader.Load(InitialSceneName, OnLoadLevel);
        }

        public void Exit()
        {
        }

        private void RegisterServices()
        {
            _serviceLocator.RegisterSingle<IAssetProvider>(new AssetProvider());
            _serviceLocator.RegisterSingle<IGameFactory>(new GameFactory(_serviceLocator.Single<IAssetProvider>()));
            _serviceLocator.RegisterSingle<IInputService>(new InputService(_serviceLocator.Single<IGameFactory>()));
            _serviceLocator.RegisterSingle<IPersistentProgressService>(new PersistentProgressService());
            _serviceLocator.RegisterSingle<ISaveLoadService>(
                new SaveLoadService(_serviceLocator.Single<IPersistentProgressService>(),
                    _serviceLocator.Single<IGameFactory>()));
        }

        private void OnLoadLevel()
        {
            _gameStateMachine.Enter<LoadProgressState>();
        }
    }
}
