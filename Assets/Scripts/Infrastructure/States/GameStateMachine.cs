using System;
using System.Collections.Generic;
using App;
using Infrastructure.Factory;
using Infrastructure.Services;
using Infrastructure.Services.Input;
using Infrastructure.Services.PersistentProgress;
using Infrastructure.Services.SaveLoad;

namespace Infrastructure.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;
        private IExitableState _activeState;

        public Action UpdateCallback;

        public GameStateMachine(SceneLoader sceneLoader, ServiceLocator serviceLocator)
        {
            _states = new Dictionary<Type, IExitableState>
            {
                [typeof(BootstrapState)] = new BootstrapState(this, sceneLoader, serviceLocator),
                [typeof(LoadLevelState)] = new LoadLevelState(this, sceneLoader, serviceLocator.Single<IGameFactory>(),
                    serviceLocator.Single<IInputService>(), serviceLocator.Single<IPersistentProgressService>()),
                [typeof(LoadProgressState)] = new LoadProgressState(this, serviceLocator.Single<IPersistentProgressService>(),
                    serviceLocator.Single<ISaveLoadService>()),
                [typeof(GameLoopState)] = new GameLoopState(this)
            };
        }

        public void Enter<TState>() where TState : class, IGameState
        {
            var newState = ChangeState<TState>();
            newState.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            var newState = ChangeState<TState>();
            newState.Enter(payload);
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
    }
}
