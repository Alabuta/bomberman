using System;
using System.Linq;
using Configs.Behaviours;
using Math.FixedPointMath;

namespace Game.Behaviours.AttackBehaviours
{
    public class SimpleAttackBehaviourAgent : BehaviourAgent
    {
        private readonly int _damageValue;

        private Hero.Hero[] _overlappedHeroes;

        public SimpleAttackBehaviourAgent(SimpleAttackBehaviourConfig config, IEntity entity)
        {
            _damageValue = config.DamageValue;

            _overlappedHeroes = Array.Empty<Hero.Hero>();
        }

        public override void Update(GameContext gameContext, IEntity entity, fix deltaTime)
        {
            var overlappedHeroes = gameContext.Heroes
                .Where(h => AreEntitiesOverlapped(entity, h))
                .ToArray();

            foreach (var hero in overlappedHeroes.Except(_overlappedHeroes))
                hero.Health.ApplyDamage(_damageValue);

            _overlappedHeroes = overlappedHeroes;
        }

        private static bool AreEntitiesOverlapped(IEntity entityA, IEntity entityB)
        {
            return fix2.distance(entityA.WorldPosition, entityB.WorldPosition) < entityA.HitRadius + entityB.HurtRadius;
        }
    }
}
