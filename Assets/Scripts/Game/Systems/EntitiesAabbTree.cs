using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using UnityEngine;
using UnityEngine.Assertions;

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
            Aabb = AABB.Invalid,
            EntriesStartIndex = -1,
            EntriesCount = 0
        };

        public int TreeHeight => _nodes.Count;

        public IEnumerable<Node> RootNodes =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : _nodes[0].TakeWhile(n => n.Aabb != AABB.Invalid);

        public IEnumerable<Node> GetNodes(int levelIndex, IEnumerable<int> indices) =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : indices.Select(i => _nodes[levelIndex][i]);

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep)
        {
            _nodes.Clear();
            _leafEntries.Clear();

            if (filter.IsEmpty())
                return;

            _nodes.Add(
                Enumerable
                    .Repeat(new Node
                    {
                        Aabb = AABB.Invalid,

                        EntriesStartIndex = -1,
                        EntriesCount = 0
                    }, MaxEntries)
                    .ToList());

            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);

                ref var transformComponent = ref filter.Get1(index);
                var currentPosition = transformComponent.WorldPosition;

                var hasPrevFrameDataComponent = entity.Has<PrevFrameDataComponent>();
                ref var prevFrameDataComponent = ref entity.Get<PrevFrameDataComponent>();

                var lastPosition = hasPrevFrameDataComponent
                    ? prevFrameDataComponent.LastWorldPosition
                    : transformComponent.WorldPosition;

                var position = lastPosition + (currentPosition - lastPosition) * simulationSubStep;

                var aabb = entity.GetEntityColliderAABB(position);
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
            if (!IntersectedByLine(p0, p1, node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex + 1 == TreeHeight)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    if (_leafEntries[i].Entity.Has<HeroTag>())
                        Debug.LogWarning("hero tag");
                    if (!IntersectedByLine(p0, p1, _leafEntries[i].Aabb))
                        continue;

                    result.Add(_leafEntries[i]);
                }

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByLine(p0, p1, result, levelIndex + 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result)
        {
            const int levelIndex = 0;

            var topLevelNodes = _nodes[levelIndex];
            for (var nodeIndex = 0; nodeIndex < topLevelNodes.Count; nodeIndex++)
                QueryNodesByAabb(aabb, result, levelIndex, nodeIndex);
        }

        private void QueryNodesByAabb(AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result, int levelIndex,
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

        private void Insert(EcsEntity entity, AABB aabb)
        {
            const int rootLevelIndex = 0;
            const int rootEntriesStartIndex = 0;

            var rootNodes = _nodes[rootLevelIndex];
            Assert.AreEqual(MaxEntries, rootNodes.Count);

            var nonEmptyNodesCount = rootNodes.Count(n => n.Aabb != AABB.Invalid);
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.Count(n => n.EntriesCount > 0));

            var nodeIndex = GetNodeIndexToInsert(rootLevelIndex, rootEntriesStartIndex,
                rootEntriesStartIndex + nonEmptyNodesCount, nonEmptyNodesCount < MinEntries, aabb);
            Assert.IsTrue(nodeIndex > -1);

            var (a, b) = ChooseLeaf(rootLevelIndex, nodeIndex, aabb, entity);
            Assert.IsTrue(a.HasValue);

            rootNodes[nodeIndex] = a.Value;

            if (!b.HasValue)
                return;

            var candidateNodeIndex = rootNodes.FindIndex(n => n.Aabb == AABB.Invalid);
            if (candidateNodeIndex != -1)
            {
                rootNodes[candidateNodeIndex] = b.Value;
                return;
            }

            Assert.IsTrue(rootNodes.All(n => n.Aabb != AABB.Invalid && n.EntriesCount > 0));

            GrowTree(b.Value);
        }

        private (Node? a, Node? b) ChooseLeaf(int nodeLevelIndex, int nodeIndex, AABB aabb, EcsEntity entity)
        {
            var levelNodes = _nodes[nodeLevelIndex];
            var node = levelNodes[nodeIndex];

            var isLeafLevel = nodeLevelIndex + 1 == TreeHeight;
            if (isLeafLevel)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitNode(nodeLevelIndex, nodeIndex, _leafEntries, (aabb, entity), GetLeafEntryAabb, default);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;
                    _leafEntries.AddRange(Enumerable.Repeat(default((AABB, EcsEntity)), MaxEntries));
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (Aabb: aabb, Entity: entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                node.EntriesCount++;

                return (a: node, b: null);
            }

            var entriesCount = node.EntriesCount;
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
            Assert.IsTrue(entriesStartIndex > -1);

            var childNodeLevelIndex = nodeLevelIndex + 1;
            var childNodeIndex = GetNodeIndexToInsert(childNodeLevelIndex, entriesStartIndex, entriesEndIndex, false, aabb);
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);

            var (a, b) = ChooseLeaf(childNodeLevelIndex, childNodeIndex, aabb, entity);
            Assert.IsTrue(a.HasValue);

            var childLevelNodes = _nodes[childNodeLevelIndex];
            childLevelNodes[childNodeIndex] = a.Value;

            if (!b.HasValue)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                return (a: node, b: null);
            }

            var newChildNode = b.Value;
            if (entriesCount == MaxEntries)
                return SplitNode(nodeLevelIndex, nodeIndex, childLevelNodes, newChildNode, GetNodeAabb, _invalidNodeEntry);

            childLevelNodes[entriesStartIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return (a: node, b: null);
        }

        private void GrowTree(Node newEntry)
        {
            var topLevelNodes = _nodes[0];
            Assert.AreEqual(MaxEntries, topLevelNodes.Count);

            var (childNodeIndexA, childNodeIndexB) = FindLargestEntriesPair(topLevelNodes, newEntry, 0, 4, GetNodeAabb);
            Assert.IsTrue(childNodeIndexA > -1 && childNodeIndexB > -1 && childNodeIndexA < childNodeIndexB);

            var childNodesStartIndexA = 0;
            var childNodesStartIndexB = topLevelNodes.Count;
            var childNodesEndIndexB = topLevelNodes.Count + MaxEntries;
            Assert.AreEqual(MaxEntries, childNodesStartIndexB);

            var childNodeA = topLevelNodes[childNodeIndexA];
            var childNodeB = childNodeIndexB < MaxEntries ? topLevelNodes[childNodeIndexB] : newEntry;

            topLevelNodes.AddRange(Enumerable.Repeat(_invalidNodeEntry, MaxEntries));

            var aabbA = childNodeA.Aabb;
            var aabbB = childNodeB.Aabb;

            var newRootNodeA = new Node
            {
                Aabb = aabbA,

                EntriesStartIndex = childNodesStartIndexA,
                EntriesCount = 0
            };

            var newRootNodeB = new Node
            {
                Aabb = aabbB,

                EntriesStartIndex = childNodesStartIndexB,
                EntriesCount = 1
            };

            topLevelNodes[childNodesStartIndexB++] = childNodeB;

            for (var rootNodeIndexC = 0; rootNodeIndexC < topLevelNodes.Count; rootNodeIndexC++)
            {
                if (rootNodeIndexC == childNodeIndexA)
                {
                    topLevelNodes[childNodesStartIndexA] = childNodeA;

                    ++childNodesStartIndexA;
                    ++newRootNodeA.EntriesCount;

                    continue;
                }

                if (rootNodeIndexC == childNodeIndexB)
                    continue;

                var nodeC = topLevelNodes[rootNodeIndexC];
                var aabbC = nodeC.Aabb;

                var conjugatedAabbA = fix.AABBs_conjugate(aabbA, aabbC);
                var conjugatedAabbB = fix.AABBs_conjugate(aabbB, aabbC);

                var isSecondNodeTarget = IsSecondNodeTarget(aabbA, aabbB, conjugatedAabbA, conjugatedAabbB);
                if (isSecondNodeTarget && childNodesStartIndexB < childNodesEndIndexB)
                {
                    topLevelNodes[childNodesStartIndexB++] = nodeC;

                    newRootNodeB.Aabb = fix.AABBs_conjugate(newRootNodeB.Aabb, aabbC);
                    ++newRootNodeB.EntriesCount;
                }
                else
                {
                    topLevelNodes[childNodesStartIndexA++] = nodeC;

                    newRootNodeA.Aabb = fix.AABBs_conjugate(newRootNodeA.Aabb, aabbC);
                    ++newRootNodeA.EntriesCount;
                }
            }

            var newRootNodes = new List<Node>(new[] { newRootNodeA, newRootNodeB });
            newRootNodes.AddRange(Enumerable
                .Repeat(new Node
                {
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = -1,
                    EntriesCount = 0
                }, MaxEntries - newRootNodes.Count));

            _nodes.Insert(0, newRootNodes);
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

        private (Node a, Node b) SplitNode<T>(int splitNodeLevel, int splitNodeIndex, List<T> nodeEntries,
            T newEntry, Func<T, AABB> getAabbFunc, T invalidEntry)
        {
            var levelNodes = _nodes[splitNodeLevel];
            var splitNode = levelNodes[splitNodeIndex];

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

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);

            return (a: splitNode, b: newNode);
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
