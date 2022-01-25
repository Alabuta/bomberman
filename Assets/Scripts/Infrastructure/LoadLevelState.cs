using App;
using Configs.Level;
using UnityEngine;

namespace Infrastructure
{
    public class LoadLevelState : IPayloadedState<LevelConfig>
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly SceneLoader _sceneLoader;

        public LoadLevelState(GameStateMachine gameStateMachine, SceneLoader sceneLoader)
        {
            _gameStateMachine = gameStateMachine;
            _sceneLoader = sceneLoader;
        }

        public void Enter(LevelConfig levelConfig)
        {
            _sceneLoader.Load(levelConfig.SceneName, OnSceneLoaded);
        }

        public void Exit()
        {
        }

        private void OnSceneLoaded()
        {
            var tile = Instantiate("PillarTile 1");

            // Camera follow
        }

        private static GameObject Instantiate(string path)
        {
            var prefab = Resources.Load<GameObject>(path);
            return Object.Instantiate(prefab);
        }
    }
}
