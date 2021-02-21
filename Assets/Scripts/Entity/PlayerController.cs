using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public class PlayerController : EntityController
    {
        protected override void Update()
        {
            MovementVector.x = Input.GetAxis("Horizontal");
            MovementVector.y = Input.GetAxis("Vertical");

            MovementVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, MovementVector.y != 0);

            Animator.SetFloat(HorizontalSpeed, MovementVector.x);
            Animator.SetFloat(VerticalSpeed, MovementVector.y);

            MovementVector = math.round(MovementVector) * EntityConfig.Speed;
        }

        protected override void OnTriggerEnter(Collider otherCollider)
        {
            if (otherCollider.CompareTag("PowerItem"))
                OnPowerItemTouch(otherCollider);
        }

        private void OnPowerItemTouch(Collider otherCollider)
        {
            // effect.ApplyTo(otherCollider.gameObject);
        }
    }
}
