using System.Collections.Generic;
using System.Linq;
using Game.Behaviours;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Level
{
    public partial class World
    {
        private readonly EcsWorld _ecsWorld;
        private readonly EcsSystems _ecsSystems;
        private readonly EcsSystems _ecsFixedSystems;

        private readonly int _tickRate;
        private readonly fix _fixedDeltaTime;

        private fix _simulationStartTime;
        private fix _simulationCurrentTime;

        private fix _timeRemainder;

        public ulong Tick { get; private set; }

        public fix FixedDeltaTime => _fixedDeltaTime;

        public void StartSimulation()
        {
            _timeRemainder = fix.zero;
            Tick = 0;

            _simulationStartTime = (fix) Time.fixedDeltaTime;
        }

        public void UpdateWorldView()
        {
            _ecsSystems.Run();

            /*foreach (var (_, player) in _players)
                player.Hero.EntityController.WorldPosition = player.Hero.WorldPosition;*/

            /*foreach (var enemy in _enemies)
                enemy.EntityController.WorldPosition = enemy.WorldPosition;*/
        }

        public void UpdateWorldModel()
        {
            /*var heroes = Players.Values.Select(p => p.HeroEntity).ToArray();
            var gameContext = new GameContext2(this, LevelModel, heroes);*/

            var deltaTime = (fix) Time.fixedDeltaTime + _timeRemainder;

            var targetTick = Tick + (ulong) ((fix) _tickRate * deltaTime);
            var tickCounts = targetTick - Tick;
            while (Tick < targetTick)
            {
                ProcessPlayersInput();

                _ecsFixedSystems.Run();

                // UpdateHeroesPositions();
                // ResolveHeroesCollisions();

                // UpdateBehaviourAgents(gameContext);
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

        /*private void UpdateHeroesPositions()
        {
            foreach (var (_, player) in _players)
            {
                var playerHero = player.Hero;
                playerHero.UpdatePosition(_fixedDeltaTime);
            }
        }*/

        /*private void ResolveHeroesCollisions()
        {
            foreach (var (_, player) in _players)
            {
                var playerHero = player.HeroEntity;

                if (!playerHero.Has<HasColliderTag>())
                    continue;

                ref var transformComponent = ref playerHero.Get<TransformComponent>();

                playerHero.Get<ColliderComponent>();

                var heroPosition = transformComponent.WorldPosition;
                var heroTileCoordinate = LevelModel.ToTileCoordinate(heroPosition);

                var neighborTiles = LevelModel
                    .GetNeighborTiles(heroTileCoordinate)
                    .Where(t => t.TileLoad?.Components?.Any(c => c is ColliderComponent2) ?? false);

                foreach (var neighborTile in neighborTiles)
                {
                    if (!neighborTile.TileLoad.TryGetComponent<ColliderComponent2>(out var tileCollider))
                        continue;

                    if ((heroCollider.InteractionLayerMask & neighborTile.TileLoad.LayerMask) == 0)
                        continue;

                    var tilePosition = neighborTile.WorldPosition;

                    var isIntersected = IntersectionPoint(heroCollider, heroPosition, tileCollider, tilePosition,
                        out var intersectionPoint);

                    if (!isIntersected)
                        continue;

                    var travelledPath = transformComponent.Speed / (fix2) _tickRate;
                    var prevPosition = heroPosition - (fix2) transformComponent.Direction * travelledPath;

                    var prevDistance = fix2.distance(prevPosition, tilePosition);
                    var r = new fix(0.49);
                    var R = playerHero.ColliderRadius;
                    var minDistance = r + R; // :TODO: use actual radius playerHero.ColliderRadius
                    if (minDistance < prevDistance)
                    {
                        var vector = fix2.normalize_safe(heroPosition - intersectionPoint, fix2.zero);
                        transformComponent.WorldPosition = intersectionPoint + vector * R; // :TODO: use actual radius
                    }
                }
            }
        }*/

        /*private static bool IntersectionPoint(ColliderComponent2 colliderA, fix2 centerA, Component colliderB, fix2 centerB, :TODO: fix
            out fix2 intersection)
        {
            intersection = default;

            return colliderA switch
            {
                CircleColliderComponent2 circleCollider => circleCollider.CircleIntersectionPoint(centerA, colliderB, centerB,
                    out intersection),
                BoxColliderComponent2 boxCollider => boxCollider.BoxIntersectionPoint(centerA, colliderB, centerB,
                    out intersection),
                _ => false
            };
        }*/

        private void UpdateBehaviourAgents(GameContext2 gameContext)
        {
            foreach (var (entity, behaviourAgents) in _behaviourAgents)
            {
                foreach (var behaviourAgent in behaviourAgents)
                    behaviourAgent.Update(gameContext, entity, _fixedDeltaTime);
            }
        }

        private static IEnumerable<int2[]> GetBombBlastTileLines(int2[] blastDirections, int blastRadius,
            int2 blastCoordinate)
        {
            return blastDirections
                .Select(blastDirection =>
                {
                    return Enumerable
                        .Range(1, blastRadius)
                        .Select(offset => blastCoordinate + blastDirection * offset)
                        .ToArray();
                })
                .Append(new[] { blastCoordinate });
        }
    }
}
