using Unity.Mathematics;
using UnityEngine;

namespace Entity.Hero
{
    public class HeroAnimator : EntityAnimator
    {
        private static readonly int IsMovingId = Animator.StringToHash("IsMoving");

        private static readonly int DirectionXId = Animator.StringToHash("DirectionX");
        private static readonly int DirectionYId = Animator.StringToHash("DirectionY");

        public void UpdateDirection(float2 direction)
        {
            Animator.SetFloat(DirectionXId, direction.x);
            Animator.SetFloat(DirectionYId, direction.y);
        }

        public void Move()
        {
            Animator.SetBool(IsMovingId, true);
        }

        public void StopMovement()
        {
            Animator.SetBool(IsMovingId, false);
        }
    }
}
