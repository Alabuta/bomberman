using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Items
{
    public sealed class ItemController : EntityController
    {
        [SerializeField]
        private ItemAnimator ItemAnimator;

        public override fix Speed { protected get; set; }

        public override int2 Direction { protected get; set; }

        protected override EntityAnimator EntityAnimator =>
            ItemAnimator;
    }
}
