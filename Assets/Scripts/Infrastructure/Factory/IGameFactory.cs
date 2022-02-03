using System.Collections.Generic;
using Configs.Entity;
using Infrastructure.Services;
using Services.PersistentProgress;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.Factory
{
    public interface IGameFactory : IService
    {
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);

        void CleanUp();

        List<ISavedProgressReader> ProgressReaders { get; }
        List<ISavedProgressWriter> ProgressWriters { get; }
    }
}
