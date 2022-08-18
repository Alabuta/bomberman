using System;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public abstract class BaseTreeNode
    {
        public int Generation;
        public int EntriesCount;

        public AABB Aabb;

        public BaseTreeNode(int generation)
        {
            Generation = generation;
            EntriesCount = 0;
            Aabb = AABB.Invalid;
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

        private (BaseTreeNode a, BaseTreeNode b)? ChooseLeaf(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            if (node is TreeLeafNode leafNodeA)
            {
                if (leafNodeA.EntriesCount >= leafNodeA.Entries.Length)
                    return SplitLeafNode(leafNodeA, entity, aabb);

                leafNodeA.Entries[leafNodeA.EntriesCount++] = (aabb, entity);
                leafNodeA.Aabb = fix.AABBs_conjugate(leafNodeA.Aabb, aabb);
                // SortNodeEntities(leafNodeA);

                return null;
            }

            if (node is TreeNonLeafNode nonLeafNode)
            {
                var (index, area) = (-1, fix.MaxValue);
                for (var i = 0; i < nonLeafNode.EntriesCount; i++)
                {
                    var (childNodeAabb, _) = nonLeafNode.Entries[i];

                    var childNodeArea = fix.AABB_area(childNodeAabb);
                    if (childNodeArea >= area)
                        continue;

                    area = childNodeArea;
                    index = i;
                }

                Assert.IsTrue(index > -1 && index < nonLeafNode.EntriesCount);

                var (_, childNode) = nonLeafNode.Entries[index];

                var splitNodes = ChooseLeaf(childNode, entity, aabb);
                if (splitNodes == null)
                {
                    node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                    return null;
                }

                var (a, b) = splitNodes.Value;
                if (nonLeafNode.EntriesCount < nonLeafNode.Entries.Length)
                {
                    nonLeafNode.Entries[index] = (a.Aabb, a);
                    nonLeafNode.Entries[nonLeafNode.EntriesCount++] = (b.Aabb, b);
                    nonLeafNode.Aabb = fix.AABBs_conjugate(nonLeafNode.Aabb, aabb);
                }
                else
                {
                    // concatenate child nodes
                }
            }

            throw new NotSupportedException();
        }

        private static void SortNodeEntities(BaseTreeNode node)
        {
            switch (node)
            {
                case TreeLeafNode leafNode:
                    Array.Sort(leafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;

                case TreeNonLeafNode nonLeafNode:
                    Array.Sort(nonLeafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;
            }
        }

        private (TreeLeafNode a, TreeLeafNode b) SplitLeafNode(TreeLeafNode leafNodeA, EcsEntity entity, AABB aabb)
        {
            var leafNodeB = new TreeLeafNode(_treeGeneration, MaxEntries);
            leafNodeB.Entries[leafNodeB.EntriesCount++] = (aabb, entity);
            leafNodeB.Aabb = fix.AABBs_conjugate(leafNodeB.Aabb, aabb);

            var moveCount = math.max(MinEntries, leafNodeA.EntriesCount / 2) - 1;
            while (moveCount-- > 0 && leafNodeA.EntriesCount > -1)
            {
                var (aabbA, entityA) = leafNodeA.Entries[leafNodeA.EntriesCount--];

                leafNodeB.Entries[leafNodeB.EntriesCount++] = (aabbA, entityA);
                leafNodeB.Aabb = fix.AABBs_conjugate(leafNodeB.Aabb, aabbA);
            }

            leafNodeA.Aabb = GetNodeAABB(leafNodeA);

            /*SortNodeEntities(leafNodeA);
            SortNodeEntities(leafNodeB);*/

            var isLeafASmaller = fix.AABB_area(leafNodeA.Aabb) <= fix.AABB_area(leafNodeB.Aabb);
            return isLeafASmaller ? (leafNodeA, leafNodeB) : (leafNodeB, leafNodeA);
        }

        private static AABB GetNodeAABB(BaseTreeNode node)
        {
            return node switch
            {
                TreeLeafNode leaf => leaf.Entries.Aggregate(AABB.Invalid, (a, p) => fix.AABBs_conjugate(a, p.Aabb)),
                TreeNonLeafNode nonLeaf => nonLeaf.Entries.Aggregate(AABB.Invalid, (a, p) => fix.AABBs_conjugate(a, p.Aabb)),
                _ => AABB.Invalid
            };
        }

        private void AdjustTreeBounds()
        {
            throw new NotImplementedException();
        }
    }
}
