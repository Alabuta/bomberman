using System.Collections.Generic;
using Configs.Entity;
using Entity.Behaviours;

namespace Entity.Enemies
{
    public class Enemy : Entity<EnemyConfig>
    {
        private readonly List<BehaviourAgent> _agents = new();

        public IReadOnlyCollection<BehaviourAgent> Agents => _agents;

        public Enemy(EnemyConfig config, EnemyController entityController)
            : base(config, entityController)
        {
        }

        public void AddBehaviourAgent(BehaviourAgent agent)
        {
            _agents.Add(agent);
        }

        public void RemoveBehaviourAgent(BehaviourAgent agent)
        {
            _agents.Remove(agent);
        }
    }
}
