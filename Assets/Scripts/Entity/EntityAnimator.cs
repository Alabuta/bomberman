using System;
using Logic;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public abstract class EntityAnimator : MonoBehaviour, IAnimationStateReader
    {
        [SerializeField, HideInInspector]
        private int IsAliveId = Animator.StringToHash("IsAlive");

        [SerializeField]
        protected Animator Animator;

        public AnimatorState State { get; private set; }

        public event Action<AnimatorState> OnAnimationStateEnter;
        public event Action<AnimatorState> OnAnimationStateExit;

        public void OnEnterState(AnimatorState state)
        {
            State = state;
            OnAnimationStateEnter?.Invoke(State);
        }

        public void OnExitState(AnimatorState state)
        {
            State = state;
            OnAnimationStateExit?.Invoke(State);
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
