using System.Collections.Generic;
using Leopotam.Ecs;

namespace Game.Components.Events
{
    public readonly struct CollisionEnterEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public CollisionEnterEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }

    public readonly struct CollisionExitEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public CollisionExitEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }

    public readonly struct CollisionStayEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public CollisionStayEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }
}
