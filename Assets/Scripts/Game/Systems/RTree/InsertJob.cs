using System;
using App;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Systems.RTree
{
    public readonly struct LevelRange
    {
        public readonly int StartIndex;
        public readonly int Capacity;

        public LevelRange(int startIndex, int capacity)
        {
            StartIndex = startIndex;
            Capacity = capacity;
        }
    }

    public struct Result
    {
        public int LeafEntriesCount;
        public int TreeHeight;
    }

    [BurstCompatible]
    public struct InsertJob : IJob
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int RootNodeMinEntries = 2;

        [ReadOnly]
        public int LeafEntriesMaxCount;

        [ReadOnly]
        public int EntriesCount;

        [ReadOnly]
        public NativeArray<RTreeLeafEntry>.ReadOnly InputEntries;

        [ReadOnly]
        public NativeArray<LevelRange>.ReadOnly NodeLevelsRanges;

        public NativeArray<Node> Nodes;
        public NativeArray<RTreeLeafEntry> LeafEntries;

        public NativeArray<int> NodeLevelsLengths; // :TODO: get from GetSubArray()

        [WriteOnly]
        public NativeArray<Result> Result;

        private int _leafEntriesCount;
        // private UnsafeAtomicCounter32 _leafEntriesCount;

        private int _rootNodesLevelIndex;

        public int TreeHeight => _rootNodesLevelIndex + 1;

        private static readonly Func<Node, AABB> GetNodeAabb = node => node.Aabb;
        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;

        public void Execute()
        {
            _rootNodesLevelIndex = 0;
            _leafEntriesCount = 0;

            NodeLevelsLengths[_rootNodesLevelIndex] = MaxEntries;
            for (var i = 1; i < NodeLevelsLengths.Length; i++)
                NodeLevelsLengths[i] = 0;

#if ENABLE_ASSERTS
            Assert.IsTrue(EntriesCount <= LeafEntriesMaxCount);
#endif

            for (var i = 0; i < MaxEntries; i++)
                Nodes[i] = InvalidEntry<Node>.Entry;

            Profiling.RTreeInsert.Begin();

            for (var i = 0; i < EntriesCount; i++)
                Insert(i);

            Profiling.RTreeInsert.End();

            Result[0] = new Result
            {
                TreeHeight = TreeHeight,
                LeafEntriesCount = _leafEntriesCount
            };
        }

        private void Insert(int entryIndex)
        {
#if ENABLE_ASSERTS
            Assert.IsTrue(TreeHeight > 0);
#endif
            var startIndex = NodeLevelsRanges[_rootNodesLevelIndex].StartIndex;
            var rootNodesCount = NodeLevelsLengths[_rootNodesLevelIndex];
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif
            var rootNodes = Nodes.GetSubArray(startIndex, rootNodesCount);

            var nonEmptyNodesCount = 0;
            for (var i = 0; i < MaxEntries; i++)
                nonEmptyNodesCount += rootNodes[i].Aabb != AABB.Empty ? 1 : 0;

#if ENABLE_ASSERTS
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.ToArray().Count(n => n.EntriesCount > 0));
#endif
            var entry = InputEntries[entryIndex];

            var getFirstEmptyNode = nonEmptyNodesCount < RootNodeMinEntries;
            var nodeIndex = GetNodeIndexToInsert(in Nodes, GetNodeAabb, startIndex, startIndex + nonEmptyNodesCount,
                getFirstEmptyNode, in entry.Aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(nodeIndex > -1);
#endif

            var targetNode = Nodes[nodeIndex];
            var extraNode = ChooseLeaf(ref targetNode, _rootNodesLevelIndex, in entry);
#if ENABLE_ASSERTS
            Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(targetNode.EntriesCount, 0);
            Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

            Nodes[nodeIndex] = targetNode;

            if (extraNode.Aabb == InvalidEntry<Node>.Entry.Aabb)
                return;

#if ENABLE_ASSERTS
            Assert.AreNotEqual(extraNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(extraNode.EntriesCount, 0);
            Assert.AreNotEqual(extraNode.EntriesStartIndex, -1);
#endif

            var candidateNodeIndex = nodeIndex + 1;
            for (; candidateNodeIndex < startIndex + rootNodesCount; candidateNodeIndex++)
                if (Nodes[candidateNodeIndex].Aabb == InvalidEntry<Node>.Entry.Aabb)
                    break;

            if (candidateNodeIndex < startIndex + rootNodesCount)
            {
                Nodes[candidateNodeIndex] = extraNode;
                return;
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(
                Nodes
                    .GetSubArray(startIndex, math.max(rootNodesCount, nodeIndex - startIndex + 1))
                    .ToArray()
                    .All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
#endif

            GrowTree(in extraNode);
        }

        private Node ChooseLeaf(ref Node node, int nodeLevelIndex, in RTreeLeafEntry entry)
        {
            var isLeafLevel = nodeLevelIndex == 0;
            if (isLeafLevel)
            {
                using var _ = Profiling.RTreeLeafNodesUpdate.Auto();

                if (node.EntriesCount == MaxEntries)
                    return SplitNode(ref node, ref LeafEntries, ref _leafEntriesCount, entry, GetLeafEntryAabb);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntriesCount;
                    _leafEntriesCount += MaxEntries;

                    for (var i = node.EntriesStartIndex; i < _leafEntriesCount; i++)
                        LeafEntries[i] = InvalidEntry<RTreeLeafEntry>.Entry;
                }

                LeafEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                node.EntriesCount++;

                return InvalidEntry<Node>.Entry;
            }

            using var __ = Profiling.RTreeNodesUpdate.Auto();

            var entriesCount = node.EntriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries || IsLevelRoot(nodeLevelIndex) &&
                entriesCount is >= RootNodeMinEntries and <= MaxEntries);
#endif

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesStartIndex > -1);
            var nodeLevelsRange = NodeLevelsRanges[nodeLevelIndex - 1];
            Assert.IsTrue(entriesStartIndex >= nodeLevelsRange.StartIndex);
            Assert.IsTrue(entriesEndIndex < nodeLevelsRange.StartIndex + nodeLevelsRange.Capacity);
            Assert.IsTrue(entriesEndIndex <= nodeLevelsRange.StartIndex + NodeLevelsLengths[nodeLevelIndex - 1]);
#endif

            var childNodeLevelIndex = nodeLevelIndex - 1;
            var childNodeIndex =
                GetNodeIndexToInsert(in Nodes, GetNodeAabb, entriesStartIndex, entriesEndIndex, false, entry.Aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);
#endif

            var targetChildNode = Nodes[childNodeIndex];
            var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, entry);

            Nodes[childNodeIndex] = targetChildNode;

            if (extraChildNode.Aabb == InvalidEntry<Node>.Entry.Aabb)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                return InvalidEntry<Node>.Entry;
            }

            if (entriesCount == MaxEntries)
            {
                var childNodesLevelStartIndex = NodeLevelsRanges[nodeLevelIndex - 1].StartIndex;
                var childNodesLevelEndIndex = childNodesLevelStartIndex + NodeLevelsLengths[childNodeLevelIndex];

                var extraNode = SplitNode(ref node, ref Nodes, ref childNodesLevelEndIndex, extraChildNode, GetNodeAabb);
                NodeLevelsLengths[childNodeLevelIndex] = childNodesLevelEndIndex - childNodesLevelStartIndex;

                return extraNode;
            }

            Nodes[entriesStartIndex + entriesCount] = extraChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
            node.EntriesCount++;

