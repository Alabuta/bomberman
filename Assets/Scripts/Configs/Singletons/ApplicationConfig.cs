using Configs.Game;
using Core;
using UnityEngine;

namespace Configs.Singletons
{
    [CreateAssetMenu(fileName = "Application", menuName = "Configs/Singletons/Application")]
    public sealed class ApplicationConfig : ScriptableObjectSingleton<ApplicationConfig>
    {
        [Header("General Parameters"), Range(-1, 300)]
        public int TargetFrameRate = 30;
        public int TickRate = 31;

        public bool EnableVSync;

        public PlayerTagConfig DefaultPlayerTag;

        [Header("Game Modes")]
        public GameModePvEConfig GameModePvE;
        public GameModePvPConfig GameModePvP;
    }
}
