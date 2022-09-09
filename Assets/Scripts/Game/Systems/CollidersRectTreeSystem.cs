using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public struct Node
    {
        public int Generation;
        public bool IsLeafNode;
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

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

        public TreeRootNode(int generation, (AABB Aabb, BaseTreeNode ChildNode)[] entries) : base(generation)
        {
            Entries = entries;
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
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private int _treeGeneration;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly List<Node> _nodes = new(512);
        private readonly List<(AABB Aabb, EcsEntity entity)> _leafEntries = new(512);

        private readonly int[] _rootNodes2 = new int[RootNodesCount];

        private TreeRootNode[] _rootNodes;

        public TreeRootNode[] RootNodes => _rootNodes ?? Array.Empty<TreeRootNode>();

        private readonly EcsFilter<TransformComponent, HasColliderTag, HasColliderTempTag> _filter;

        public void Run()
        {
            _nodes.Clear();
            _leafEntries.Clear();

            if (_filter.IsEmpty())
                return;

            ++_treeGeneration;

            for (var i = 0; i < _rootNodes2.Length; i++)
            {
                var nodeIndex = _nodes.Count;
                var leafNodesStartIndex = nodeIndex + 1;

                _rootNodes2[i] = nodeIndex;

                _nodes.Add(new Node
                {
                    Generation = _treeGeneration,
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = leafNodesStartIndex,
                    EntriesCount = MinEntries
                });

                _nodes.AddRange(
                    Enumerable
                        .Range(0, MaxEntries)
                        .Select(_ => new Node
                        {
                            Generation = _treeGeneration,
                            IsLeafNode = true,
                            Aabb = AABB.Invalid,

                            EntriesStartIndex = -1,
                            EntriesCount = 0
                        })
                );
            }

            /*_rootNodes = Enumerable
                .Range(0, RootNodesCount)
                .Select(_ => new TreeRootNode(_treeGeneration, Enumerable
                    .Range(0, MinEntries)
                    .Select(_ => (AABB.Invalid, (BaseTreeNode) new TreeLeafNode(_treeGeneration, MaxEntries)))
                    .ToArray()))
                .ToArray();*/

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var aabb = entity.GetEntityColliderAABB(position);
                Insert2(entity, aabb);
            }
        }

        private void Insert2(EcsEntity entity, AABB aabb)
        {
            var (indexA, maxArena) = (-1, fix.MaxValue);
            foreach (var i in _rootNodes2)
            {
                var nodeAabb = _nodes[i].Aabb;
                if (nodeAabb == AABB.Invalid)
                {
                    indexA = i;
                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, aabb);
                if (conjugatedArea >= maxArena)
                    continue;

                (indexA, maxArena) = (i, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1);

            var childNodes = ChooseLeaf2(indexA, entity, aabb);
            if (childNodes.Length == 1)
                return;

            // AdjustBounds
            // GrowTree
            throw new NotImplementedException();
        }

        /*private bool IsLeafNode(int nodeIndex) =>
            _nodes[nodeIndex].Entries is LeafNodeEntries;*/

        private Node[] ChooseLeaf2(int nodeIndex, EcsEntity entity, AABB aabb)
        {
            var node = _nodes[nodeIndex];

            if (node.IsLeafNode)
            {
                if (node.EntriesCount == MaxEntries)
                    // return SplitLeafNode2(node, entity, aabb);
                    throw new NotImplementedException();

                Assert.IsTrue(node.EntriesCount is > -1 and < MaxEntries);
                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;

                    _leafEntries.AddRange(
                        Enumerable
                            .Range(0, MaxEntries)
                            .Select(_ => (AABB.Invalid, EcsEntity.Null)));
                }

                /*if (node.Entries.EntriesStartIndex == -1)
                {
                    node.Entries.EntriesStartIndex = _leafEntries.EntriesCount;
                    node.Entries.EndIndex = 0;

                    _leafEntries.AddRange(
                        Enumerable
                            .Range(0, MaxEntries)
                            .Select(_ => (AABB.Invalid, EcsEntity.Null)));
                }*/

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (aabb, entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb); // :TODO: check if changed
                node.EntriesCount++; // :TODO: check if changed

                return new[] { node };
            }

            var startIndex = node.EntriesStartIndex;
            var entriesCount = node.EntriesCount;

            var (childNodeIndex, maxArena) = (-1, fix.MaxValue);
            for (var i = startIndex; i < startIndex + entriesCount; i++)
            {
                var childNodeAabb = _nodes[i].Aabb;

                var conjugatedArea = GetConjugatedArea(childNodeAabb, aabb);
                if (conjugatedArea >= maxArena)
                    continue;

                (childNodeIndex, maxArena) = (i, conjugatedArea);
            }

            Assert.IsTrue(childNodeIndex > -1);

            var childNodes = ChooseLeaf2(childNodeIndex, entity, aabb);
            if (childNodes.Length == 1)
            {
                _nodes[childNodeIndex] = childNodes[0];
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb); // :TODO: check if changed
                return childNodes;
            }

            var newChildNode = childNodes[1];

            if (entriesCount == MaxEntries)
                // return SplitRootNode2(rootNode, b);
                throw new NotImplementedException();

            _nodes[startIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return new[] { node };
        }


        private void Insert(EcsEntity entity, AABB aabb)
        {
            var (indexA, maxArena) = (-1, fix.MaxValue);
            for (var i = 0; i < _rootNodes.Length; i++)
            {
                var rootNode = _rootNodes[i];

                if (rootNode.Aabb == AABB.Invalid)
                {
                    indexA = i;
                    break;
                }

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
                {
                    indexA = 0;
                }

                else
                    Assert.IsTrue(indexA > -1 && indexA < rootNode.EntriesCount);

                var (_, childNode) = rootNode.Entries[indexA];

                var (a, b) = ChooseLeaf(childNode, entity, aabb);
                if (b == null)
                {
                    rootNode.Entries[indexA].Aabb = childNode.Aabb;
                    rootNode.Entries[indexA].ChildNode = childNode;
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
                    if (conjugatedArea < maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, childNode.Aabb);
                if (conjugatedArea < maxArena)
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
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, entityAabb);
                if (conjugatedArea < maxArena)
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
