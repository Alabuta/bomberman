using Data;
using Infrastructure.States.SaveLoad;
using Services.PersistentProgress;

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

        private static PlayerProgress CreateEmptyProgress() => new(new Score(0, 0), new LevelStage(0, 0));
    }
}
