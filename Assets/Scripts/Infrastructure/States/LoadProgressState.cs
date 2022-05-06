using System.Linq;
using Configs.Singletons;
using Data;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.Services.SaveLoad;
using UnityEngine.Assertions;

namespace Infrastructure.States
{
    public class LoadProgressState : IGameState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadService;

        public LoadProgressState(GameStateMachine gameStateMachine, IPersistentProgressService progressService,
            ISaveLoadService saveLoadService)
        {
            _gameStateMachine = gameStateMachine;
            _progressService = progressService;
            _saveLoadService = saveLoadService;
        }

        public void Enter()
        {
            LoadOrCreateProgressState();

            var levelStage = _progressService.Progress.WorldData.LevelStage;
            _gameStateMachine.Enter<LoadLevelState, LevelStage>(levelStage);
        }

        public void Exit()
        {
        }

        private void LoadOrCreateProgressState()
        {
            // :TODO: use player progress from server
            _progressService.Progress = /*_saveLoadService.LoadProgress() ??*/ CreateEmptyProgress();

            _saveLoadService.SaveProgress();
        }

        private static PlayerProgress CreateEmptyProgress()
        {
            var applicationConfig = ApplicationConfig.Instance;

            var gameModePvE = applicationConfig.GameModePvE;

            var levelConfig = gameModePvE.LevelConfigs.FirstOrDefault();
            Assert.IsNotNull(levelConfig);

            var levelStageConfig = levelConfig.LevelStages.FirstOrDefault();
            Assert.IsNotNull(levelStageConfig);

            var defaultLevelStage = new LevelStage(gameModePvE, levelConfig, levelStageConfig);
            var defaultScore = new Score(0, 1);

            return new PlayerProgress(defaultScore, defaultLevelStage);
        }
    }
}
