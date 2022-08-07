using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems
{
    public struct TreeNode
    {
        public int Generation;

        public int EntriesCount;
        public readonly (BoundingRect Rect, TreeNode ChildNode)[] Entries;

        public TreeNode(int generation, int maxEntries)
        {
            Generation = generation;

            EntriesCount = 0;
            Entries = new (BoundingRect Rect, TreeNode Id)[maxEntries];
        }
    }

    public struct TreeLeaf
    {
        public int Generation;

        public int EntriesCount;
        public readonly (BoundingRect Rect, int Id)[] Entries;

        public TreeLeaf(int generation, int maxEntries)
        {
            Generation = generation;

            EntriesCount = 0;
            Entries = new (BoundingRect Rect, int Id)[maxEntries];
        }
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int MaxTreeDepth = 16;
        private const int NodeDivisions = 2;

        private const int NodeCapacity = 16;
        private const int ChildrenNodesCount = 4;

        private int _treeGeneration;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _filter;

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var (min, max) = entity.GetEntityColliderAABB(position);
            }
        }
    }
}
