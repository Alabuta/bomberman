using Configs.Entity;
using UnityEngine;

namespace Entity
{
    [RequireComponent(typeof(Animator))]
    public class EntityAnimator<T> : MonoBehaviour where T : EntityConfig
    {
        private readonly int _verticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private readonly int _horizontalSpeedId = Animator.StringToHash("HorizontalSpeed");

        [SerializeField]
        private Animator Animator;

        [SerializeField]
        public EntityController<T> Entity;

        public float PlaybackSpeed
        {
            get => Animator.speed;
            set => Animator.speed = value;
        }

        protected void Update()
        {
            Animator.SetFloat(_horizontalSpeedId, Entity.MovementVector.x);
            Animator.SetFloat(_verticalSpeedId, Entity.MovementVector.y);
        }
    }
}
