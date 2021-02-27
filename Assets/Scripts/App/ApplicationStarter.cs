using System;
using System.Collections;
using System.Linq;
using App.Level;
using Configs.Singletons;
using Core;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace App
{
    internal enum SceneBuildIndex
    {
        Empty,
        StartScreen,
        // RoundNumberScreen,
        // StageScreen,
        GameLevel
    }

    public static class ApplicationStarter
    {
        public static void StartGame()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");

#if UNITY_EDITOR
            StartCorotutine.Start(LoadScene(SceneBuildIndex.Empty, () =>
            {
#endif
                StartCorotutine.Start(LoadScene(SceneBuildIndex.GameLevel, () =>
                {
                    var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

                    var levelManager = applicationHolder.Add<ILevelManager>(new LevelManager());
                    levelManager.GenerateLevel(levelConfig);
                }));
#if UNITY_EDITOR
            }));
#endif
        }

        private static IEnumerator LoadScene(SceneBuildIndex sceneBuildIndex, Action action)
        {
            var onSceneLoadCallback = OnSceneLoaded(sceneBuildIndex, action);
            SceneManager.sceneLoaded += onSceneLoadCallback;

            var asyncOperation = SceneManager.LoadSceneAsync((int) sceneBuildIndex, LoadSceneMode.Single);

            while (!asyncOperation.isDone)
                yield return null;

            SceneManager.sceneLoaded -= onSceneLoadCallback;
        }

        private static UnityAction<Scene, LoadSceneMode> OnSceneLoaded(SceneBuildIndex sceneBuildIndex, Action action)
        {
            return (scene, mode) =>
            {
                if ((int) sceneBuildIndex != scene.buildIndex)
                    return;

                action?.Invoke();
            };
        }
    }
}
