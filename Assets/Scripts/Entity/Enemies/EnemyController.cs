using Unity.Mathematics;
using UnityEngine;

namespace Entity.Enemies
{
    public sealed class EnemyController : EntityController
    {
        public override float Speed { get; set; }
        public override float2 Direction { get; set; }

        [SerializeField]
        private EnemyAnimator EnemyAnimator;

        protected override EntityAnimator EntityAnimator =>
            EnemyAnimator;
    }
}
