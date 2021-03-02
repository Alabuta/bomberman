using UnityEngine;

namespace App
{
    public static class ApplicationEntryPoint
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeLoadScene()
        {
            ApplicationStarter.StartGame();
        }
    }
}
