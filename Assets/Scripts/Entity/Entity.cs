using System;
using Configs.Entity;
using Unity.Mathematics;

namespace Entity
{
    public abstract class Entity<TConfig> : IEntity where TConfig : EntityConfig
    {
        public event Action OnKillEvent;

        public EntityConfig EntityConfig { get; protected set; }

        public IEntityController EntityController { get; protected set; }

        public bool IsAlive => Health > 0;

        public int Health { get; set; }

        public int InitialHealth => EntityConfig.Health;

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (EntityController != null)
                    EntityController.Speed = value;
            }
        }

        public float InitialSpeed => EntityConfig.Speed;

        public float SpeedMultiplier { get; set; }

        public float2 Direction
        {
            get => _direction;
            set
            {
                _direction = value;

                if (EntityController != null)
                    EntityController.Direction = value;
            }
        }

        public float3 WorldPosition => EntityController.WorldPosition;

        private float _speed;
        private float2 _direction;

        protected Entity(TConfig config, IEntityController entityController)
        {
            EntityConfig = config;
            EntityController = entityController;

            Speed = 0;
            SpeedMultiplier = 1;

            Health = EntityConfig.Health;
            _direction = EntityConfig.StartDirection;
        }

        public void Kill()
        {
            Health = 0;
            Speed = 0;
            SpeedMultiplier = 1;

            EntityController.Kill();

            OnKillEvent?.Invoke();
        }
    }
}
