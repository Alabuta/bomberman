using System.Threading.Tasks;
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
        private const string InitialSceneName = "BootstrapScene";
        private readonly GameStateMachine _gameStateMachine;
        private readonly ServiceLocator _serviceLocator;

        public BootstrapState(GameStateMachine gameStateMachine, ServiceLocator serviceLocator)
        {
            _serviceLocator = serviceLocator;
            _gameStateMachine = gameStateMachine;

            RegisterServices();
        }

        public async Task Enter()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            await SceneLoader.LoadSceneAsAddressable(InitialSceneName);
            await _gameStateMachine.Enter<LoadProgressState>();
        }

        public void Exit()
        {
        }

        private void RegisterServices()
        {
            _serviceLocator.RegisterSingle<IAssetProvider>(new AssetProvider());
            _serviceLocator.RegisterSingle<IGameFactory>(new GameFactory(_serviceLocator.Single<IAssetProvider>()));
            _serviceLocator.RegisterSingle<IInputService>(new InputService());
            _serviceLocator.RegisterSingle<IPersistentProgressService>(new PersistentProgressService());
            _serviceLocator.RegisterSingle<ISaveLoadService>(
                new SaveLoadService(_serviceLocator.Single<IPersistentProgressService>(),
                    _serviceLocator.Single<IGameFactory>()));
        }
    }
}
