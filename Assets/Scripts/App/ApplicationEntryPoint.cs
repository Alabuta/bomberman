using UnityEngine;

namespace App
{
    public static class ApplicationEntryPoint
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeLoadScene()
        {
            var appStarter = new ApplicationStarter();
            appStarter.StartGame();
        }
    }
}
