using System.Linq;
using Configs.Behaviours;
using Math.FixedPointMath;
using Unity.Mathematics;

namespace Entity.Behaviours
{
    public class AttackBehaviourAgent : BehaviourAgent
    {
        private int2 _tileCoordinate;

        private readonly fix _attackThreshold;
        private readonly int _damageValue;

        public AttackBehaviourAgent(AttackBehaviourConfig config, IEntity entity)
        {
            _attackThreshold = (fix) config.AttackThreshold;
            _damageValue = config.DamageValue;
        }

        public override void Update(GameContext gameContext, IEntity entity)
        {
            var levelGridModel = gameContext.LevelGridModel;
            var entityTileCoordinate = levelGridModel.ToTileCoordinate(entity.WorldPosition);

            var hero = gameContext.Heroes.FirstOrDefault(h => AreEntitiesOverlapped(entity, h));
            if (hero == null)
                return;

            hero.HeroHealth.ApplyDamage(_damageValue);

            _tileCoordinate = entityTileCoordinate;
        }

        private bool AreEntitiesOverlapped(IEntity entityA, IEntity entityB)
        {
            return fix2.distance(entityA.WorldPosition, entityB.WorldPosition) < entityA.HitRadius + entityB.HurtRadius;
        }
    }
}
