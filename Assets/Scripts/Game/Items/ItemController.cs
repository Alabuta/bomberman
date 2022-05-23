using Math.FixedPointMath;
using UnityEngine;

namespace Game.Items
{
    public class ItemController : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        private int IsAliveId = Animator.StringToHash("IsAlive");

        [SerializeField]
        protected Transform Transform;

        [SerializeField]
        protected Animator Animator;

        public fix2 WorldPosition
        {
            get => (fix2) Transform.position;
            set => Transform.position = fix2.ToXY(value);
        }

        public void DestroyItem()
        {
            PlayDestroyAnimation();
            /*gameObject.SetActive(false);
            Destroy(this);*/
        }

        public void PlayDestroyAnimation()
        {
            Animator.SetBool(IsAliveId, false);
            Animator.speed = 1;
        }
    }
}
