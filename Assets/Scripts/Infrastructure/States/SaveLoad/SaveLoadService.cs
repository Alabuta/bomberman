using Data;
using Infrastructure.Data;
using UnityEngine;

namespace Infrastructure.States.SaveLoad
{
    public class SaveLoadService : ISaveLoadService
    {
        private const string ProgressKey = "Progress";

        public void SaveProgress()
        {
        }

        public PlayerProgress LoadProgress()
        {
            return PlayerPrefs.GetString(ProgressKey)?.Deserialize<PlayerProgress>();
        }
    }
}