#if ENABLE_ASSERTS
            Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries || IsLevelRoot(nodeLevelIndex) &&
                node.EntriesCount is >= RootNodeMinEntries and <= MaxEntries);
#endif
            return InvalidEntry<Node>.Entry;
        }

        private void GrowTree(in Node newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodesStartIndex = NodeLevelsRanges[_rootNodesLevelIndex].StartIndex;
            var rootNodesCount = NodeLevelsLengths[_rootNodesLevelIndex];

#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
            Assert.IsFalse(rootNodesCount + MaxEntries > NodeLevelsRanges[_rootNodesLevelIndex].Capacity);
#endif

            var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = rootNodesStartIndex,
                EntriesCount = rootNodesCount
            };

            var rootNodesLevelEndIndex = rootNodesStartIndex + rootNodesCount;
            var newRootNodeB = SplitNode(ref newRootNodeA, ref Nodes, ref rootNodesLevelEndIndex, newEntry, GetNodeAabb);

            NodeLevelsLengths[_rootNodesLevelIndex] = rootNodesLevelEndIndex - rootNodesStartIndex;
            ++_rootNodesLevelIndex;

#if ENABLE_ASSERTS
            Assert.IsFalse(NodeLevelsRanges.Length < TreeHeight);
            Assert.IsFalse(NodeLevelsLengths.Length < TreeHeight);
