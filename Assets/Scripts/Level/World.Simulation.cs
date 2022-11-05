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

        private fix _simulationStartTime;
        private fix _simulationCurrentTime;

        private fix _timeRemainder;

        public ulong Tick { get; private set; }
        public int TickRate { get; }
        public fix FixedDeltaTime { get; }

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
            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) TickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                ProcessPlayersInput();

                _ecsFixedSystems.Run();

                ++Tick;
            }

            _timeRemainder = fix.max(fix.zero, deltaTime - (fix) tickCounts / (fix) TickRate);

            _simulationCurrentTime += deltaTime - _timeRemainder;
        }

        private void ProcessPlayersInput()
        {
            if (_playersInputActions.TryGetValue(Tick, out var playerInputActions))
            {
                foreach (var inputAction in playerInputActions)
                {
                    var player = _playerInputs[inputAction.PlayerInput];
                    player.ApplyInputAction(this, inputAction);
                }
            }

            _playersInputActions.Remove(Tick);
        }
    }
}
