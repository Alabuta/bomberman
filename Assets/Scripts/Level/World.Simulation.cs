using App;
using Game.Systems.RTree;
using Leopotam.Ecs;
using Math.FixedPointMath;
using UnityEngine;

namespace Level
{
    public partial class World
    {
        private readonly EcsWorld _ecsWorld;
        private readonly EcsSystems _ecsSystems;
        private readonly EcsSystems _ecsFixedSystems;

        private readonly IRTree _entitiesAabbTree;

        private fix _simulationStartTime;
        private fix _simulationCurrentTime;

        private fix _timeRemainder;

        public ulong Tick { get; private set; }
        public int TickRate { get; }
        public fix FixedDeltaTime { get; }

        public IRTree EntitiesAabbTree => _entitiesAabbTree;

        public void StartSimulation()
        {
            _timeRemainder = fix.zero;
            Tick = 0;

            _simulationStartTime = (fix) Time.fixedDeltaTime;
        }

        public void UpdateWorldView()
        {
            _ecsSystems.Run();
        }

        public void UpdateWorldModel()
        {
            using var _ = Profiling.UpdateWorldModel.Auto();

            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) TickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                using ( Profiling.EcsFixedSystemsUpdate.Auto() )
                    _ecsFixedSystems.Run();

                ++Tick;
            }

            _timeRemainder = fix.max(fix.zero, deltaTime - (fix) tickCounts / (fix) TickRate);

            _simulationCurrentTime += deltaTime - _timeRemainder;
        }
    }
}
