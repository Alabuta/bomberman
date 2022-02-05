using Data;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.Services.SaveLoad;

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
            _progressService.Progress = _saveLoadService.LoadProgress() ?? CreateEmptyProgress();
        }

        private PlayerProgress CreateEmptyProgress()
        {
            var progress = new PlayerProgress(new Score(0, 1), new LevelStage(0, 0));
            _saveLoadService.SaveProgress();
            return progress;
        }
    }
}
