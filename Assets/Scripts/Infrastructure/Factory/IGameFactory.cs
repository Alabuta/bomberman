using System.Collections.Generic;
using Configs;
using Configs.Entity;
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
        IPlayer CreatePlayer(PlayerConfig playerConfig, IPlayerInput playerInput);

        [CanBeNull]
        IPlayerInput CreatePlayerInputHolder(PlayerConfig playerConfig, int playerIndex);

        [CanBeNull]
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }

        List<ISavedProgressWriter> ProgressWriters { get; }
    }
}
