using System.Collections.Generic;
using Game.Components.Events;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using UnityEngine.Assertions;

namespace Game.Systems
{
    // :TODO: rename
    public sealed class BombsProcessSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<BombTag, IsKinematicTag, OnCollisionEnterEventComponent> _onEnterFilter;
        private readonly EcsFilter<BombTag, IsKinematicTag, OnCollisionExitEventComponent> _onExitFilter;

        private readonly Dictionary<EcsEntity, int> _collisionsCountPerBomb = new();

        public void Run()
        {
            foreach (var index in _onEnterFilter)
            {
                var bombEntity = _onEnterFilter.GetEntity(index);

                ref var eventComponent = ref _onEnterFilter.Get3(index);

                if (!_collisionsCountPerBomb.ContainsKey(bombEntity))
                    _collisionsCountPerBomb[bombEntity] = 0;

                _collisionsCountPerBomb[bombEntity] += eventComponent.Entities.Count;
            }

            foreach (var index in _onExitFilter)
            {
                var bombEntity = _onExitFilter.GetEntity(index);

                if (!_collisionsCountPerBomb.TryGetValue(bombEntity, out var count))
                {
                    bombEntity.Del<IsKinematicTag>();
                    continue;
                }

                ref var eventComponent = ref _onExitFilter.Get3(index);
                count -= eventComponent.Entities.Count;
                Assert.IsTrue(count > -1);

                if (count > 0)
                {
                    _collisionsCountPerBomb[bombEntity] = count;
                    continue;
                }

                bombEntity.Del<IsKinematicTag>();

                _collisionsCountPerBomb.Remove(bombEntity);
            }
        }
    }
}
