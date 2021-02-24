using System.Collections;
using System.Collections.Generic;
using System.Linq;
using App.Level;
using Configs.Singletons;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace App
{
    public class ApplicationStarter
    {
        public void StartGame()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");


            SceneManager.LoadSceneAsync(applicationConfig.StartScene.name, LoadSceneMode.Single);
            var scene = SceneManager.GetActiveScene();
            // scene.isLoaded;

            var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

            var levelManager = applicationHolder.Add<ILevelManager>(new LevelManager());
            levelManager.GenerateLevel(levelConfig);
        }

        IEnumerator LoadScene()
        {
            var applicationConfig = ApplicationConfig.Instance;

            var asyncOperation = SceneManager.LoadSceneAsync(applicationConfig.StartScene.name, LoadSceneMode.Single);

            while ( !asyncOperation.isDone )
                yield return null;
        }
    }
}
