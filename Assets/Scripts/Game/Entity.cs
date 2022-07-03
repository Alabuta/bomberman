using System;
using System.Linq;
using Configs.Entity;
using Game.Enemies;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using Component = Game.Components.Component;

namespace Game
{
    public struct TransformComponent
    {
        public fix2 WorldPosition;
        public int2 Direction;
        public fix Speed;
    }

    public struct SimpleAttackBehaviourComponent
    {
        public int DamageValue; // :TODO: get damage value from actual entity parameters
    }

    public struct SimpleMovementBehaviourComponent
    {
        public int2[] MovementDirections;
        public bool TryToSelectNewTile;
        public fix DirectionChangeChance;

        public fix2 FromWorldPosition;
        public fix2 ToWorldPosition;
    }

    public struct EnemyComponent
    {
        public EnemyConfig Config;
        public EnemyController Controller;

        public fix HitRadius;
        public fix HurtRadius;

        // public fix CurrentSpeed;
        public fix InitialSpeed;
        public fix SpeedMultiplier;

        public int InteractionLayerMask;
    }

    /*public class EnemyCreateSystem : IEcsInitSystem
    {
        private EcsWorld _ecsWorld;

        private IGameFactory _gameFactory;
        private World _levelWorld;
        private LevelStageConfig _levelStageConfig;

        public async Task Init()
        {
            // var entity = _ecsWorld.NewEntity();
            await CreateAndSpawnEnemies();
        }

        private async Task CreateAndSpawnEnemies()
        {
            var levelModel = _levelWorld.LevelModel;

            var enemySpawnElements = _levelStageConfig.Enemies;
            var enemyConfigs = enemySpawnElements
                .SelectMany(e => Enumerable.Range(0, e.Count).Select(_ => e.EnemyConfig))
                .ToArray();

            var playersCoordinates = _levelWorld.Players.Values
                .Select(p => levelModel.ToTileCoordinate(p.Hero.WorldPosition))
                .ToArray();

            var floorTiles = levelModel.GetTilesByType(LevelTileType.FloorTile)
                .Where(t => !playersCoordinates.Contains(t.Coordinate))
                .ToList();

            Assert.IsTrue(enemyConfigs.Length <= floorTiles.Count, "enemies to spawn count greater than the floor tiles count");

            foreach (var enemyConfig in enemyConfigs)
            {
                var index = _levelWorld.RandomGenerator.Range(0, floorTiles.Count, _levelStageConfig.Index);
                var floorTile = floorTiles[index];
                var task = _gameFactory.InstantiatePrefabAsync(enemyConfig.Prefab, fix2.ToXY(floorTile.WorldPosition));
                var go = await task;
                Assert.IsNotNull(go);

                floorTiles.RemoveAt(index);

                var entityController = go.GetComponent<EnemyController>();
                Assert.IsNotNull(entityController);

                var enemy = _gameFactory.CreateEnemy(enemyConfig, entityController, _levelWorld.NewEntity());
                Assert.IsNotNull(enemy);

                _levelWorld.AddEnemy(enemy);

                var behaviourAgents = _gameFactory.CreateBehaviourAgent(enemyConfig.BehaviourConfig, enemy);
                foreach (var behaviourAgent in behaviourAgents)
                    _levelWorld.AddBehaviourAgent(enemy, behaviourAgent);

                _gameFactory.AddBehaviourComponents(enemyConfig.BehaviourConfig, enemy, enemy.Id);
            }
        }
    }*/

    public abstract class Entity<TConfig> : ITileLoad, IEntity where TConfig : EntityConfig
    {
        public event Action<IEntity> DeathEvent;
        public event Action<IEntity, int> DamageEvent;

        public EntityConfig Config { get; protected set; }

        public IEntityController EntityController { get; protected set; }

        public bool IsAlive => Health.Current > 0;

        public Health Health { get; private set; }

        public int LayerMask { get; }

        public Component[] Components { get; protected set; }
        public GameObject DestroyEffectPrefab => null;

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = Components.OfType<T>().FirstOrDefault();
            return component != default;
        }

        public fix Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (EntityController != null)
                    EntityController.Speed = value;
            }
        }

        public fix InitialSpeed => (fix) Config.Speed;

        public fix SpeedMultiplier { get; set; }

        public int2 Direction
        {
            get => _direction;
            set
            {
                _direction = value;

                if (EntityController != null)
                    EntityController.Direction = value;
            }
        }

        public fix2 WorldPosition { get; set; }

        public fix HitRadius { get; }
        public fix HurtRadius { get; }
        public fix ColliderRadius { get; }

        private fix _speed;
        private int2 _direction;

        protected Entity(TConfig config, IEntityController entityController)
        {
            Config = config;
            EntityController = entityController;

            LayerMask = config.LayerMask;

            Speed = fix.zero;
            SpeedMultiplier = fix.one;

            Health = new Health(Config.Health);
            Health.HealthDamagedEvent += OnHealthDamaged;

            Direction = Config.StartDirection;

            HitRadius = (fix) config.HitRadius;
            HurtRadius = (fix) config.HurtRadius;

            ColliderRadius = (fix) config.ColliderRadius;

            WorldPosition = entityController.WorldPosition;
        }

        public void Die()
        {
            Speed = fix.zero;
            SpeedMultiplier = fix.one;

            Health.HealthDamagedEvent -= OnHealthDamaged;
            Health = new Health(0);

            EntityController.Die();

            DeathEvent?.Invoke(this);
        }

        private void OnHealthDamaged(int damage)
        {
            if (damage == 0) // :TODO: maybe it's not a good idea?
                return;

            if (Health.Current < 1)
                Die();

            else
                TakeDamage(damage);
        }

        private void TakeDamage(int damage)
        {
            EntityController.TakeDamage(damage);

            DamageEvent?.Invoke(this, damage);
        }
    }
}
