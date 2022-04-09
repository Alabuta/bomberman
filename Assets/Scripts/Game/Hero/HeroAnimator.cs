using Unity.Mathematics;
using UnityEngine;

namespace Game.Hero
{
    public class HeroAnimator : EntityAnimator
    {
        [SerializeField, HideInInspector]
        private int IsMovingId = Animator.StringToHash("IsMoving");

        [SerializeField, HideInInspector]
        private int DirectionXId = Animator.StringToHash("DirectionX");

        [SerializeField, HideInInspector]
        private int DirectionYId = Animator.StringToHash("DirectionY");

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