#endif

            NodeLevelsLengths[_rootNodesLevelIndex] = MaxEntries;

            var newRootNodesStartIndex = NodeLevelsRanges[_rootNodesLevelIndex].StartIndex;

            Nodes[newRootNodesStartIndex + 0] = newRootNodeA;
            Nodes[newRootNodesStartIndex + 1] = newRootNodeB;

            for (var i = 2; i < MaxEntries; i++)
                Nodes[newRootNodesStartIndex + i] = InvalidEntry<Node>.Entry;

#if ENABLE_ASSERTS
            var newRootNodes = Nodes.GetSubArray(newRootNodesStartIndex, MaxEntries);

            var nodes = newRootNodes
                .ToArray()
                // .Take(rootNodesCount)
                .ToArray();

            Assert.IsTrue(nodes.Count(n => n.Aabb != AABB.Empty) >= RootNodeMinEntries);
            Assert.IsTrue(nodes.Count(n => n.EntriesStartIndex != -1) >= RootNodeMinEntries);
            Assert.IsTrue(nodes.Count(n => n.EntriesCount > 0) >= RootNodeMinEntries);
#endif
        }

        private static Node SplitNode<T>(ref Node splitNode, ref NativeArray<T> nodeEntries, ref int nodeEntriesEndIndex,
            in T newEntry, Func<T, AABB> getAabbFunc)
            where T : unmanaged
        {
            using var _ = Profiling.RTreeSplitNode.Auto();

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
            var newEntriesStartIndex = nodeEntriesEndIndex;
            var newNode = new Node
            {
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            var invalidEntry = InvalidEntry<T>.Entry;

            nodeEntriesEndIndex += MaxEntries;

            nodeEntries[newEntriesStartIndex] = newNodeStartEntry;
            for (var i = newEntriesStartIndex + 1; i < newEntriesStartIndex; i++)
                nodeEntries[i] = invalidEntry;

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

                var isNewNodeTarget =
                    IsSecondNodeTarget(splitNodeAabb, newNodeAabb, conjugatedAabbA, conjugatedAabbB);
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (isNewNodeTarget || targetNode.EntriesCount != i)
                    nodeEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(ref nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[splitNode.EntriesStartIndex + i] = invalidEntry;

            for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[newNode.EntriesStartIndex + i] = invalidEntry;

#if ENABLE_ASSERTS
            Assert.IsTrue(
                nodeEntries
                    .GetSubArray(splitNode.EntriesStartIndex, splitNode.EntriesCount)
                    .ToArray()
                    .Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries);

            Assert.IsTrue(
                nodeEntries
                    .GetSubArray(newNode.EntriesStartIndex, newNode.EntriesCount)
                    .ToArray()
                    .Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private bool IsLevelRoot(int nodeLevelIndex) =>
            nodeLevelIndex == _rootNodesLevelIndex;

        private static int GetNodeIndexToInsert<T>(in NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc,
            int entriesStartIndex, int entriesEndIndex, bool getFirstEmptyNode, in AABB aabb)
            where T : struct
        {
            if (getFirstEmptyNode)
                return entriesEndIndex;

            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesStartIndex + MaxEntries; i++)
            {
                if (i >= entriesEndIndex)
                    break;

                var nodeAabb = getAabbFunc(nodeEntries[i]);
                var conjugatedArea = GetConjugatedArea(nodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
        }

        private static void FillNodes<T>(ref NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc, ref Node splitNode,
            ref Node newNode)
            where T : struct
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
            in NativeArray<T> nodeEntries,
            T newEntry,
            int startIndex,
            int endIndex,
            Func<T, AABB> getAabbFunc)
            where T : struct
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

        private static fix GetConjugatedArea(in AABB aabbA, in AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));
    }
}
