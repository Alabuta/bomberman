using System;
using System.Linq;
using Configs.Entity;
using Game.Components;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Game
{
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
