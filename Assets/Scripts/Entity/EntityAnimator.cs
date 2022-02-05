using System;
using Configs.Entity;
using Logic;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public abstract class EntityAnimator<T> : MonoBehaviour, IAnimationStateReader where T : EntityConfig
    {
        [SerializeField, HideInInspector]
        private int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");

        [SerializeField, HideInInspector]
        private int HorizontalSpeedId = Animator.StringToHash("HorizontalSpeed");

        [SerializeField, HideInInspector]
        private int IsAlive = Animator.StringToHash("IsAlive");

        [SerializeField]
        private Animator Animator;

        [SerializeField]
        public EntityController<T> Entity;

        public AnimatorState State { get; private set; }

        public event Action<AnimatorState> OnAnimationStateEnter;
        public event Action<AnimatorState> OnAnimationStateExit;

        public void OnEnterState(AnimatorState stateHash)
        {
            State = stateHash;
            OnAnimationStateEnter?.Invoke(State);
        }

        public void OnStateExit(AnimatorState stateHash)
        {
            State = stateHash;
            OnAnimationStateExit?.Invoke(State);
        }

        protected void Start()
        {
            Animator.SetBool(IsAlive, Entity.IsAlive);

            Entity.OnKillEvent += OnEntityKill;
        }

        protected void Update()
        {
            Animator.SetFloat(HorizontalSpeedId, Entity.MovementVector.x);
            Animator.SetFloat(VerticalSpeedId, Entity.MovementVector.y);

            Animator.speed = Entity.Speed / Entity.InitialSpeed;
        }

        private void OnEntityKill()
        {
            Animator.SetBool(IsAlive, Entity.IsAlive);

            Animator.speed = Entity.InitialSpeed;
        }

        private void OnDestroy()
        {
            Entity.OnKillEvent -= OnEntityKill;
        }
    }
}
