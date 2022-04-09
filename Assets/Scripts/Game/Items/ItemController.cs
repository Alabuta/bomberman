using Math.FixedPointMath;
using UnityEngine;

namespace Game.Items
{
    public class ItemController : MonoBehaviour
    {
        [SerializeField]
        protected Transform Transform;

        public fix2 WorldPosition
        {
            get => (fix2) Transform.position;
            set => Transform.position = fix2.ToXY(value);
        }

        public void DestroyItem()
        {
            gameObject.SetActive(false);
            Destroy(this);
        }
    }
}
