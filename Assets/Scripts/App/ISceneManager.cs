using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace App
{
    public enum SceneBuildIndex
    {
        Empty,
        StartScreen,
        // RoundNumberScreen,
        // StageScreen,
        GameLevel
    }

    public interface ISceneManager
    {
        Scene ActiveScene { get; }

        SceneBuildIndex ActiveSceneBuildIndex { get; }

        GameObject ActiveSceneRoot { get; }

        void StartNewGame();

        void LoadScene(SceneBuildIndex sceneBuildIndex, Action action);
    }
}
