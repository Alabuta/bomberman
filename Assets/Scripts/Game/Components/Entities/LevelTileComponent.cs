using System.Collections.Generic;
using Leopotam.Ecs;
using Level;

namespace Game.Components.Entities
{
    public struct LevelTileComponent
    {
        public LevelTileType Type;
        public HashSet<EcsEntity> EntitiesHolder;
    }
}
