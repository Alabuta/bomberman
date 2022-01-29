using Configs.Entity;
using Infrastructure.Services;
using Unity.Mathematics;
using UnityEngine;

namespace Infrastructure.Factory
{
    public interface IGameFactory : IService
    {
        GameObject SpawnEntity(EntityConfig heroConfig, float3 position);
    }
}
