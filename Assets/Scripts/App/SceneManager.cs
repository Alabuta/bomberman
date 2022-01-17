using System;
using System.Collections;
using System.Linq;
using Configs.Singletons;
using Core;
using Level;
using ModestTree;
using UnityEngine;
using UnityEngine.Events;
using UnitySceneManagement = UnityEngine.SceneManagement;

namespace App
{
    public sealed class SceneManager : ISceneManager
    {
        public UnitySceneManagement.Scene ActiveScene => UnitySceneManagement.SceneManager.GetActiveScene();

        public SceneBuildIndex ActiveSceneBuildIndex =>
            (SceneBuildIndex) UnitySceneManagement.SceneManager.GetActiveScene().buildIndex;

        public GameObject ActiveSceneRoot { get; private set; }

        public void StartNewGame()
        {
            LoadScene(SceneBuildIndex.GameLevel, () =>
            {
                var applicationConfig = ApplicationConfig.Instance;
                var applicationHolder = ApplicationHolder.Instance;
                Assert.IsNotNull(applicationHolder, "failed to initialize app holder");

                var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

                var levelManager = applicationHolder.Add<ILevelManager>(new GameLevelManager());
                levelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig);
            });
        }

        public void LoadScene(SceneBuildIndex sceneBuildIndex, Action action)
        {
            // Play FadeIn effect animation
            // Load scene while playing FadeOut effect animation

            StartCorotutine.Start(LoadSceneAsync(sceneBuildIndex, action));
        }

        private IEnumerator LoadSceneAsync(SceneBuildIndex sceneBuildIndex, Action action)
        {
            var onSceneLoadCallback = OnSceneLoaded(sceneBuildIndex, action);
            UnitySceneManagement.SceneManager.sceneLoaded += onSceneLoadCallback;

            var loadOperation = UnitySceneManagement.SceneManager.LoadSceneAsync(
                (int) sceneBuildIndex, UnitySceneManagement.LoadSceneMode.Single
            );

            while (!loadOperation.isDone)
                yield return null;

            UnitySceneManagement.SceneManager.sceneLoaded -= onSceneLoadCallback;
        }

        private UnityAction<UnitySceneManagement.Scene, UnitySceneManagement.LoadSceneMode> OnSceneLoaded(
            SceneBuildIndex sceneBuildIndex, Action action)
        {
            return (scene, mode) =>
            {
                if ((int) sceneBuildIndex != scene.buildIndex)
                    return;

                ActiveSceneRoot = new GameObject("GameField");

                action?.Invoke();
            };
        }
    }
}
