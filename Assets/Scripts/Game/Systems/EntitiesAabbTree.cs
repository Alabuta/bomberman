﻿using System;
using System.Collections.Generic;
using System.Linq;
using App;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Systems
{
    public struct Node
    {
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

    public sealed class EntitiesAabbTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private readonly List<List<Node>> _nodes = new();
        private readonly List<(AABB Aabb, EcsEntity Entity)> _leafEntries = new(512);

        private readonly Node _invalidNodeEntry = new()
        {
            Aabb = AABB.Empty,
            EntriesStartIndex = -1,
            EntriesCount = 0
        };

        private readonly (AABB Aabb, EcsEntity Entity) _invalidLeafEntry = new(AABB.Empty, EcsEntity.Null);

        public int TreeHeight => _nodes.Count;

        public IEnumerable<Node> RootNodes =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : _nodes[0].TakeWhile(n => n.Aabb != AABB.Empty);

        public IEnumerable<Node> GetNodes(int levelIndex, IEnumerable<int> indices) =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : indices.Select(i => _nodes[levelIndex][i]);

        public IEnumerable<(AABB Aabb, EcsEntity Entity)> GetLeafEntries(IEnumerable<int> indices) =>
            _leafEntries.Count == 0 ? Enumerable.Empty<(AABB Aabb, EcsEntity Entity)>() : indices.Select(i => _leafEntries[i]);

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep)
        {
            using var _ = Profiling.RTreeBuild.Auto();

            _nodes.Clear();
            _leafEntries.Clear();

            if (filter.IsEmpty())
                return;

            _nodes.Add(Enumerable.Repeat(_invalidNodeEntry, MaxEntries).ToList());

            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);
                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                Insert(entity, aabb);
            }
        }

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<(AABB Aabb, EcsEntity Entity)> result)
        {
            const int levelIndex = 0;

            var topLevelNodes = _nodes[levelIndex];
            for (var nodeIndex = 0; nodeIndex < topLevelNodes.Count; nodeIndex++)
                QueryNodesByLine(p0, p1, result, levelIndex, nodeIndex);
        }

        private void QueryNodesByLine(
            fix2 p0, fix2 p1,
            ICollection<(AABB Aabb, EcsEntity Entity)> result,
            int levelIndex, int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!node.Aabb.CohenSutherlandLineClip(ref p0, ref p1))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex + 1 == TreeHeight)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    if (!IntersectedByLine(p0, p1, _leafEntries[i].Aabb))
                        continue;

                    result.Add(_leafEntries[i]);
                }

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByLine(p0, p1, result, levelIndex + 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, in AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(in AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result)
        {
            const int levelIndex = 0;

            var topLevelNodes = _nodes[levelIndex];
            for (var nodeIndex = 0; nodeIndex < topLevelNodes.Count; nodeIndex++)
                QueryNodesByAabb(aabb, result, levelIndex, nodeIndex);
        }

        private void QueryNodesByAabb(in AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result, int levelIndex,
            int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!fix.is_AABB_overlapped_by_AABB(aabb, node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex + 1 == TreeHeight)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                    result.Add(_leafEntries[i]);

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByAabb(aabb, result, levelIndex + 1, i);
        }

        private void Insert(EcsEntity entity, in AABB aabb)
        {
            const int rootLevelIndex = 0;
            const int rootEntriesStartIndex = 0;

            var rootNodes = _nodes[rootLevelIndex];
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodes.Count);
#endif

            var nonEmptyNodesCount = 0;
            for (var i = 0; i < rootNodes.Count; i++)
            {
                if (rootNodes[i].Aabb != AABB.Empty)
                    nonEmptyNodesCount++;
            }
#if ENABLE_ASSERTS
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.Count(n => n.EntriesCount > 0));
#endif

            var nodeIndex = GetNodeIndexToInsert(rootLevelIndex, rootEntriesStartIndex,
                rootEntriesStartIndex + nonEmptyNodesCount, nonEmptyNodesCount < MinEntries, aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(nodeIndex > -1);
#endif

            var targetNode = rootNodes[nodeIndex];
            var extraNode = ChooseLeaf(ref targetNode, rootLevelIndex, aabb, entity);
#if ENABLE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, targetNode.Aabb);
#endif

            rootNodes[nodeIndex] = targetNode;

            if (!extraNode.HasValue)
                return;

#if ENABLE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, extraNode.Value.Aabb);
#endif

            var candidateNodeIndex = rootNodes.FindIndex(n => n.Aabb == AABB.Empty);
            if (candidateNodeIndex != -1)
            {
                rootNodes[candidateNodeIndex] = extraNode.Value;
                return;
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(rootNodes.All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
#endif

            GrowTree(extraNode.Value);
        }

        private Node? ChooseLeaf(ref Node node, int nodeLevelIndex, in AABB aabb, EcsEntity entity)
        {
            var isLeafLevel = nodeLevelIndex + 1 == TreeHeight;
            if (isLeafLevel)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitNode(ref node, _leafEntries, (aabb, entity), GetLeafEntryAabb, _invalidLeafEntry);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;
                    _leafEntries.AddRange(Enumerable.Repeat(_invalidLeafEntry, MaxEntries));
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (Aabb: aabb, Entity: entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                node.EntriesCount++;

                return null;
            }

            var entriesCount = node.EntriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);
#endif

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesStartIndex > -1);
#endif

            var childNodeLevelIndex = nodeLevelIndex + 1;
            var childNodeIndex = GetNodeIndexToInsert(childNodeLevelIndex, entriesStartIndex, entriesEndIndex, false, aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);
#endif

            var targetChildNode = _nodes[childNodeLevelIndex][childNodeIndex];
            var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, aabb, entity);

            var childLevelNodes = _nodes[childNodeLevelIndex];
            childLevelNodes[childNodeIndex] = targetChildNode;

            if (!extraChildNode.HasValue)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                return null;
            }

            var newChildNode = extraChildNode.Value;
            if (entriesCount == MaxEntries)
                return SplitNode(ref node, childLevelNodes, newChildNode, GetNodeAabb, _invalidNodeEntry);

            childLevelNodes[entriesStartIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return null;
        }

        private void GrowTree(in Node newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodes = _nodes[0];
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodes.Count);
#endif

            var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = 0,
                EntriesCount = rootNodes.Count
            };

            var newRootNodeB = SplitNode(ref newRootNodeA, rootNodes, newEntry, GetNodeAabb, _invalidNodeEntry);

            var newRootNodes = new List<Node>(new[] { newRootNodeA, newRootNodeB });
            newRootNodes.AddRange(Enumerable
                .Repeat(new Node
                {
                    Aabb = AABB.Empty,
                    EntriesStartIndex = -1,
                    EntriesCount = 0
                }, MaxEntries - newRootNodes.Count));

            _nodes.Insert(0, newRootNodes);

