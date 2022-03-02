using System.Collections.Generic;
using Configs;
using Configs.Behaviours;
using Configs.Entity;
using Entity;
using Entity.Behaviours;
using Entity.Enemies;
using Entity.Hero;
using Game;
using Infrastructure.Services;
using Infrastructure.Services.PersistentProgress;
using Input;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.Factory
{
    public interface IGameFactory : IService
    {
        [CanBeNull]
        IPlayer CreatePlayer(PlayerConfig playerConfig);

        [CanBeNull]
        IPlayerInput CreatePlayerInputHolder(PlayerConfig playerConfig, int playerIndex);

        Hero CreateHero(HeroConfig heroConfig, HeroController entityController);

        Enemy CreateEnemy(EnemyConfig enemyConfig, EnemyController entityController);

        [CanBeNull]
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }

        List<ISavedProgressWriter> ProgressWriters { get; }

        BehaviourAgent CreateEntityBehaviourAgent(BehaviourConfig behaviourConfig, IEntity entity);
    }
}
