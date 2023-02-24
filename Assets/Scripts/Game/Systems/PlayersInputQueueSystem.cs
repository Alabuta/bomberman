using System.Collections.Generic;
using App;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Level;
using Unity.Mathematics;

namespace Game.Systems
{
    public struct PlayerInputAction
    {
        public float2 MovementVector;
        public bool BombPlant;
        public bool BombBlast;
    }

    public class PlayersInputQueueSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IInputService _inputService;

        private readonly List<(IPlayerInputProvider inputProvider, PlayerInputAction inputAction)> _playersInputActions = new();

        public void SubscribeToPlayerInputActions(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnMoveActionEvent += OnMoveAction;
            playerInputProvider.OnBombPlantActionEvent += OnBombPlantAction;
            playerInputProvider.OnBombBlastActionEvent += OnBombBlastAction;
        }

        public void UnregisterPlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnMoveActionEvent -= OnMoveAction;
            playerInputProvider.OnBombPlantActionEvent -= OnBombPlantAction;
            playerInputProvider.OnBombBlastActionEvent -= OnBombBlastAction;
        }

        public void Run()
        {
            using var _ = Profiling.PlayersInputProcess.Auto();

            foreach (var (provider, action) in _playersInputActions)
            {
                if (!_inputService.TryGetRegisteredPlayerTag(provider, out var playerTag))
                    continue;

                if (!_world.Players.TryGetValue(playerTag, out var player))
                    continue;

                // :TODO: check whether player is active
                player.ApplyInputAction(_world, action);
            }

            _playersInputActions.Clear();
        }

        private void OnMoveAction(IPlayerInputProvider inputProvider, float2 movementVector)
        {
            _playersInputActions.Add((inputProvider, new PlayerInputAction { MovementVector = movementVector }));
        }

        private void OnBombPlantAction(IPlayerInputProvider inputProvider)
        {
            _playersInputActions.Add((inputProvider, new PlayerInputAction { BombPlant = true }));
        }

        private void OnBombBlastAction(IPlayerInputProvider inputProvider)
        {
            _playersInputActions.Add((inputProvider, new PlayerInputAction { BombBlast = true }));
        }
    }
}
