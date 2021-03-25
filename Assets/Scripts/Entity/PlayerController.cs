using System;
using Configs.Entity;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Entity
{
    public class BombPlantEventData : EventArgs
    {
        public readonly int BlastRadius;
        public readonly float2 WorldPosition;

        public BombPlantEventData(int blastRadius, float2 worldPosition)
        {
            BlastRadius = blastRadius;
            WorldPosition = worldPosition;
        }
    }

    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerController : EntityController<BombermanConfig>, IPlayer
    {
        public event Action<int> BombCapacityChangedEvent;

        public event EventHandler<BombPlantEventData> BombPlantedEvent;

        private float _speed;
        private int _bombCapacity;

        private new void Start()
        {
            base.Start();

            BlastRadius = EntityConfig.BlastRadius;
            BombCapacity = EntityConfig.BombCapacity;
        }

        [UsedImplicitly]
        public void OnMove(InputAction.CallbackContext context)
        {
            SpeedVector.xy = context.ReadValue<Vector2>();
            SpeedVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, SpeedVector.y != 0);
            SpeedVector = math.round(SpeedVector) * Speed;
        }

        [UsedImplicitly]
        public void OnBombPlant(InputAction.CallbackContext context)
        {
            if (!context.action.triggered || BombCapacity <= 0)
                return;

            --BombCapacity;

            BombPlantedEvent?.Invoke(this, new BombPlantEventData(BlastRadius, WorldPosition.xy));
        }

        public override int Health { get; set; }

        public override float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                Animator.speed = _speed / MaxSpeed;
            }
        }

        public int BlastRadius { get; set; }

        public int BombCapacity
        {
            get => _bombCapacity;
            set
            {
                _bombCapacity = value;
                BombCapacityChangedEvent?.Invoke(_bombCapacity);
            }
        }
    }
}
