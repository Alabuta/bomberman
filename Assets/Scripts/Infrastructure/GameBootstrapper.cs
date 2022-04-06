using Infrastructure.States;
using UI;
using UnityEngine;

namespace Infrastructure
{
    public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
    {
        public LoadingScreenController LoadingScreenController; // :TODO: refactor

        private Game _game;

        private void Awake()
        {
            _game = new Game(this, LoadingScreenController);
            _game.GameStateMachine.Enter<BootstrapState>();

            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            _game.GameStateMachine.Update();
        }

        private void FixedUpdate()
        {
            _game.GameStateMachine.FixedUpdate();
        }
    }
}
