using App;
using Configs.Level;
using Configs.Singletons;
using Data;
using Infrastructure.Factory;
using Infrastructure.States;
using Level;
using Services.PersistentProgress;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelStage>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;
        private readonly IGameFactory _gameFactory;
        private readonly IPersistentProgressService _progressService;

        public LoadLevelState(
            GameStateMachine gameStateMachine,
            SceneLoader sceneLoader,
            IGameFactory gameFactory,
            IPersistentProgressService progressService)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
            _gameFactory = gameFactory;
            _progressService = progressService;
        }

        public void Enter(LevelStage levelStage)
        {
            // :TODO: show loading progress

            _gameFactory.CleanUp();

            var applicationConfig = ApplicationConfig.Instance;

            var gameMode = applicationConfig.GameModePvE;
            var levelConfig = gameMode.Levels[levelStage.LevelIndex];

            _sceneLoader.Load(levelConfig.SceneName, () => OnLoaded(levelConfig));
        }

        public void Exit()
        {
        }

        private void OnLoaded(LevelConfig levelConfig)
        {
            InitWorld(levelConfig);
            InformProgressReaders();

            _gameStateMachine.Enter<GameLoopState>();
        }

        private void InformProgressReaders()
        {
            foreach (var progressReader in _gameFactory.ProgressReaders)
                progressReader.LoadProgress(_progressService.Progress);
        }

        private void InitWorld(LevelConfig levelConfig)
        {
            var applicationConfig = ApplicationConfig.Instance;

            Game.LevelManager = new GameLevelManager();
            Game.LevelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig, _gameFactory);

            // Camera follow
        }
    }
}
