using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Infrastructure.Factory;
using Infrastructure.Services;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.Services.SaveLoad;
using UI;

namespace Infrastructure.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;
        private IExitableState _activeState;

        public Action UpdateCallback;
        public Action FixedUpdateCallback;

        public GameStateMachine(ServiceLocator serviceLocator, LoadingScreenController loadingScreenController)
        {
            _states = new Dictionary<Type, IExitableState>
            {
                [typeof(BootstrapState)] = new BootstrapState(this, serviceLocator),
                [typeof(LoadLevelState)] = new LoadLevelState(this, serviceLocator.Single<IGameFactory>(),
                    serviceLocator.Single<IInputService>(), serviceLocator.Single<IPersistentProgressService>(),
                    loadingScreenController),
                [typeof(LoadProgressState)] = new LoadProgressState(this, serviceLocator.Single<IPersistentProgressService>(),
                    serviceLocator.Single<ISaveLoadService>()),
                [typeof(GameLoopState)] = new GameLoopState(this)
            };
        }

        public async Task Enter<TState>() where TState : class, IGameState
        {
            var newState = ChangeState<TState>();
            await newState.Enter();
        }

        public async Task Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            var newState = ChangeState<TState>();
            await newState.Enter(payload);
        }

        private TState ChangeState<TState>() where TState : class, IExitableState
        {
            _activeState?.Exit();

            var newState = GetState<TState>();
            _activeState = newState;

            return newState;
        }

        private TState GetState<TState>() where TState : class, IExitableState =>
            _states[typeof(TState)] as TState;

        public void Update()
        {
            UpdateCallback?.Invoke();
        }

        public void FixedUpdate()
        {
            FixedUpdateCallback?.Invoke();
        }
    }
}
