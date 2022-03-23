using System.Linq;
using Entity.Behaviours;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public partial class World
    {
        private const int TickRate = 60;

        private double _simulationStartTime;
        private double _simulationCurrentTime;

        private double _timeRemainder;
        private ulong _tick;

        public void StartSimulation()
        {
            _timeRemainder = 0;
            _tick = 0;

            _simulationStartTime = Time.timeAsDouble;
        }

        public void UpdateSimulation()
        {
            var heroes = Players.Values.Select(p => p.Hero).ToArray();
            var gameContext = new GameContext(LevelGridModel, heroes);

            var deltaTime = Time.fixedDeltaTime + _timeRemainder;

            var targetTick = _tick + (ulong) (TickRate * deltaTime);
            var tickCounts = targetTick - _tick;
            while (_tick < targetTick)
            {
                foreach (var (entity, behaviourAgents) in _behaviourAgents)
                {
                    foreach (var behaviourAgent in behaviourAgents)
                        behaviourAgent.Update(gameContext, entity);
                }

                ++_tick;
            }

            _timeRemainder = math.max(0, deltaTime - tickCounts / (double) TickRate);

            _simulationCurrentTime += deltaTime - _timeRemainder;
        }
    }
}
