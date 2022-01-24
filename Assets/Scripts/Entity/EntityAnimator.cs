using Configs.Entity;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public class EntityAnimator<T> : MonoBehaviour where T : EntityConfig
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

        protected void Start()
        {
            Animator.SetBool(IsAlive, Entity.IsAlive);

            Entity.OnKillEvent += OnEntityKill;
        }

        private void OnDestroy()
        {
            Entity.OnKillEvent -= OnEntityKill;
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
    }
}
