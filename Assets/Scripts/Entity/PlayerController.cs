using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    public class PlayerController : EntityController
    {
        protected override void Update()
        {
            SpeedVector.x = Input.GetAxis("Horizontal");
            SpeedVector.y = Input.GetAxis("Vertical");

            SpeedVector.xy *= math.select(HorizontalMovementMask, VerticalMovementMask, SpeedVector.y != 0);

            Animator.SetFloat(HorizontalSpeed, SpeedVector.x);
            Animator.SetFloat(VerticalSpeed, SpeedVector.y);

            SpeedVector = math.round(SpeedVector) * EntityConfig.Speed;
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
