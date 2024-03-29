using Data;
using Infrastructure.Data;
using Infrastructure.Factory;
using Infrastructure.Services.PersistentProgress;
using UnityEngine;

namespace Infrastructure.Services.SaveLoad
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string ProgressKey = "Progress";

        private readonly IPersistentProgressService _progressService;
        private readonly IGameFactory _gameFactory;

        public SaveLoadService(IPersistentProgressService progressService, IGameFactory gameFactory)
        {
            _progressService = progressService;
            _gameFactory = gameFactory;
        }

        public void SaveProgress()
        {
            foreach (var progressWriter in _gameFactory.ProgressWriters)
                progressWriter.UpdateProgress(_progressService.Progress);

            PlayerPrefs.SetString(ProgressKey, _progressService.Progress.ToJson());
        }

        public PlayerProgress LoadProgress()
        {
            return PlayerPrefs.GetString(ProgressKey)?.Deserialize<PlayerProgress>();
        }
    }
}
