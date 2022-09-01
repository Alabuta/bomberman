using System;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public abstract class BaseTreeNode
    {
        public int Generation;
        public int EntriesCount;

        public AABB Aabb;

        protected BaseTreeNode(int generation)
        {
            Generation = generation;
            EntriesCount = 0;
            Aabb = AABB.Invalid;
        }
    }

    public class TreeRootNode : BaseTreeNode
    {
        public readonly (AABB Aabb, BaseTreeNode ChildNode)[] Entries;

        public TreeRootNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Rect, BaseTreeNode ChildNode)[maxEntries];

            for (var i = 0; i < Entries.Length; i++)
                Entries[i].Aabb = AABB.Invalid;
        }
    }

    public class TreeLeafNode : BaseTreeNode
    {
        public readonly (AABB Aabb, EcsEntity Entity)[] Entries;

        public TreeLeafNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Aabb, EcsEntity Entity)[maxEntries];

            for (var i = 0; i < Entries.Length; i++)
                Entries[i].Aabb = AABB.Invalid;
        }
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int RootNodesCount = 2;
        private const int MaxEntries = 8;
        private const int MinEntries = MaxEntries / 2;

        private int _treeGeneration;

        private TreeRootNode[] _rootNodes;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _filter;

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            ++_treeGeneration;

            _rootNodes = Enumerable
                .Range(0, RootNodesCount)
                .Select(_ => new TreeRootNode(_treeGeneration, MaxEntries))
                .ToArray();

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var aabb = entity.GetEntityColliderAABB(position);
                Insert(entity, aabb);
            }
        }

        private void Insert(EcsEntity entity, AABB aabb)
        {
            var (indexA, maxArena) = (-1, fix.MaxValue);
            for (var i = 0; i < _rootNodes.Length; i++)
            {
                var rootNode = _rootNodes[i];

                var conjugatedArea = GetConjugatedArea(rootNode.Aabb, aabb);
                if (conjugatedArea >= maxArena)
                    continue;

                (indexA, maxArena) = (i, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexA < _rootNodes.Length);

            var (a, b) = ChooseLeaf(_rootNodes[indexA], entity, aabb);
            if (b == null)
                return;

            // AdjustBounds
            // GrowTree
            throw new NotImplementedException();
        }

        private (BaseTreeNode a, BaseTreeNode b) ChooseLeaf(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            if (node is TreeLeafNode leafNode)
            {
                if (leafNode.EntriesCount == leafNode.Entries.Length)
                    return SplitLeafNode(leafNode, entity, aabb);

                leafNode.Entries[leafNode.EntriesCount++] = (aabb, entity);
                leafNode.Aabb = fix.AABBs_conjugate(leafNode.Aabb, aabb);

                return (leafNode, null);
            }

            if (node is TreeRootNode rootNode)
            {
                var (indexA, maxArena) = (-1, fix.MaxValue);
                for (var i = 0; i < rootNode.EntriesCount - 1; i++)
                {
                    var (aabbA, _) = rootNode.Entries[i];

                    var conjugatedArea = GetConjugatedArea(aabbA, aabb);
                    if (conjugatedArea >= maxArena)
                        continue;

                    (indexA, maxArena) = (i, conjugatedArea);
                }

                if (rootNode.EntriesCount == 0)
                    indexA = 0;

                Assert.IsTrue(indexA > -1 && indexA < rootNode.EntriesCount);

                var (_, childNode) = rootNode.Entries[indexA];

                var (a, b) = ChooseLeaf(childNode, entity, aabb);
                if (b == null)
                {
                    node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                    return (a, null);
                }

                // rootNode.Entries[indexA] = (a.Aabb, a);

                if (rootNode.EntriesCount >= rootNode.Entries.Length)
                    return SplitRootNode(rootNode, b);

                rootNode.Entries[rootNode.EntriesCount++] = (b.Aabb, b);
                rootNode.Aabb = fix.AABBs_conjugate(rootNode.Aabb, aabb);

                return (a, b);
            }

            throw new NotSupportedException();
        }

        private (TreeRootNode a, TreeRootNode b) SplitRootNode(TreeRootNode nodeA, BaseTreeNode childNode)
        {
            Assert.AreEqual(MaxEntries, nodeA.EntriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = 0; i < nodeA.EntriesCount; i++)
            {
                var (aabbA, _) = nodeA.Entries[i];
                fix conjugatedArea;

                for (var j = i + 1; j < nodeA.EntriesCount; j++)
                {
                    var (aabbB, _) = nodeA.Entries[j];

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea >= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, childNode.Aabb);
                if (conjugatedArea >= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, nodeA.EntriesCount, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexB > -1 && (indexA < nodeA.EntriesCount || indexB < nodeA.EntriesCount));

            var nodeBFirstEntry = indexB != nodeA.EntriesCount ? nodeA.Entries[indexB] : (childNode.Aabb, childNode);
            var nodeB = new TreeRootNode(_treeGeneration, MaxEntries)
            {
                EntriesCount = 1,
                Entries =
                {
                    [0] = nodeBFirstEntry
                },
                Aabb = nodeBFirstEntry.Aabb
            };

            (nodeA.Entries[0], nodeA.Entries[indexA]) = (nodeA.Entries[indexA], nodeA.Entries[0]);
            nodeA.EntriesCount = 1;
            nodeA.Aabb = nodeA.Entries[0].Aabb;

            for (var i = MaxEntries; i > 0; i--)
            {
                if (i == indexB)
                    continue;

                var (aabb, entity) = i == MaxEntries ? (childNode.Aabb, childNode) : nodeA.Entries[i];

                var conjugatedAabbA = fix.AABBs_conjugate(nodeA.Aabb, aabb);
                var conjugatedAreaA = fix.AABB_area(conjugatedAabbA);

                var conjugatedAabbB = fix.AABBs_conjugate(nodeB.Aabb, aabb);
                var conjugatedAreaB = fix.AABB_area(conjugatedAabbB);

                var areaIncreaseA = conjugatedAreaA - fix.AABB_area(nodeA.Aabb);
                var areaIncreaseB = conjugatedAreaB - fix.AABB_area(nodeB.Aabb);

                if (areaIncreaseA <= areaIncreaseB)
                {
                    nodeA.Entries[nodeA.EntriesCount++] = (aabb, entity);
                    nodeA.Aabb = conjugatedAabbA;
                }
                else
                {
                    nodeB.Entries[nodeB.EntriesCount++] = (aabb, entity);
                    nodeB.Aabb = conjugatedAabbB;
                }
            }

            Assert.IsTrue(nodeA.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(nodeB.EntriesCount is >= MinEntries and <= MaxEntries);

            return (nodeA, nodeB);
        }

        private (TreeLeafNode a, TreeLeafNode b) SplitLeafNode(TreeLeafNode nodeA, EcsEntity ecsEntity, AABB entityAabb)
        {
            Assert.AreEqual(MaxEntries, nodeA.EntriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = 0; i < nodeA.EntriesCount; i++)
            {
                var (aabbA, _) = nodeA.Entries[i];
                fix conjugatedArea;

                for (var j = i + 1; j < nodeA.EntriesCount; j++)
                {
                    var (aabbB, _) = nodeA.Entries[j];

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea >= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, entityAabb);
                if (conjugatedArea >= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, nodeA.EntriesCount, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexB > -1 && (indexA < nodeA.EntriesCount || indexB < nodeA.EntriesCount));

            var nodeB = new TreeLeafNode(_treeGeneration, MaxEntries)
            {
                EntriesCount = 1,
                Entries =
                {
                    [0] = indexB != nodeA.EntriesCount ? nodeA.Entries[indexB] : (entityAabb, ecsEntity)
                }
            };
            nodeB.Aabb = nodeB.Entries[0].Aabb;

            (nodeA.Entries[0], nodeA.Entries[indexA]) = (nodeA.Entries[indexA], nodeA.Entries[0]);
            nodeA.EntriesCount = 1;
            nodeA.Aabb = nodeA.Entries[0].Aabb;

            for (var i = MaxEntries; i > 0; i--)
            {
                if (i == indexB)
                    continue;

                var (aabb, entity) = i == MaxEntries ? (entityAabb, ecsEntity) : nodeA.Entries[i];

                var conjugatedAabbA = fix.AABBs_conjugate(nodeA.Aabb, aabb);
                var conjugatedAreaA = fix.AABB_area(conjugatedAabbA);

                var conjugatedAabbB = fix.AABBs_conjugate(nodeB.Aabb, aabb);
                var conjugatedAreaB = fix.AABB_area(conjugatedAabbB);

                var areaIncreaseA = conjugatedAreaA - fix.AABB_area(nodeA.Aabb);
                var areaIncreaseB = conjugatedAreaB - fix.AABB_area(nodeB.Aabb);

                if (areaIncreaseA <= areaIncreaseB)
                {
                    nodeA.Entries[nodeA.EntriesCount++] = (aabb, entity);
                    nodeA.Aabb = conjugatedAabbA;
                }
                else
                {
                    nodeB.Entries[nodeB.EntriesCount++] = (aabb, entity);
                    nodeB.Aabb = conjugatedAabbB;
                }
            }

            Assert.IsTrue(nodeA.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(nodeB.EntriesCount is >= MinEntries and <= MaxEntries);

            return (nodeA, nodeB);
        }

        private static fix GetConjugatedArea(AABB aabbA, AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));

        private static void SortNodeEntities(BaseTreeNode node)
        {
            switch (node)
            {
                case TreeLeafNode leafNode:
                    Array.Sort(leafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;

                case TreeRootNode nonLeafNode:
                    Array.Sort(nonLeafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;
            }
        }

        private void AdjustTreeBounds()
        {
            throw new NotImplementedException();
        }
    }
}
