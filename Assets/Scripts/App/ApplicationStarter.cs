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
    public class ApplicationStarter
    {
        public static void StartGame()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");

            StartCorotutine.Start(LoadScene("EmptyScene", () =>
            {
                StartCorotutine.Start(LoadScene(applicationConfig.StartSceneName, () =>
                {
                    var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

                    var levelManager = applicationHolder.Add<ILevelManager>(new LevelManager());
                    levelManager.GenerateLevel(levelConfig);
                }));
            }));
        }

        private static IEnumerator LoadScene(string sceneName, Action action)
        {
            var onSceneLoadCallback = OnSceneLoaded(sceneName, action);
            SceneManager.sceneLoaded += onSceneLoadCallback;

            var asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            while (!asyncOperation.isDone)
                yield return null;

            SceneManager.sceneLoaded -= onSceneLoadCallback;
        }

        private static UnityAction<Scene, LoadSceneMode> OnSceneLoaded(string sceneName, Action action)
        {
            return (scene, mode) =>
            {
                if (sceneName != scene.name)
                    return;

                action?.Invoke();
            };
        }
    }
}
