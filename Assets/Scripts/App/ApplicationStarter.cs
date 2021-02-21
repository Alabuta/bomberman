using Configs.Singletons;
using UnityEngine;
using UnityEngine.Assertions;

namespace App
{
    public class ApplicationStarter
    {
        public ApplicationStarter()
        {
            var applicationConfig = ApplicationConfig.Instance;

            QualitySettings.vSyncCount = applicationConfig.EnableVSync ? 1 : 0;
            Application.targetFrameRate = applicationConfig.TargetFrameRate;
        }

        public void StartGame()
        {
            var applicationHolder = ApplicationHolder.Instance;
            Assert.IsNotNull(applicationHolder, "failed to initialize app holder");
        }
    }
}
