using App;
using Game.Components;
using Leopotam.Ecs;
using Level;

namespace Game.Systems
{
    public class BeforeSimulationStepSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent> _transformFilter;

        public void Run()
        {
            using var _ = Profiling.BeforeSimulationStep.Auto();

            foreach (var entityIndex in _transformFilter)
            {
                var entity = _transformFilter.GetEntity(entityIndex);
                ref var transformComponent = ref _transformFilter.Get1(entityIndex);

                ref var prevFrameDataComponent = ref entity.Get<PrevFrameDataComponent>();
                prevFrameDataComponent.LastWorldPosition = transformComponent.WorldPosition;
            }
        }
    }
}
