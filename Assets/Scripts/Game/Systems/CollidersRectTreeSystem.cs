using System;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;

namespace Game.Systems
{
    public abstract class BaseTreeNode
    {
        public int Generation;
        public int EntriesCount;

        public BaseTreeNode(int generation)
        {
            Generation = generation;
            EntriesCount = 0;
        }
    }

    public class TreeNonLeafNode : BaseTreeNode
    {
        public readonly (AABB Aabb, BaseTreeNode ChildNode)[] Entries;

        public TreeNonLeafNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Rect, BaseTreeNode ChildNode)[maxEntries];
        }
    }

    public class TreeLeafNode : BaseTreeNode
    {
        public readonly (AABB Aabb, EcsEntity Entity)[] Entries;

        public TreeLeafNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Aabb, EcsEntity Entity)[maxEntries];
        }
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int MaxEntries = 8;
        private const int MinEntries = MaxEntries / 2;

        private int _treeGeneration;

        private BaseTreeNode _root;

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

                var aabb = entity.GetEntityColliderAABB(position);
                Insert(_root, entity, aabb);
            }
        }

        private void Insert(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            var leafNode = ChooseLeaf(node, entity, aabb);

            // AdjustBounds
            // GrowTree
            throw new NotImplementedException();
        }

        private (TreeLeafNode left, TreeLeafNode right) ChooseLeaf(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            if (node is TreeLeafNode leafNode)
            {
                if (leafNode.EntriesCount < leafNode.Entries.Length)
                {
                    leafNode.Entries[leafNode.EntriesCount++] = (aabb, entity);
                    return (leafNode, null);
                }

                ;
            }

            throw new NotImplementedException();
        }

        private void AdjustTreeBounds()
        {
            throw new NotImplementedException();
        }
    }
}
