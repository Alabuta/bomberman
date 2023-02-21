using System.Collections.Generic;
using App;
using Configs.Entity;
using Configs.Game;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Events;
using Game.Components.Tags;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public class PlayersInputQueueSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly IInputService _inputService;

        private readonly Dictionary<IPlayerInputProvider, PlayerTagConfig> _playerInputProviders = new();
        private readonly Dictionary<ulong, List<(PlayerTagConfig playerTag, PlayerInputAction inputAction)>>
            _playersInputActions = new();

        public void RegisterPlayerInputProvider(IPlayer player, IPlayerInputProvider playerInputProvider)
        {
            _playerInputProviders.Add(playerInputProvider, player.PlayerConfig.PlayerTagConfig);
            playerInputProvider.OnInputActionEvent += OnPlayerInputAction;
        }

        public void UnregisterPlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnInputActionEvent -= OnPlayerInputAction;
            _playerInputProviders.Remove(playerInputProvider);
        }

        public void OnPlayerInputAction(PlayerInputAction inputActions)
        {
            if (!_world.Players.TryGetValue(playerTag, out var player))
                continue;

            if (!_playersInputActions.ContainsKey(_world.Tick))
                _playersInputActions.Add(_world.Tick, new());

            _playersInputActions[_world.Tick].Add((null, inputActions));
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
                foreach (var action in playerInputActions)
                {
                    var playerTag = _playerInputProviders[action.PlayerInputProvider];
                    if (!_world.Players.TryGetValue(playerTag, out var player))
                        continue;

                    ApplyInputAction(_world, action);
                    player.ApplyInputAction(_world, action);
                }
            }

            _playersInputActions.Remove(_world.Tick);
        }

        public void ApplyInputAction(World world, PlayerInputAction inputAction, IPlayer player)
        {
            // OnMove(inputAction.MovementVector);

            /*if (inputAction.BombPlant)
            {
                ref var transformComponent = ref HeroEntity.Get<TransformComponent>();
                var _ = world.OnPlayerBombPlant(this, transformComponent.WorldPosition); // :TODO: fix?
            }*/

            if (inputAction.BombBlast)
                world.OnPlayerBombBlast(player);
        }
    }
}
