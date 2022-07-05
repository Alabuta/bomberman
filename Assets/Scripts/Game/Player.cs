﻿using Configs;
using Data;
using Game.Components;
using Game.Components.Entities;
using Infrastructure.Services.PersistentProgress;
using Input;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game
{
    public struct PlayerComponent
    {
        public PlayerConfig Config { get; }
    }

    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig PlayerConfig { get; }

        // public Hero.Hero Hero { get; private set; }
        public EcsEntity HeroEntity { get; private set; }
        private Score _score;

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;
        }

        public void ApplyInputAction(World world, PlayerInputAction inputAction)
        {
            ref var healthComponent = ref HeroEntity.Get<HealthComponent>();
            if (healthComponent.CurrentHealth < 1) // :TODO: refactor
                return;

            OnMove(inputAction.MovementVector);

            if (inputAction.BombPlant)
            {
                ref var transformComponent = ref HeroEntity.Get<TransformComponent>();
                world.OnPlayerBombPlant(this, transformComponent.WorldPosition);
            }

            if (inputAction.BombBlast)
                world.OnPlayerBombBlast(this);
        }

        /*
        public void AttachHero(Hero.Hero hero)
        {
            Hero = hero;
            Hero.DeathEvent += OnHeroDeath;
        }*/

        public void AttachHero(EcsEntity entity)
        {
            HeroEntity = entity;
            Assert.IsTrue(entity.Has<TransformComponent>());
            Assert.IsTrue(entity.Has<HealthComponent>());
            Assert.IsTrue(entity.Has<EntityComponent>());
            Assert.IsTrue(entity.Has<HeroTag>());

            // Hero.DeathEvent += OnHeroDeath; :TODO:
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
            ref var entityComponent = ref HeroEntity.Get<EntityComponent>();

            if (math.lengthsq(value) > 0)
            {
                transformComponent.Direction = (int2) math.round(value);
                transformComponent.Speed = entityComponent.InitialSpeed * entityComponent.SpeedMultiplier;
            }
            else
                transformComponent.Speed = fix.zero;
        }

        /*private void OnHeroDeath(IEntity entity)
        {
            Hero.DeathEvent -= OnHeroDeath;
        }*/
    }
}
