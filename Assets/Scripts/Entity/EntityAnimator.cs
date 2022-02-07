using System;
using Logic;
using Unity.Mathematics;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public abstract class EntityAnimator : MonoBehaviour, IAnimationStateReader
    {
        [SerializeField, HideInInspector]
        private int IsAliveId = Animator.StringToHash("IsAlive");

        [SerializeField, HideInInspector]
        private int IsMovingId = Animator.StringToHash("IsMoving");

        [SerializeField, HideInInspector]
        private int DirectionXId = Animator.StringToHash("DirectionX");

        [SerializeField, HideInInspector]
        private int DirectionYId = Animator.StringToHash("DirectionY");

        [SerializeField]
        private Animator Animator;

        public AnimatorState State { get; private set; }

        public event Action<AnimatorState> OnAnimationStateEnter;
        public event Action<AnimatorState> OnAnimationStateExit;

        public void OnEnterState(AnimatorState state)
        {
            State = state;
            OnAnimationStateEnter?.Invoke(State);
        }

        public void OnStateExit(AnimatorState state)
        {
            State = state;
            OnAnimationStateExit?.Invoke(State);
        }

        public void UpdateDirection(float2 direction)
        {
            Animator.SetFloat(DirectionXId, direction.x);
            Animator.SetFloat(DirectionYId, direction.y);
        }

        public void UpdateSpeed(float speed)
        {
            Animator.SetBool(IsMovingId, speed > .5f);
        }

        public void UpdatePlaybackSpeed(float speed)
        {
            Animator.speed = speed;
        }

        public void SetAlive()
        {
            Animator.SetBool(IsAliveId, true);
            Animator.speed = 1;
        }

        public void SetDead()
        {
            Animator.SetBool(IsAliveId, false);
            Animator.speed = 1;
        }
    }
}
