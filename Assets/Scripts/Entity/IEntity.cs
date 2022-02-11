using System;
using Configs.Entity;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public interface IEntity
    {
        event Action OnKillEvent;

        IEntityController EntityController { get; }

        bool IsAlive { get; }

        int Health { get; set; }
        int InitialHealth { get; }

        float CurrentSpeed { get; }
        float InitialSpeed { get; }
        float SpeedMultiplier { get; set; }

        float2 WorldPosition { get; }

        float2 Direction { get; }

        void ApplyAttack();

        void Kill();
    }

    public abstract class Entity<TConfig> : IEntity where TConfig : EntityConfig
    {
        public event Action OnKillEvent;

        public bool IsAlive => Health > 0;

        public int Health { get; set; }

        public int InitialHealth => EntityConfig.Health;

        public float CurrentSpeed { get; protected set; }

        public float InitialSpeed => EntityConfig.Speed;

        public float SpeedMultiplier { get; set; }

        public float2 WorldPosition { get; }

        public float2 Direction { get; }

        public IEntityController EntityController { get; protected set; }


        [SerializeField]
        protected TConfig EntityConfig;

        public Entity(TConfig config, IEntityController entityController)
        {
            EntityConfig = config;
            EntityController = entityController;

            CurrentSpeed = 0;
            SpeedMultiplier = 1;

            Health = EntityConfig.Health;
            Direction = EntityConfig.StartDirection;
        }

        public void ApplyAttack()
        {
            // :TODO: pass attack config as a parameter
        }

        public void Kill()
        {
            Health = 0;
            CurrentSpeed = 0;
            SpeedMultiplier = 1;

            EntityController.Kill();

            OnKillEvent?.Invoke();
        }
    }
}
