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

        public override float Speed { get; set; }
        public override float2 Direction { get; set; }
    }
}
