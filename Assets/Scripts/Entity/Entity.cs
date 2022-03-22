using System;
using Configs.Entity;
using Entity.Hero;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity
{
    public abstract class Entity<TConfig> : IEntity where TConfig : EntityConfig
    {
        public event Action KillEvent;

        public EntityConfig EntityConfig { get; protected set; }

        public IEntityController EntityController { get; protected set; }

        public bool IsAlive => Health.Current > 0;

        public Health Health { get; private set; }

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

        public fix InitialSpeed => (fix) EntityConfig.Speed;

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

        public fix2 WorldPosition
        {
            get => EntityController.WorldPosition;
            set => EntityController.WorldPosition = value;
        }

        public fix HitRadius { get; }
        public fix HurtRadius { get; }

        private fix _speed;
        private int2 _direction;

        protected Entity(TConfig config, IEntityController entityController)
        {
            EntityConfig = config;
            EntityController = entityController;

            Speed = fix.zero;
            SpeedMultiplier = fix.one;

            Health = new Health(EntityConfig.Health);
            Health.HealthChangedEvent += OnHealthChanged;

            Direction = EntityConfig.StartDirection;

            HitRadius = (fix) config.HitRadius;
            HurtRadius = (fix) config.HurtRadius;
        }

        public void Kill()
        {
            Speed = fix.zero;
            SpeedMultiplier = fix.one;

            Health = new Health(0);

            EntityController.Kill();

            KillEvent?.Invoke();
        }

        private void OnHealthChanged(int health)
        {
            if (health < 1)
                Kill();
        }
    }
}
