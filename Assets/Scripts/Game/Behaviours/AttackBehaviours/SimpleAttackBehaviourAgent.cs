namespace Game.Behaviours.AttackBehaviours
{
    /*public class SimpleAttackBehaviourAgent : BehaviourAgent :TODO: fix
    {
        private readonly int _damageValue;

        private Hero.Hero[] _overlappedHeroes;

        public SimpleAttackBehaviourAgent(SimpleAttackBehaviourConfig config, IEntity entity)
        {
            _damageValue = config.DamageValue; // :TODO: get damage value from actual entity

            _overlappedHeroes = Array.Empty<Hero.Hero>();
        }

        public override void Update(GameContext2 gameContext2, IEntity entity, fix deltaTime)
        {
            var overlappedHeroes = gameContext2.Heroes
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
    }*/
}
