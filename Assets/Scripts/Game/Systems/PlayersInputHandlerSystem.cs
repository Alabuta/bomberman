using System.Collections.Generic;
using System.Threading.Tasks;
using App;
using Game.Components;
using Game.Components.Entities;
using Infrastructure.Services.Input;
using Input;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public struct PlayerInputAction
    {
        public float2 MovementVector;
        public bool BombPlant;
        public bool BombBlast;
    }

    public class PlayersInputHandlerSystem : IEcsRunSystem
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

        public void UnsubscribePlayerInputProvider(IPlayerInputProvider playerInputProvider)
        {
            playerInputProvider.OnBombBlastActionEvent -= OnBombBlastAction;
            playerInputProvider.OnBombPlantActionEvent -= OnBombPlantAction;
            playerInputProvider.OnMoveActionEvent -= OnMoveAction;
        }

        public void Run()
        {
            using var _ = Profiling.PlayersInputProcess.Auto();

            foreach (var (inputProvider, inputAction) in _playersInputActions)
            {
                if (!_inputService.TryGetRegisteredPlayerTag(inputProvider, out var playerTag))
                {
                    UnsubscribePlayerInputProvider(inputProvider);
                    continue;
                }

                if (!_world.Players.TryGetValue(playerTag, out var player))
                    continue;

                // :TODO: check whether player is active
                ApplyInputAction(_world, player, in inputAction);
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

        private static void ApplyInputAction(World world, IPlayer player, in PlayerInputAction inputAction)
        {
            if (inputAction.BombPlant)
                world.CreatePlayerBombPlantAction(player);
            else if (inputAction.BombBlast)
                world.CreatePlayerBombBlastAction(player);
            else
                ApplyMoveAction(world, player, inputAction.MovementVector);
        }

        private static void ApplyMoveAction(World world, IPlayer player, float2 value)
        {
            var heroEntity = player.HeroEntity;
            Assert.IsTrue(heroEntity.Has<EntityComponent>());
            Assert.IsTrue(heroEntity.Has<TransformComponent>());
            Assert.IsTrue(heroEntity.Has<MovementComponent>());

            // :TODO: refactor - replace by MovementComponent
            ref var transformComponent = ref heroEntity.Get<TransformComponent>();
            // ref var movementComponent = ref heroEntity.Get<MovementComponent>();
            ref var entityComponent = ref heroEntity.Get<EntityComponent>();

            var hasMovement = math.lengthsq(value) > 0;
            if (hasMovement)
                transformComponent.Direction = (int2) math.round(value);

            heroEntity.Replace(new MovementComponent
            {
                Speed = fix.select(fix.zero, entityComponent.InitialSpeed * entityComponent.SpeedMultiplier, hasMovement)
            });
        }
    }
}
