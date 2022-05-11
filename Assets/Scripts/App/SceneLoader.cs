using System;
using System.Collections;
using System.Threading.Tasks;
using Infrastructure;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace App
{
    public class SceneLoader
    {
        private readonly ICoroutineRunner _coroutineRunner;

        public SceneLoader(ICoroutineRunner coroutineRunner)
        {
            _coroutineRunner = coroutineRunner;
        }

        public static async Task LoadSceneAsAddressable(string sceneName)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
                return;

            var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Assert.IsTrue(handle.IsValid(),
                $"invalid async operation handle {sceneName}: {handle.Status} {handle.OperationException}");

            await handle.Task;
        }

        public void LoadSceneFromBuild(string sceneName, Action callback)
        {
            _coroutineRunner.StartCoroutine(LoadScene(sceneName, callback));
        }

        private static IEnumerator LoadScene(string sceneName, Action callback)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                callback?.Invoke();
                yield break;
            }

            var loadOperation = SceneManager.LoadSceneAsync(sceneName);

            while (!loadOperation.isDone)
                yield return null;

            callback?.Invoke();
        }
    }
}
