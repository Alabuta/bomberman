using System;
using Logic;
using UnityEngine;

namespace Game
{
    [RequireComponent(typeof(Animator))]
    public class EffectAnimator : MonoBehaviour, IAnimationStateReader
    {
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
    }
}
