using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Entity.Enemies
{
    public sealed class EnemyController : EntityController
    {
        [SerializeField]
        private EnemyAnimator EnemyAnimator;

        protected override EntityAnimator EntityAnimator =>
            EnemyAnimator;

        public override fix Speed { get; set; }
        public override int2 Direction { get; set; }
    }
}
