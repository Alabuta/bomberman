using System.Linq;
using Entity.Behaviours;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public partial class World
    {
        private const int TickRate = 60;

        private readonly fix _fixedDeltaTime = fix.one / (fix) TickRate;

        private fix _simulationStartTime;
        private fix _simulationCurrentTime;

        private fix _timeRemainder;

        public ulong Tick { get; private set; }

        public void StartSimulation()
        {
            _timeRemainder = fix.zero;
            Tick = 0;

            _simulationStartTime = (fix) Time.timeAsDouble;
        }

        public void UpdateSimulation()
        {
            var heroes = Players.Values.Select(p => p.Hero).ToArray();
            var gameContext = new GameContext(LevelGridModel, heroes);

            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) TickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                if (_playerInputActions.TryGetValue(Tick, out var playerInputActions))
                {
                    foreach (var inputAction in playerInputActions)
                    {
                        var player = _playerInputs[inputAction.PlayerInput];
                        player.ApplyInputAction(inputAction);
                    }
                }

                _playerInputActions.Remove(Tick);

                foreach (var (_, player) in _players)
                {
                    player.Hero.UpdatePosition(_fixedDeltaTime);

                    var heroWorldPosition = player.Hero.WorldPosition;
                    var tileCoordinate = LevelGridModel.ToTileCoordinate(heroWorldPosition);
                    var minDistance = player.Hero.ColliderRadius + new fix(.5);
                    var overlappedTiles = new[]
                    {
                        new int2(1, 1),
                        new int2(1, -1),
                        new int2(-1, -1),
                        new int2(-1, 1),

                        new int2(0, 1),
                        new int2(1, 0),
                        new int2(0, -1),
                        new int2(-1, 0)
                    };
                    var overlappedTiles2 = overlappedTiles
                            .Select(d => tileCoordinate + d)
                            .Where(LevelGridModel.IsCoordinateInField)
                            .Select(c => LevelGridModel[c])
                            .Where(t => t.Type != LevelTileType.FloorTile)
                        /*.FirstOrDefault(t => fix2.distance(t.WorldPosition, heroWorldPosition) < minDistance)*/;
                    /*if (overlappedTile != null)
                    {
                        var vector = fix2.normalize(player.Hero.WorldPosition - overlappedTile.WorldPosition);
                        player.Hero.WorldPosition = overlappedTile.WorldPosition + vector * minDistance;
                    }*/
                    foreach (var overlappedTile in overlappedTiles2)
                    {
                        if (fix2.distance(overlappedTile.WorldPosition, heroWorldPosition) < minDistance)
                        {
                            var vector = fix2.normalize(overlappedTile.WorldPosition - player.Hero.WorldPosition);
                            player.Hero.WorldPosition = overlappedTile.WorldPosition - vector * minDistance;
                        }
                    }
                    /*if (LevelGridModel[tileCoordinate].Type != LevelTileType.FloorTile)
                    {
                        player.Hero.WorldPosition = LevelGridModel.ToWorldPosition(tileCoordinate - player.Hero.Direction);
                    }*/

                    /*if (math.any(tileCoordinate < int2.zero) || math.any(tileCoordinate >= LevelGridModel.Size))
                    {
                        player.Hero.WorldPosition = LevelGridModel.ToWorldPosition(tileCoordinate - player.Hero.Direction);
                    }*/
                }

                foreach (var (entity, behaviourAgents) in _behaviourAgents)
                {
                    foreach (var behaviourAgent in behaviourAgents)
                        behaviourAgent.Update(gameContext, entity, _fixedDeltaTime);
                }

                ++Tick;
            }

            _timeRemainder = fix.max(fix.zero, deltaTime - (fix) tickCounts / (fix) TickRate);

            _simulationCurrentTime += deltaTime - _timeRemainder;
        }

        public void UpdateView()
        {
            foreach (var (_, player) in _players)
                player.Hero.EntityController.WorldPosition = player.Hero.WorldPosition;

            foreach (var enemy in _enemies)
                enemy.EntityController.WorldPosition = enemy.WorldPosition;
        }
    }
}
