using Configs.Game;
using Data;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Tags;
using Infrastructure.Services.PersistentProgress;
using Input;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig PlayerConfig { get; }

        public EcsEntity HeroEntity { get; private set; }
        private Score _score;

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;
        }

        public void ApplyInputAction(World world, PlayerInputAction inputAction)
        {
            OnMove(inputAction.MovementVector);

            if (inputAction.BombPlant)
            {
                ref var transformComponent = ref HeroEntity.Get<TransformComponent>();
                var _ = world.OnPlayerBombPlant(this, transformComponent.WorldPosition); // :TODO: fix?
            }

            if (inputAction.BombBlast)
                world.OnPlayerBombBlast(this);
        }

        public void AttachHero(EcsEntity entity)
        {
            HeroEntity = entity;

            Assert.IsTrue(HeroEntity.Has<TransformComponent>());
            Assert.IsTrue(HeroEntity.Has<HealthComponent>());
            Assert.IsTrue(HeroEntity.Has<EntityComponent>());
            Assert.IsTrue(HeroEntity.Has<HeroTag>());
        }

        public void LoadProgress(PlayerProgress progress)
        {
            _score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = _score;
        }

        private void OnMove(float2 value)
        {
            ref var transformComponent = ref HeroEntity.Get<TransformComponent>();
            ref var movementComponent = ref HeroEntity.Get<MovementComponent>();
            ref var entityComponent = ref HeroEntity.Get<EntityComponent>();

            if (math.lengthsq(value) > 0)
            {
                transformComponent.Direction = (int2) math.round(value);
                movementComponent.Speed = entityComponent.InitialSpeed * entityComponent.SpeedMultiplier;
            }
            else
                movementComponent.Speed = fix.zero;
        }
    }
}
