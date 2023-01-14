using System;
using App;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems.RTree
{
    public readonly struct TreeLevelNodesRange
    {
        public readonly int StartIndex;

        public TreeLevelNodesRange(int startIndex)
        {
            StartIndex = startIndex;
        }
    }

    public struct ThreadInsertJobResult
    {
        public int LeafEntriesCount;
        public int TreeHeight;
    }

    [BurstCompatible]
    public struct InsertJob : IJobParallelFor
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int RootNodeMinEntries = 2;

        [ReadOnly]
        public int TreeMaxHeight;

        [ReadOnly]
        public int NodesContainerCapacity;

        [ReadOnly]
        public int ResultEntriesContainerCapacity;

        [ReadOnly]
        public NativeArray<RTreeLeafEntry>.ReadOnly InputEntries;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RTreeNode> NodesContainer;
        public NativeArray<int> NodesEndIndicesContainer;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> RootNodesLevelIndices;

        public NativeArray<RTreeLeafEntry> ResultEntries;

        [WriteOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<ThreadInsertJobResult> PerThreadJobResults;

        private int _leafEntriesCounter;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> PerThreadWorkerIndices;

        [NativeDisableContainerSafetyRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<UnsafeAtomicCounter32> WorkersInUseCounter;

        [NativeSetThreadIndex]
        private int _threadIndex;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<int> _currentThreadNodesEndIndices;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<RTreeLeafEntry> _currentThreadResultEntries;

        private int WorkerIndex => PerThreadWorkerIndices[_threadIndex];
        private int RootNodesLevelIndex => RootNodesLevelIndices[WorkerIndex];
        private int TreeHeight => RootNodesLevelIndex + 1;

        private static readonly Func<RTreeNode, AABB> GetNodeAabb = node => node.Aabb;
        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;

        public void Execute(int entryIndex)
        {
            using var _ = Profiling.RTreeInsertJob.Auto();

            var workerIndex = PerThreadWorkerIndices[_threadIndex];
            if (workerIndex == -1)
            {
                workerIndex = WorkersInUseCounter[0].Add(1);
                PerThreadWorkerIndices[_threadIndex] = workerIndex;

                RootNodesLevelIndices[workerIndex] = 0;

                _leafEntriesCounter = 0;

                _currentThreadNodesEndIndices = NodesEndIndicesContainer
                    .GetSubArray(workerIndex * TreeMaxHeight, TreeMaxHeight);

                for (var i = 0; i < MaxEntries; i++)
                    NodesContainer[workerIndex * NodesContainerCapacity + i] = InvalidEntry<RTreeNode>.Entry;

                _currentThreadNodesEndIndices[RootNodesLevelIndex] = MaxEntries;
                for (var i = 1; i < _currentThreadNodesEndIndices.Length; i++)
                    _currentThreadNodesEndIndices[i] = 0;

                _currentThreadResultEntries = ResultEntries.GetSubArray(workerIndex * ResultEntriesContainerCapacity,
                    ResultEntriesContainerCapacity);
            }

            Insert(entryIndex);

            PerThreadJobResults[workerIndex] = new ThreadInsertJobResult
            {
                TreeHeight = TreeHeight
            };
        }

        private void Insert(int entryIndex)
        {
#if ENABLE_ASSERTS
            Assert.IsTrue(TreeHeight > 0);
#endif
            var startIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);
            var rootNodesCount = _currentThreadNodesEndIndices[RootNodesLevelIndex];
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            var entry = InputEntries[entryIndex];

            var targetNodeIndex = GetNodeIndexToInsert(in NodesContainer, startIndex, startIndex + MaxEntries,
                in entry.Aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(targetNodeIndex > -1);
#endif

            var targetNode = NodesContainer[targetNodeIndex];
            var extraNode = ChooseLeaf(ref targetNode, RootNodesLevelIndex, in entry);
#if ENABLE_ASSERTS
            Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(targetNode.EntriesCount, 0);
            Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

            NodesContainer[targetNodeIndex] = targetNode;

            if (extraNode.Aabb == InvalidEntry<RTreeNode>.Entry.Aabb)
                return;

#if ENABLE_ASSERTS
            Assert.AreNotEqual(extraNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(extraNode.EntriesCount, 0);
            Assert.AreNotEqual(extraNode.EntriesStartIndex, -1);
#endif

            var candidateNodeIndex = targetNodeIndex + 1;
            for (; candidateNodeIndex < startIndex + rootNodesCount; candidateNodeIndex++)
                if (NodesContainer[candidateNodeIndex].Aabb == InvalidEntry<RTreeNode>.Entry.Aabb)
                    break;

            if (candidateNodeIndex < startIndex + rootNodesCount)
            {
                NodesContainer[candidateNodeIndex] = extraNode;
                return;
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(
                NodesContainer
                    .GetSubArray(startIndex, math.max(rootNodesCount, targetNodeIndex - startIndex + 1))
                    .All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
#endif

            GrowTree(in extraNode);
        }

        private RTreeNode ChooseLeaf(ref RTreeNode node, int nodeLevelIndex, in RTreeLeafEntry entry)
        {
            var isLeafLevel = nodeLevelIndex == 0;
            if (isLeafLevel)
            {
                using var _ = Profiling.RTreeLeafNodesUpdate.Auto();

                if (node.EntriesCount == MaxEntries)
                    return SplitNode(ref node, ref _currentThreadResultEntries, ref _leafEntriesCounter, entry,
                        GetLeafEntryAabb);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntriesCounter;
                    _leafEntriesCounter += MaxEntries;

                    for (var i = 0; i < MaxEntries; i++)
                        _currentThreadResultEntries[node.EntriesStartIndex + i] = InvalidEntry<RTreeLeafEntry>.Entry;
                }

                _currentThreadResultEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                node.EntriesCount++;

                return InvalidEntry<RTreeNode>.Entry;
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
            var nodeLevelsRangeStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
            Assert.IsTrue(entriesStartIndex >= nodeLevelsRangeStartIndex);
            var nodeLevelsRangeCapacity = GetNodeLevelCapacity(nodeLevelIndex - 1);
            Assert.IsTrue(entriesEndIndex < nodeLevelsRangeStartIndex + nodeLevelsRangeCapacity);
            Assert.IsTrue(entriesEndIndex <= nodeLevelsRangeStartIndex + _currentThreadNodesEndIndices[nodeLevelIndex - 1]);
#endif

            var childNodeLevelIndex = nodeLevelIndex - 1;
            var childNodeIndex = GetNodeIndexToInsert(in NodesContainer, in node, in entry.Aabb);
#if ENABLE_ASSERTS
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);
#endif

            var targetChildNode = NodesContainer[childNodeIndex];
            var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, in entry);

            NodesContainer[childNodeIndex] = targetChildNode;

            if (extraChildNode.Aabb == InvalidEntry<RTreeNode>.Entry.Aabb)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
#if ENABLE_ASSERTS
                Assert.IsFalse(targetChildNode.EntriesStartIndex < 0);
                Assert.IsTrue(targetChildNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
                return InvalidEntry<RTreeNode>.Entry;
            }

            if (entriesCount == MaxEntries)
            {
                var childNodesLevelStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
                var childNodesLevelEndIndex = childNodesLevelStartIndex + _currentThreadNodesEndIndices[childNodeLevelIndex];

                var extraNode = SplitNode(ref node, ref NodesContainer, ref childNodesLevelEndIndex, extraChildNode,
                    GetNodeAabb);
                _currentThreadNodesEndIndices[childNodeLevelIndex] = childNodesLevelEndIndex - childNodesLevelStartIndex;

                return extraNode;
            }

            NodesContainer[entriesStartIndex + entriesCount] = extraChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
            node.EntriesCount++;

#if ENABLE_ASSERTS
            Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries || IsLevelRoot(nodeLevelIndex) &&
                node.EntriesCount is >= RootNodeMinEntries and <= MaxEntries);
#endif
            return InvalidEntry<RTreeNode>.Entry;
        }

        private void GrowTree(in RTreeNode newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodesStartIndex =
                GetNodeLevelStartIndex(RootNodesLevelIndex); //_currentThreadNodeRangesByLevel[RootNodesLevelIndex].StartIndex;
            var rootNodesCount = _currentThreadNodesEndIndices[RootNodesLevelIndex];

#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
            Assert.IsFalse(rootNodesCount + MaxEntries > GetNodeLevelCapacity(RootNodesLevelIndex));
#endif

            var newRootNodeA = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = rootNodesStartIndex,
                EntriesCount = rootNodesCount
            };

            var rootNodesLevelEndIndex = rootNodesStartIndex + rootNodesCount;
            var newRootNodeB = SplitNode(ref newRootNodeA, ref NodesContainer, ref rootNodesLevelEndIndex, newEntry,
                GetNodeAabb);

            _currentThreadNodesEndIndices[RootNodesLevelIndex] = rootNodesLevelEndIndex - rootNodesStartIndex;
            ++RootNodesLevelIndices[WorkerIndex];

            _currentThreadNodesEndIndices[RootNodesLevelIndex] = MaxEntries;

            var newRootNodesStartIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);

            NodesContainer[newRootNodesStartIndex + 0] = newRootNodeA;
            NodesContainer[newRootNodesStartIndex + 1] = newRootNodeB;

            for (var i = 2; i < MaxEntries; i++)
                NodesContainer[newRootNodesStartIndex + i] = InvalidEntry<RTreeNode>.Entry;

#if ENABLE_ASSERTS
            var newRootNodes = NodesContainer.GetSubArray(newRootNodesStartIndex, MaxEntries).ToArray();
            Assert.IsTrue(newRootNodes.Count(n => n.Aabb != AABB.Empty) >= RootNodeMinEntries);
            Assert.IsTrue(newRootNodes.Count(n => n.EntriesStartIndex != -1) >= RootNodeMinEntries);
            Assert.IsTrue(newRootNodes.Count(n => n.EntriesCount > 0) >= RootNodeMinEntries);
#endif
        }

        private static RTreeNode SplitNode<T>(ref RTreeNode splitNode, ref NativeArray<T> nodeEntries,
            ref int nodeEntriesEndIndex, in T newEntry, Func<T, AABB> getAabbFunc)
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

            var (indexA, indexB) = FindLargestEntriesPair(in nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
#if ENABLE_ASSERTS
            Assert.IsTrue(indexA > -1 && indexB > -1);
#endif

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntriesEndIndex;
            var newNode = new RTreeNode
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
                    IsSecondNodeTarget(in splitNodeAabb, in newNodeAabb, in conjugatedAabbA, in conjugatedAabbB);
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
                    .Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries);

            Assert.IsTrue(
                nodeEntries
                    .GetSubArray(newNode.EntriesStartIndex, newNode.EntriesCount)
                    .Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private bool IsLevelRoot(int nodeLevelIndex) =>
            nodeLevelIndex == RootNodesLevelIndex;

        private static int GetNodeIndexToInsert(in NativeArray<RTreeNode> nodeEntries, in RTreeNode parentNode,
            in AABB newEntryAabb)
        {
            var entriesStartIndex = parentNode.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + parentNode.EntriesCount;

            return GetNodeIndexToInsert(in nodeEntries, entriesStartIndex, entriesEndIndex, in newEntryAabb);
        }

        private static int GetNodeIndexToInsert(in NativeArray<RTreeNode> nodeEntries, int entriesStartIndex,
            int entriesEndIndex, in AABB newEntryAabb)
        {
            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
            {
                var entry = nodeEntries[i];
                if (entry.EntriesCount < MinEntries)
                {
                    if (i < entriesStartIndex + MinEntries)
                        return i;

                    break;
                }

#if ENABLE_ASSERTS
                Assert.AreNotEqual(AABB.Empty, entry.Aabb);
                Assert.AreNotEqual(-1, entry.EntriesStartIndex);
                Assert.AreNotEqual(0, entry.EntriesCount);
#endif

                var conjugatedArea = GetConjugatedArea(in entry.Aabb, in newEntryAabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
        }

        private static void FillNodes<T>(ref NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc, ref RTreeNode splitNode,
            ref RTreeNode newNode)
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

                var conjugatedArea = GetConjugatedArea(in targetNodeAabb, in entryAabb);
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

                    conjugatedArea = GetConjugatedArea(in aabbA, in aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                var newEntryAabb = getAabbFunc.Invoke(newEntry);
                if (newEntryAabb == AABB.Empty)
                    continue;

                conjugatedArea = GetConjugatedArea(in aabbA, in newEntryAabb);
                if (conjugatedArea <= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            return (indexA, indexB);
        }

        private static bool IsSecondNodeTarget(in AABB nodeAabbA, in AABB nodeAabbB, in AABB conjugatedAabbA,
            in AABB conjugatedAabbB)
        {
            var (areaIncreaseA, deltaA) = GetAreaAndSizeIncrease(in nodeAabbA, in conjugatedAabbA);
            var (areaIncreaseB, deltaB) = GetAreaAndSizeIncrease(in nodeAabbB, in conjugatedAabbB);

            if (areaIncreaseA > areaIncreaseB == deltaA > deltaB)
                return areaIncreaseA > areaIncreaseB;

            if (areaIncreaseA == areaIncreaseB)
                return deltaA > deltaB;

            if (deltaA == deltaB)
                return areaIncreaseA > areaIncreaseB;

            return true;
        }

        private int GetNodeLevelStartIndex(int nodeLevelIndex)
        {
            var index = math.pow(MaxEntries, TreeMaxHeight + 1) / (MaxEntries - 1);
            index *= 1f - 1f / math.pow(MaxEntries, nodeLevelIndex);
            index += WorkerIndex * NodesContainerCapacity;

            return (int) index;
        }

        private static int GetNodeLevelCapacity(int nodeLevelIndex) =>
            (int) math.pow(MaxEntries, MaxEntries - nodeLevelIndex);

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
