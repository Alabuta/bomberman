using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using App.Level;
using Configs.Singletons;
using Core;
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

            StartCorotutine.Start(LoadScene(() =>
            {
                var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

                var levelManager = applicationHolder.Add<ILevelManager>(new LevelManager());
                levelManager.GenerateLevel(levelConfig);
            }));
        }

        private IEnumerator LoadScene(Action action)
        {
            var applicationConfig = ApplicationConfig.Instance;

            var asyncOperation = SceneManager.LoadSceneAsync(applicationConfig.StartScene.name, LoadSceneMode.Single);

            while (!asyncOperation.isDone)
                yield return null;

            action.Invoke();
        }
    }
}
