using Infrastructure.States;
using UnityEngine;

namespace Infrastructure
{
    public class GameBootstrapper : MonoBehaviour, ICoroutineRunner
    {
        private Game _game;

        private void Awake()
        {
            _game = new Game(this);
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
