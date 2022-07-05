using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Enemies
{
    public sealed class EnemyController : EntityController
    {
        [SerializeField]
        private EnemyAnimator EnemyAnimator;

        public override fix Speed { protected get; set; }

        public override int2 Direction { protected get; set; }

        protected override EntityAnimator EntityAnimator =>
            EnemyAnimator;
    }
}
