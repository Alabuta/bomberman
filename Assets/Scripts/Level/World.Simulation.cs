using System.Linq;
using Game.Behaviours;
using Game.Colliders;
using Math.FixedPointMath;
using UnityEngine;
using Component = Game.Components.Component;

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
            var gameContext = new GameContext(LevelModel, heroes);

            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) TickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                ProcessPlayersInput();

                UpdateCollisions();

                UpdateBehaviourAgents(gameContext);

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

        private void ProcessPlayersInput()
        {
            if (_playersInputActions.TryGetValue(Tick, out var playerInputActions))
            {
                foreach (var inputAction in playerInputActions)
                {
                    var player = _playerInputs[inputAction.PlayerInput];
                    player.ApplyInputAction(inputAction);

                    if (inputAction.BombPlant)
                        OnPlayerBombPlant(player, player.Hero.WorldPosition);

                    if (inputAction.BombBlast)
                        OnPlayerBombBlast(player);
                }
            }

            _playersInputActions.Remove(Tick);
        }

        private void UpdateCollisions()
        {
            var radius = LevelModel.TileInnerRadius;

            foreach (var (_, player) in _players)
            {
                var heroCollider = player.Hero.Components.FirstOrDefault(c => c is ColliderComponent) as ColliderComponent;
                player.Hero.UpdatePosition(_fixedDeltaTime);

                var heroPosition = player.Hero.WorldPosition;
                var heroTileCoordinate = LevelModel.ToTileCoordinate(heroPosition);

                var neighborTiles = LevelModel
                    .GetNeighborTiles(heroTileCoordinate)
                    .Where(t => t.TileLoad?.Components?.Any(c => c is ColliderComponent) ?? false);

                foreach (var neighborTile in neighborTiles)
                {
                    var tileCollider = neighborTile.TileLoad.Components.FirstOrDefault(c => c is ColliderComponent);
                    var tilePosition = neighborTile.WorldPosition;

                    var isIntersected = IntersectionPoint(heroCollider, heroPosition, tileCollider, tilePosition,
                        out var intersectionPoint);

                    if (!isIntersected)
                        continue;

                    var vector = fix2.normalize_safe(heroPosition - intersectionPoint, fix2.zero);
                    Debug.LogWarning(vector);
                    player.Hero.WorldPosition = intersectionPoint + vector * player.Hero.ColliderRadius;
                }
            }
        }

        private static bool IntersectionPoint(ColliderComponent colliderA, fix2 centerA, Component colliderB, fix2 centerB,
            out fix2 intersection)
        {
            intersection = default;

            return colliderA switch
            {
                CircleColliderComponent circleCollider => circleCollider.CircleIntersectionPoint(centerA, colliderB, centerB,
                    out intersection),
                BoxColliderComponent boxCollider => boxCollider.BoxIntersectionPoint(centerA, colliderB, centerB,
                    out intersection),
                _ => false
            };
        }

        private void UpdateBehaviourAgents(GameContext gameContext)
        {
            foreach (var (entity, behaviourAgents) in _behaviourAgents)
            {
                foreach (var behaviourAgent in behaviourAgents)
                    behaviourAgent.Update(gameContext, entity, _fixedDeltaTime);
            }
        }
    }
}
