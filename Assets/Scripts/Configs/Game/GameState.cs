using Core;
using UnityEngine;

namespace Configs.Game
{
    public class GameState : ScriptableObjectSingleton<GameState>
    {
        private int _highScore;
        private int _currentScore;

        private int _healths;
    }
}
