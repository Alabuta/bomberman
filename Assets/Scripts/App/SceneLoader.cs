using System;
using System.Collections;
using Infrastructure;
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
