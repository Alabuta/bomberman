using System.Collections.Generic;
using System.Threading.Tasks;
using Configs;
using Configs.Behaviours;
using Configs.Entity;
using Configs.Items;
using Game;
using Game.Enemies;
using Game.Hero;
using Game.Items;
using Infrastructure.Services;
using Infrastructure.Services.PersistentProgress;
using Input;
using Items;
using JetBrains.Annotations;
using Leopotam.Ecs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

        BombItem CreateItem(BombItemConfig bobItemConfig, ItemController controller);

        GameObject InstantiatePrefab(GameObject prefab, float3 position, Transform parent = null);

        Task<GameObject> InstantiatePrefabAsync(AssetReferenceGameObject reference,
            float3 position,
            Transform parent = null);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }

        List<ISavedProgressWriter> ProgressWriters { get; }

        Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<AssetReference> references);

        Task<T> LoadAssetAsync<T>(AssetReference reference);

        void AddBehaviourComponents(IEnumerable<BehaviourConfig> behaviourConfigs, EcsEntity entity);
    }
}
