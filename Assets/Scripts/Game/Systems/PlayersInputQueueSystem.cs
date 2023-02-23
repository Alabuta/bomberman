using System.Collections.Generic;
using App;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public class PlayersInputQueueSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IInputService _inputService;

        private readonly List<PlayerInputAction> _playersInputActions = new();

        public void RegisterPlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnInputActionEvent += OnPlayerInputAction;
        }

        public void UnregisterPlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnInputActionEvent -= OnPlayerInputAction;
        }

        private void OnPlayerInputAction(PlayerInputAction inputAction)
        {
            _playersInputActions.Add(inputAction);
        }

        public void Run()
        {
            using var _ = Profiling.PlayersInputProcess.Auto();

            foreach (var action in _playersInputActions)
            {
                if (!_inputService.TryGetRegisteredPlayerTag(action.PlayerInputProvider, out var playerTag))
                    continue;

                if (!_world.Players.TryGetValue(playerTag, out var player))
                    continue;

                player.ApplyInputAction(_world, action);
            }

            _playersInputActions.Clear();
        }
    }
}
