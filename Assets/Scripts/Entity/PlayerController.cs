using System;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public sealed class PlayerController : EntityController
    {
        private float _speed;

        protected override void Update()
        {
            SpeedVector.x = Input.GetAxis("Horizontal");
            SpeedVector.y = Input.GetAxis("Vertical");

            SpeedVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, SpeedVector.y != 0);

            Animator.SetFloat(HorizontalSpeed, SpeedVector.x);
            Animator.SetFloat(VerticalSpeed, SpeedVector.y);

            SpeedVector = math.round(SpeedVector) * Speed;
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
    }
}
