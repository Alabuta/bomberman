using System.Collections.Generic;
using Leopotam.Ecs;

namespace Game.Components.Events
{
    public readonly struct OnCollisionEnterEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public OnCollisionEnterEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }

    public readonly struct OnCollisionExitEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public OnCollisionExitEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }

    public readonly struct OnCollisionStayEventComponent
    {
        public readonly HashSet<EcsEntity> Entities;

        public OnCollisionStayEventComponent(HashSet<EcsEntity> entities)
        {
            Entities = entities;
        }
    }
}
