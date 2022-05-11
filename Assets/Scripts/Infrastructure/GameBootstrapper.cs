using Infrastructure.States;
using UI;
using UnityEngine;

namespace Infrastructure
{
    public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
    {
        public LoadingScreenController LoadingScreenController; // :TODO: refactor

        private Game _game;

        private async void Awake()
        {
            DontDestroyOnLoad(this);

            _game = new Game(this, LoadingScreenController);
            await _game.GameStateMachine.Enter<BootstrapState>();
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
