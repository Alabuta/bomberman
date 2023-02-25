using Configs.Game;
using Data;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Tags;
using Infrastructure.Services.PersistentProgress;
using Leopotam.Ecs;
using UnityEngine.Assertions;

namespace Game
{
    public class Player : IPlayer, ISavedProgressWriter
    {
        public PlayerConfig PlayerConfig { get; }

        public EcsEntity HeroEntity { get; private set; }

        public Score Score { get; private set; }

        public Player(PlayerConfig playerConfig)
        {
            PlayerConfig = playerConfig;
        }

        public void AttachHero(EcsEntity entity)
        {
            Assert.IsTrue(entity.Has<TransformComponent>());
            Assert.IsTrue(entity.Has<HealthComponent>());
            Assert.IsTrue(entity.Has<EntityComponent>());
            Assert.IsTrue(entity.Has<HeroTag>());

            HeroEntity = entity;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            Score = progress.Score;
        }

        public void UpdateProgress(PlayerProgress progress)
        {
            progress.Score = Score;
        }
    }
}
