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
        private readonly int _tickRate;
        private readonly fix _fixedDeltaTime;

        private fix _simulationStartTime;
        private fix _simulationCurrentTime;

        private fix _timeRemainder;

        public ulong Tick { get; private set; }

        public void StartSimulation()
        {
            _timeRemainder = fix.zero;
            Tick = 0;

            _simulationStartTime = (fix) Time.fixedDeltaTime;
        }

        public void UpdateWorldView()
        {
            foreach (var (_, player) in _players)
                player.Hero.EntityController.WorldPosition = player.Hero.WorldPosition;

            foreach (var enemy in _enemies)
                enemy.EntityController.WorldPosition = enemy.WorldPosition;
        }

        public void UpdateWorldModel()
        {
            var heroes = Players.Values.Select(p => p.Hero).ToArray();
            var gameContext = new GameContext(LevelModel, heroes);

            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) _tickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                ProcessPlayersInput();

                UpdateHeroesPositions();
                ResolveHeroesCollisions();

                UpdateBehaviourAgents(gameContext);

                ++Tick;
            }

            _timeRemainder = fix.max(fix.zero, deltaTime - (fix) tickCounts / (fix) _tickRate);

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

        private void UpdateHeroesPositions()
        {
            foreach (var (_, player) in _players)
            {
                var playerHero = player.Hero;
                playerHero.UpdatePosition(_fixedDeltaTime);
            }
        }

        private void ResolveHeroesCollisions()
        {
            foreach (var (_, player) in _players)
            {
                var playerHero = player.Hero;

                if (!playerHero.TryGetComponent<ColliderComponent>(out var heroCollider))
                    continue;

                var heroPosition = playerHero.WorldPosition;
                var heroTileCoordinate = LevelModel.ToTileCoordinate(heroPosition);

                var neighborTiles = LevelModel
                    .GetNeighborTiles(heroTileCoordinate)
                    .Where(t => t.TileLoad?.Components?.Any(c => c is ColliderComponent) ?? false);

                foreach (var neighborTile in neighborTiles)
                {
                    if (!neighborTile.TileLoad.TryGetComponent<ColliderComponent>(out var tileCollider))
                        continue;

                    if ((heroCollider.InteractionLayerMask & neighborTile.TileLoad.LayerMask) == 0)
                        continue;

                    var tilePosition = neighborTile.WorldPosition;

                    var isIntersected = IntersectionPoint(heroCollider, heroPosition, tileCollider, tilePosition,
                        out var intersectionPoint);

                    if (!isIntersected)
                        continue;

                    var travelledPath = playerHero.Speed / (fix2) _tickRate;
                    var prevPosition = heroPosition - (fix2) playerHero.Direction * travelledPath;

                    // fix2.abs(prevPosition - tilePosition);
                    var prevDistance = fix2.distance(prevPosition, tilePosition);
                    var r = new fix(0.5);
                    var minDistance = r + playerHero.ColliderRadius; // :TODO: use actual radius playerHero.ColliderRadius
                    if (minDistance < prevDistance)
                    {
                        /*var theta = travelledPath / r;
                        var v1 = heroPosition - tilePosition;
                        var vector = fix2.normalize_safe(v1, fix2.zero);
                        playerHero.WorldPosition = playerHero.WorldPosition + vector * (minDistance - fix2.length(v1));*/

                        var vector = fix2.normalize_safe(heroPosition - intersectionPoint, fix2.zero);
                        playerHero.WorldPosition =
                            intersectionPoint + vector * (playerHero.ColliderRadius); // :TODO: use actual radius
                    }
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
