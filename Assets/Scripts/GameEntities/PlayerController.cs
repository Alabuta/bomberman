using Configs.Entity;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameEntities
{
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerController : EntityController<BombermanConfig>, IPlayer
    {
        private static readonly float2 HorizontalMovementMask = new float2(1, 0);
        private static readonly float2 VerticalMovementMask = new float2(0, 1);

        private float _speed;

        private new void Start()
        {
            base.Start();

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
        public int BombCapacity { get; set; }
    }
}
