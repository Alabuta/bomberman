using System.Collections.Generic;
using Leopotam.Ecs;

namespace Game.Components.Events
{
    public struct OnCollisionEnterEventComponent
    {
        public HashSet<EcsEntity> Entities;
    }

    public struct OnCollisionExitEventComponent
    {
        public HashSet<EcsEntity> Entities;
    }

    public struct OnCollisionStayEventComponent
    {
        public HashSet<EcsEntity> Entities;
    }
}
