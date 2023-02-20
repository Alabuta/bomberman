using System.Collections.Generic;
using App;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public class PlayersInputProcessSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IInputService _inputService;

        private readonly Dictionary<IPlayerInputProvider, IPlayer> _playerInputProviders = new();
        private readonly Dictionary<ulong, List<PlayerInputAction>> _playersInputActions = new();

        private readonly Dictionary<IPlayer, Queue<EcsEntity>> _playerBombs = new();

        public void RegisterPlayerInputProvider(IPlayer player, IPlayerInputProvider playerInputProvider)
        {
            _playerInputProviders.Add(playerInputProvider, player);
            playerInputProvider.OnInputActionEvent += OnPlayerInputAction;
        }

        public void UnregisterPlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnInputActionEvent -= OnPlayerInputAction;
            _playerInputProviders.Remove(playerInputProvider);
        }

        public void OnPlayerInputAction(PlayerInputAction inputActions)
        {
            if (!_playersInputActions.ContainsKey(_world.Tick))
                _playersInputActions.Add(_world.Tick, new List<PlayerInputAction>());

            _playersInputActions[_world.Tick].Add(inputActions);
        }

        public void Run()
        {
            ProcessPlayersInput();
        }

        private void ProcessPlayersInput()
        {
            using var _ = Profiling.PlayersInputProcess.Auto();

            if (_playersInputActions.TryGetValue(_world.Tick, out var playerInputActions))
            {
                foreach (var inputAction in playerInputActions)
                {
                    var player = _playerInputProviders[inputAction.PlayerInputProvider];
                    player.ApplyInputAction(_world, inputAction);
                }
            }

            _playersInputActions.Remove(_world.Tick);
        }
    }
}