#if ENABLE_ASSERTS
            Assert.IsTrue(_nodes[0].Count(n => n.Aabb != AABB.Empty) >= MinEntries);
            Assert.IsTrue(_nodes[0].Count(n => n.EntriesStartIndex != -1) >= MinEntries);
            Assert.IsTrue(_nodes[0].Count(n => n.EntriesCount > 0) >= MinEntries);
#endif
        }

        private static Node SplitNode<T>(ref Node splitNode, List<T> nodeEntries,
            T newEntry, Func<T, AABB> getAabbFunc, T invalidEntry)
        {
            var entriesCount = splitNode.EntriesCount;
            var startIndex = splitNode.EntriesStartIndex;
            var endIndex = startIndex + entriesCount;

#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, entriesCount);
#endif

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB) = FindLargestEntriesPair(nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
#if ENABLE_ASSERTS
            Assert.IsTrue(indexA > -1 && indexB > -1);
#endif

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntries.Count;
            var newNode = new Node
            {
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

            while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[splitNode.EntriesStartIndex + i] = invalidEntry;

            for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[newNode.EntriesStartIndex + i] = invalidEntry;

#if ENABLE_ASSERTS
            Assert.IsTrue(nodeEntries.Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries * 2);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private int GetNodeIndexToInsert(int nodeLevel, int entriesStartIndex, int entriesEndIndex, bool isFillCase,
            AABB newEntryAabb)
        {
            var levelNodes = _nodes[nodeLevel];

            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesStartIndex + MaxEntries; i++)
            {
                var nodeAabb = levelNodes[i].Aabb;
                if (i >= entriesEndIndex)
                {
                    if (isFillCase)
                        nodeIndex = i;

                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, newEntryAabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
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
            var sourceNodeAabb = AABB.Empty;

            var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Empty, fix.MaxValue);
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

#if ENABLE_ASSERTS
            Assert.IsTrue(sourceEntryIndex > -1);
#endif

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
                    if (aabbB == AABB.Empty)
                        continue;

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                var newEntryAabb = getAabbFunc.Invoke(newEntry);
                if (newEntryAabb == AABB.Empty)
                    continue;

                conjugatedArea = GetConjugatedArea(aabbA, newEntryAabb);
                if (conjugatedArea <= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            return (indexA, indexB);
        }

        private static bool IsSecondNodeTarget(in AABB nodeAabbA, in AABB nodeAabbB, in AABB conjugatedAabbA,
            in AABB conjugatedAabbB)
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

        private static (fix areaIncrease, fix sizeIncrease) GetAreaAndSizeIncrease(in AABB nodeAabb, in AABB conjugatedAabb)
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

        private static fix GetConjugatedArea(in AABB aabbA, in AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));
    }
}
