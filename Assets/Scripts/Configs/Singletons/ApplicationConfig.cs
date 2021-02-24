using Configs.Game;
using Core;
using UnityEditor;
using UnityEngine;

namespace Configs.Singletons
{
    [CreateAssetMenu(fileName = "ApplicationConfig", menuName = "Configs/Singletons/Application")]
    public sealed class ApplicationConfig : ScriptableObjectSingleton<ApplicationConfig>
    {
        [Header("General Parameters"), Range(-1, 300)]
        public int TargetFrameRate = 30;

        public bool EnableVSync;

        public SceneAsset StartScene;

        [Header("Game Modes")]
        [InspectorName("Game Mode PvE")]
        public GameModePvE GameModePvE;
    }
}
