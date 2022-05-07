using System;
using Infrastructure;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        public void Load(string sceneName, Action callback)
        {
            LoadScene(sceneName, callback);
        }

        private static async void LoadScene(string sceneName, Action callback)
        {
            if (SceneManager.GetActiveScene().name == sceneName)
            {
                callback?.Invoke();
                return;
            }

            var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                return;

            callback?.Invoke();
        }
    }
}
