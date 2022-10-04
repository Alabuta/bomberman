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
        public bool IsLeafNode;
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int RootNodesStartCount = 2;
        private const int RootNodesMaxCount = 4;

        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag /*, HasColliderTempTag*/> _filter;

        private readonly List<int> _rootNodes = new(MaxEntries);

        private readonly List<Node> _nodes = new(512);
        private readonly List<(AABB Aabb, EcsEntity Entity)> _leafEntries = new(512);

        private readonly (AABB Invalid, EcsEntity Null) _invalidLeafEntry = (AABB.Invalid, EcsEntity.Null);
        private readonly Node _invalidNodeEntry = new()
        {
            Aabb = AABB.Invalid,

            EntriesStartIndex = -1,
            EntriesCount = 0
        };

        public IEnumerable<int> RootNodes => _rootNodes;

        public IEnumerable<Node> GetNodes(IEnumerable<int> indices) =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : indices.Select(i => _nodes[i]);

        public void Run()
        {
            _rootNodes.Clear();
            _nodes.Clear();
            _leafEntries.Clear();

            if (_filter.IsEmpty())
                return;

            var rootNodes = Enumerable
                .Range(0, RootNodesMaxCount)
                .Select(nodeIndex =>
                {
                    if (_rootNodes.Count < RootNodesStartCount)
                        _rootNodes.Add(nodeIndex);

                    return new Node
                    {
                        IsLeafNode = false,
                        Aabb = AABB.Invalid,

                        EntriesStartIndex = RootNodesMaxCount + nodeIndex * MaxEntries,
                        EntriesCount = 0
                    };
                })
                .ToArray();

            _nodes.AddRange(rootNodes);

            var leafNodes = Enumerable
                .Repeat(new Node
                {
                    IsLeafNode = true,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = -1,
                    EntriesCount = 0
                }, MaxEntries * rootNodes.Length);

            _nodes.AddRange(leafNodes);

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
            var (index, minArea) = (-1, fix.MaxValue);
            foreach (var i in _rootNodes)
            {
                var nodeAabb = _nodes[i].Aabb;
                if (nodeAabb == AABB.Invalid)
                {
                    index = i;
                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (index, minArea) = (i, conjugatedArea);
            }

            Assert.IsTrue(index > -1);

            var childNodes = ChooseLeaf(index, aabb, entity);

            _nodes[index] = childNodes[0];

            if (childNodes.Length == 1)
                return;

            Assert.IsTrue(_rootNodes.Count < RootNodesMaxCount);

            var newRootNodeIndex = _rootNodes.Last() + 1;
            _rootNodes.Add(newRootNodeIndex);
            _nodes[_rootNodes[newRootNodeIndex]] = childNodes[1];

            if (_rootNodes.Count < RootNodesMaxCount)
                return;

            Assert.IsTrue(_rootNodes.Count == RootNodesMaxCount);

            GrowTree();
        }

        private void GrowTree()
        {
            var (indexA, indexB) = FindLargestEntriesPair(_rootNodes, -1, 0, _rootNodes.Count, GetRootNodeAabb);
            Assert.IsTrue(indexA > -1 && indexB > -1);

            var (rootNodeIndexA, rootNodeIndexB) = (_rootNodes[indexA], _rootNodes[indexB]);
            Assert.IsTrue(rootNodeIndexA < rootNodeIndexB);

            var childNodesStartIndexA = _rootNodes[0];

            var childNodesStartIndexB = _nodes.Count;
            var childNodesB = Enumerable
                .Range(0, MaxEntries)
                .Select(_ => new Node
                {
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = -1,
                    EntriesCount = 0
                })
                .ToArray();

            var newRootNodeA = new Node
            {
                IsLeafNode = false,
                Aabb = AABB.Invalid,

                EntriesStartIndex = childNodesStartIndexA,
                EntriesCount = 0
            };

            var newRootNodeB = new Node
            {
                IsLeafNode = false,
                Aabb = AABB.Invalid,

                EntriesStartIndex = childNodesStartIndexB,
                EntriesCount = 1
            };

            var index = 0;
            childNodesB[index] = _nodes[rootNodeIndexB];
            ++index;

            foreach (var rootNodeIndexC in _rootNodes.Select(i => _rootNodes[i]))
            {
                if (rootNodeIndexC == rootNodeIndexA)
                {
                    _nodes[childNodesStartIndexA] = _nodes[rootNodeIndexA];

                    ++childNodesStartIndexA;
                    ++newRootNodeA.EntriesCount;

                    continue;
                }

                if (rootNodeIndexC == rootNodeIndexB)
                    continue;

                var aabbA = _nodes[rootNodeIndexA].Aabb;
                var aabbB = _nodes[rootNodeIndexB].Aabb;

                var nodeC = _nodes[rootNodeIndexC];
                var aabbC = nodeC.Aabb;

                var conjugatedAabbA = fix.AABBs_conjugate(aabbA, aabbC);
                var conjugatedAabbB = fix.AABBs_conjugate(aabbB, aabbC);

                var isSecondNodeTarget = IsSecondNodeTarget(aabbA, aabbB, conjugatedAabbA, conjugatedAabbB);
                if (isSecondNodeTarget)
                {
                    childNodesB[index] = nodeC;
                    ++index;

                    newRootNodeB.Aabb = fix.AABBs_conjugate(newRootNodeB.Aabb, aabbC);
                    ++newRootNodeB.EntriesCount;
                }
                else
                {
                    _nodes[childNodesStartIndexA] = nodeC;
                    ++childNodesStartIndexA;

                    newRootNodeA.Aabb = fix.AABBs_conjugate(newRootNodeA.Aabb, aabbC);
                    ++newRootNodeA.EntriesCount;
                }
            }

            _nodes.AddRange(childNodesB);

            var newRootNodeIndexA = _nodes.Count;
            var newRootNodeIndexB = newRootNodeIndexA + 1;

            _rootNodes.Clear();

            _rootNodes.Add(newRootNodeIndexA);
            _rootNodes.Add(newRootNodeIndexB);

            _nodes.Add(newRootNodeA);
            _nodes.Add(newRootNodeB);

            var newRootNodesStartIndex = _nodes.Count;
            _nodes.AddRange(Enumerable
                .Range(2, RootNodesMaxCount)
                .Select(nodeIndex => new Node
                {
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = newRootNodesStartIndex + nodeIndex * MaxEntries,
                    EntriesCount = 0
                }));

            /*var newRootNodesStartIndex = _nodes.Count;
            var newRootNodes = Enumerable
                .Range(0, RootNodesMaxCount - 2)
                .Select(nodeIndex => new Node
                {
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = newRootNodesStartIndex + nodeIndex * MaxEntries,
                    EntriesCount = 0
                })
                .ToArray();

            _nodes.AddRange(newRootNodes);

            var newLeafNodes = Enumerable
                .Repeat(new Node
                {
                    IsLeafNode = true,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = -1,
                    EntriesCount = 0
                }, MaxEntries * (newRootNodes.Length - 1));

            _nodes.AddRange(newLeafNodes);

            var newRootNodeIndexA = _nodes.Count;
            var newRootNodeIndexB = newRootNodeIndexA + 1;

            _rootNodes.Clear();

            _rootNodes.Add(newRootNodeIndexA);
            _rootNodes.Add(newRootNodeIndexB);*/

            AABB GetRootNodeAabb(int i) =>
                i > -1 ? _nodes[i].Aabb : AABB.Invalid;
        }

        private Node[] ChooseLeaf(int nodeIndex, AABB aabb, EcsEntity entity)
        {
            var node = _nodes[nodeIndex];
            if (node.IsLeafNode)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitNode(nodeIndex, true, _leafEntries, (aabb, entity), GetLeafEntryAabb, _invalidLeafEntry);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;
                    _leafEntries.AddRange(Enumerable.Repeat(_invalidLeafEntry, MaxEntries));
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (Aabb: aabb, Entity: entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                node.EntriesCount++;

                return new[] { node };
            }

            var entriesCount = node.EntriesCount;
            var startIndex = node.EntriesStartIndex;
            var endIndex = startIndex + (entriesCount >= MinEntries ? entriesCount : MaxEntries);

            var (childNodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = startIndex; i < endIndex; i++)
            {
                var childNodeAabb = _nodes[i].Aabb;
                if (childNodeAabb == AABB.Invalid)
                {
                    childNodeIndex = i;
                    break;
                }

                var conjugatedArea = GetConjugatedArea(childNodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (childNodeIndex, minArea) = (i, conjugatedArea);
            }

            Assert.IsTrue(childNodeIndex >= startIndex && startIndex <= endIndex);
            Assert.IsTrue(childNodeIndex < endIndex);

            if (childNodeIndex - startIndex == node.EntriesCount)
                node.EntriesCount++;

            var childNodes = ChooseLeaf(childNodeIndex, aabb, entity);

            _nodes[childNodeIndex] = childNodes[0];

            if (childNodes.Length == 1)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                return new[] { node };
            }

            var newChildNode = childNodes[1];

            if (entriesCount == MaxEntries)
                return SplitNode(nodeIndex, false, _nodes, newChildNode, GetNodeAabb, _invalidNodeEntry);

            _nodes[startIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return new[] { node };
        }

        private Node[] SplitNode<T>(int splitNodeIndex, bool isLeafNode, List<T> nodeEntries, T newEntry,
            Func<T, AABB> getAabbFunc, T invalidEntry)
        {
            var splitNode = _nodes[splitNodeIndex];

            var entriesCount = splitNode.EntriesCount;
            var startIndex = splitNode.EntriesStartIndex;
            var endIndex = startIndex + entriesCount;

            Assert.AreEqual(MaxEntries, entriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB) = FindLargestEntriesPair(nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
            Assert.IsTrue(indexA > -1 && indexB > -1);

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntries.Count;
            var newNode = new Node
            {
                IsLeafNode = isLeafNode,
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            nodeEntries.Add(newNodeStartEntry);
            nodeEntries.AddRange(Enumerable.Repeat(invalidEntry, MaxEntries - 1));

            (nodeEntries[startIndex], nodeEntries[indexA]) = (nodeEntries[indexA], nodeEntries[startIndex]);

            splitNode.EntriesCount = 1;
            splitNode.Aabb = getAabbFunc.Invoke(nodeEntries[startIndex]);

            for (var i = 1; i <= MaxEntries; i++)
            {
                if (startIndex + i == indexB)
                    continue;

                var entry = i == MaxEntries ? newEntry : nodeEntries[startIndex + i];
                var entityAabb = getAabbFunc.Invoke(entry);

                var splitNodeAabb = GetNodeAabb(splitNode);
                var newNodeAabb = GetNodeAabb(newNode);

                var conjugatedAabbA = fix.AABBs_conjugate(splitNodeAabb, entityAabb);
                var conjugatedAabbB = fix.AABBs_conjugate(newNodeAabb, entityAabb);

                var isNewNodeTarget = IsSecondNodeTarget(splitNodeAabb, newNodeAabb, conjugatedAabbA, conjugatedAabbB);
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (isNewNodeTarget || targetNode.EntriesCount != i)
                    nodeEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            if (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);

            return new[] { splitNode, newNode };
        }

        private static void FillNodes<T>(IList<T> nodeEntries, Func<T, AABB> getAabbFunc, ref Node splitNode,
            ref Node newNode)
        {
            ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
            ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

            var sourceNodeEntriesCount = sourceNode.EntriesCount;
            var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
            var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

            var targetNodeAabb = GetNodeAabb(targetNode);
            var sourceNodeAabb = AABB.Invalid;

            var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Invalid, fix.MaxValue);
            for (var i = sourceNodeStartIndex; i < sourceNodeEndIndex; i++)
            {
                var entry = nodeEntries[i];
                var entryAabb = getAabbFunc.Invoke(entry);

                var conjugatedArea = GetConjugatedArea(targetNodeAabb, entryAabb);
                if (conjugatedArea > minArena)
                {
                    sourceNodeAabb = fix.AABBs_conjugate(sourceNodeAabb, entryAabb);
                    continue;
                }

                sourceNodeAabb = fix.AABBs_conjugate(sourceNodeAabb, sourceEntryAabb);

                (sourceEntryIndex, sourceEntryAabb, minArena) = (i, entryAabb, conjugatedArea);
            }

            Assert.IsTrue(sourceEntryIndex > -1);

            var targetEntryIndex = targetNode.EntriesStartIndex + targetNode.EntriesCount;
            nodeEntries[targetEntryIndex] = nodeEntries[sourceEntryIndex];

            targetNode.Aabb = fix.AABBs_conjugate(targetNode.Aabb, sourceEntryAabb);
            targetNode.EntriesCount++;

            if (sourceEntryIndex != sourceNodeEndIndex - 1)
                nodeEntries[sourceEntryIndex] = nodeEntries[sourceNodeEndIndex - 1];

            sourceNode.Aabb = sourceNodeAabb;
            sourceNode.EntriesCount--;
        }

        private static (int indexA, int indexB) FindLargestEntriesPair<T>(
            IReadOnlyList<T> nodeEntries,
            T newEntry,
            int startIndex,
            int endIndex,
            Func<T, AABB> getAabbFunc)
        {
            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = startIndex; i < endIndex; i++)
            {
                var aabbA = getAabbFunc.Invoke(nodeEntries[i]);
                fix conjugatedArea;

                for (var j = i + 1; j < endIndex; j++)
                {
                    var aabbB = getAabbFunc.Invoke(nodeEntries[j]);
                    if (aabbB == AABB.Invalid)
                        continue;

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                var newEntryAabb = getAabbFunc.Invoke(newEntry);
                if (newEntryAabb == AABB.Invalid)
                    continue;

                conjugatedArea = GetConjugatedArea(aabbA, newEntryAabb);
                if (conjugatedArea <= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            return (indexA, indexB);
        }

        private static bool IsSecondNodeTarget(AABB nodeAabbA, AABB nodeAabbB, AABB conjugatedAabbA, AABB conjugatedAabbB)
        {
            var (areaIncreaseA, deltaA) = GetAreaAndSizeIncrease(nodeAabbA, conjugatedAabbA);
            var (areaIncreaseB, deltaB) = GetAreaAndSizeIncrease(nodeAabbB, conjugatedAabbB);

            if (areaIncreaseA > areaIncreaseB == deltaA > deltaB)
                return areaIncreaseA > areaIncreaseB;

            if (areaIncreaseA == areaIncreaseB)
                return deltaA > deltaB;

            if (deltaA == deltaB)
                return areaIncreaseA > areaIncreaseB;

            return true;
        }

        private static (fix areaIncrease, fix sizeIncrease) GetAreaAndSizeIncrease(AABB nodeAabb, AABB conjugatedAabb)
        {
            var conjugatedArea = fix.AABB_area(conjugatedAabb);
            var areaIncrease = conjugatedArea - fix.AABB_area(nodeAabb);

            var size = nodeAabb.max - nodeAabb.min;
            var conjugatedSize = conjugatedAabb.max - conjugatedAabb.min;
            var sizeIncrease = fix.max(conjugatedSize.x - size.x, conjugatedSize.y - size.y);

            return (areaIncrease, sizeIncrease);
        }

        private static AABB GetNodeAabb(Node node) =>
            node.Aabb;

        private static AABB GetLeafEntryAabb((AABB Aabb, EcsEntity Entity) leafEntry) =>
            leafEntry.Aabb;

        private static fix GetConjugatedArea(AABB aabbA, AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));
    }
}
