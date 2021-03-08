using System;
using Configs.Entity;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameEntities
{
    [RequireComponent(typeof(LocalPlayerInput))]
    public sealed class PlayerController : EntityController<BombermanConfig>, IPlayer
    {
        private static readonly float2 HorizontalMovementMask = new float2(1, 0);
        private static readonly float2 VerticalMovementMask = new float2(0, 1);

        private float _speed;
        // private LocalPlayerInput _input;

        private new void Start()
        {
            base.Start();

            BombCapacity = EntityConfig.BombCapacity;

            /*_input = gameObject.GetComponent<LocalPlayerInput>();
            _input.MovementVector.Subscribe(UpdateSpeedVector).AddTo(this);
            _input.BombPlanted.Subscribe(_ => OnBombPlant()).AddTo(this);*/
        }

        private new void Update()
        {
            base.Update();
        }

        protected override void OnTriggerEnter2D(Collider2D otherCollider)
        {
            // throw new System.NotImplementedException();

            /*if (otherCollider.CompareTag("PowerItem"))
                OnPowerItemTouch(otherCollider);

            else if (otherCollider.CompareTag("Enemy"))
                OnEnemyTouch(otherCollider);

            else if (otherCollider.CompareTag("Explosive"))
                OnExplosiveTouch(otherCollider);*/
        }

        private void OnPowerItemTouch(Collider otherCollider)
        {
            throw new System.NotImplementedException();

            // effect.ApplyTo(otherCollider.gameObject);
        }

        private void OnEnemyTouch(Collider otherCollider)
        {
            throw new System.NotImplementedException();
        }

        private void OnExplosiveTouch(Collider otherCollider)
        {
            throw new System.NotImplementedException();
        }

        private void UpdateSpeedVector(float2 vector)
        {
            SpeedVector.xy = vector;
            SpeedVector = math.round(SpeedVector) * Speed;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            SpeedVector.xy = context.ReadValue<Vector2>();
            // SpeedVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, SpeedVector.y != 0);
            SpeedVector = math.round(SpeedVector) * Speed;
        }

        public void OnBombPlant(InputAction.CallbackContext context)
        {
            if (!context.action.triggered || BombCapacity <= 0)
                return;

            Debug.LogWarning("Bomb has been planted!");
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
