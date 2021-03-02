using System.Linq;
using App.Level;
using Configs.Singletons;
using UnityEngine;
using UnityEngine.Assertions;

namespace App
{
    public static class ApplicationStarter
    {
        public static void StartGame()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;

            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");

            var sceneManager = applicationHolder.Add<ISceneManager>(new SceneManager());
            sceneManager.LoadScene(SceneBuildIndex.GameLevel, () =>
            {
                var levelConfig = applicationConfig.GameModePvE.LevelConfigs.First();

                var levelManager = applicationHolder.Add<ILevelManager>(new LevelManager());
                levelManager.GenerateLevel(applicationConfig.GameModePvE, levelConfig);
            });
        }
    }
}
