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
        IPlayerInput CreatePlayerInputHolder(PlayerConfig playerConfig, int playerIndex);

        [CanBeNull]
        IPlayer CreatePlayer(PlayerConfig playerConfig);

        Hero CreateHero(HeroConfig heroConfig, HeroController entityController);

        Enemy CreateEnemy(EnemyConfig enemyConfig, EnemyController entityController);

        IReadOnlyList<IBehaviourAgent>
            CreateEntityBehaviourAgent(IEnumerable<BehaviourConfig> behaviourConfigs, IEntity entity);

        [CanBeNull]
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);

        GameObject InstantiatePrefab(GameObject prefab, float3 position);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }

        List<ISavedProgressWriter> ProgressWriters { get; }
    }
}
