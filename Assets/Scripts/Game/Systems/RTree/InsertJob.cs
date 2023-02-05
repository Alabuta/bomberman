using System.Linq.Expressions;
using Math.FixedPointMath;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Game.Systems.RTree
{
    public struct InsertJobReadOnlyData
    {
        [ReadOnly]
        public int TreeMaxHeight;

        [ReadOnly]
        public int EntriesTotalCount;

        [ReadOnly]
        public int PerWorkerEntriesCount;

        [ReadOnly]
        public int NodesContainerCapacity;

        [ReadOnly]
        public int ResultEntriesContainerCapacity;

        [ReadOnly]
        public NativeArray<RTreeLeafEntry>.ReadOnly InputEntries;
    }

    public struct InsertJobSharedWriteData
    {
        [NativeDisableContainerSafetyRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<UnsafeAtomicCounter32> CountersContainer;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> PerThreadWorkerIndices;

        public NativeArray<RTreeLeafEntry> ResultEntries;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RTreeNode> NodesContainer;

        public NativeArray<int> NodesEndIndicesContainer;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> RootNodesLevelIndices;
    }

    public partial class AabbRTree
    {
        [BurstCompile(DisableSafetyChecks = true, CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance)]
        public struct InsertJob : IInsertJob
        {
            [ReadOnly]
            public InsertJobReadOnlyData ReadOnlyData;

            public InsertJobSharedWriteData SharedWriteData;

            [NativeDisableContainerSafetyRestriction]
            private NativeArray<int> _currentThreadNodesEndIndices;

            [NativeDisableContainerSafetyRestriction]
            private NativeArray<RTreeLeafEntry> _currentThreadResultEntries;

            [NativeSetThreadIndex]
            private int _workerThreadIndex;

            private int _jobIndex;

            private int _leafEntriesCounter;

            private int RootNodesLevelIndex => SharedWriteData.RootNodesLevelIndices[_jobIndex];

            public void Execute(ref PerWorkerData perWorkerData, int entriesStartIndex, int count)
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeInsertJob.Auto();
#endif

                _currentThreadNodesEndIndices = perWorkerData.CurrentThreadNodesEndIndices;
                _currentThreadResultEntries = perWorkerData.CurrentThreadResultEntries;
                _jobIndex = perWorkerData.WorkerIndex;

                var entriesEndIndex = entriesStartIndex + count;
                entriesEndIndex = math.min(entriesEndIndex, ReadOnlyData.EntriesTotalCount);

#if ENABLE_ASSERTS
                Assert.IsFalse(entriesEndIndex - entriesStartIndex > ReadOnlyData.PerWorkerEntriesCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.EntriesTotalCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.InputEntries.Length);
#endif

                for (var entryIndex = entriesStartIndex; entryIndex < entriesEndIndex; entryIndex++)
                    Insert(entryIndex);

                perWorkerData.LeafEntriesCounter += _leafEntriesCounter;
            }

            public void Execute(int entriesStartIndex, int count)
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeInsertJob.Auto();
#endif
                var jobIndex = SharedWriteData.PerThreadWorkerIndices[_workerThreadIndex];
                var isWorkerInitialized = jobIndex != -1;
                if (!isWorkerInitialized)
                {
#if ENABLE_PROFILING
                    Profiling.RTreeInsertJobInitWorker.Begin();
#endif

                    jobIndex = SharedWriteData.CountersContainer[0].Add(1);
                    SharedWriteData.PerThreadWorkerIndices[_workerThreadIndex] = jobIndex;

                    _leafEntriesCounter = 0;
                    SharedWriteData.RootNodesLevelIndices[jobIndex] = 0;
#if ENABLE_PROFILING
                    Profiling.RTreeInsertJobInitWorker.End();
#endif
                }

/*#if ENABLE_ASSERTS
                Assert.IsFalse(count > EntriesScheduleBatch);
                Assert.IsFalse(jobIndex > WorkersCount);
#endif*/

                var treeMaxHeight = ReadOnlyData.TreeMaxHeight;
                var resultEntriesContainerCapacity = ReadOnlyData.ResultEntriesContainerCapacity;

                _currentThreadNodesEndIndices = SharedWriteData.NodesEndIndicesContainer
                    .GetSubArray(_jobIndex * treeMaxHeight, treeMaxHeight);

                _currentThreadResultEntries = SharedWriteData.ResultEntries
                    .GetSubArray(_jobIndex * resultEntriesContainerCapacity, resultEntriesContainerCapacity);

                if (!isWorkerInitialized)
                {
#if ENABLE_PROFILING
                    Profiling.RTreeInsertJobInitWorker.Begin();
#endif
                    _currentThreadNodesEndIndices[RootNodesLevelIndex] = MaxEntries;
                    for (var i = 1; i < _currentThreadNodesEndIndices.Length; i++)
                        _currentThreadNodesEndIndices[i] = 0;

                    var nodesContainerStartIndex = _jobIndex * ReadOnlyData.NodesContainerCapacity;
                    for (var i = 0; i < MaxEntries; i++)
                        SharedWriteData.NodesContainer[nodesContainerStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;
#if ENABLE_PROFILING
                    Profiling.RTreeInsertJobInitWorker.End();
#endif
                }

                // var entriesStartIndex = _jobIndex * ReadOnlyData.PerWorkerEntriesCount;
                var entriesEndIndex = entriesStartIndex + count; //ReadOnlyData.PerWorkerEntriesCount;
                entriesEndIndex = math.min(entriesEndIndex, ReadOnlyData.EntriesTotalCount);

#if ENABLE_ASSERTS
                Assert.IsFalse(entriesEndIndex - entriesStartIndex > ReadOnlyData.PerWorkerEntriesCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.EntriesTotalCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.InputEntries.Length);
#endif

                for (var entryIndex = entriesStartIndex; entryIndex < entriesEndIndex; entryIndex++)
                    Insert(entryIndex);
            }

            public void Execute(int jobIndex)
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeInsertJob.Auto();
#endif

                _jobIndex = jobIndex;
                _leafEntriesCounter = 0;
                SharedWriteData.RootNodesLevelIndices[jobIndex] = 0;

                var treeMaxHeight = ReadOnlyData.TreeMaxHeight;
                var resultEntriesContainerCapacity = ReadOnlyData.ResultEntriesContainerCapacity;

                _currentThreadNodesEndIndices = SharedWriteData.NodesEndIndicesContainer
                    .GetSubArray(jobIndex * treeMaxHeight, treeMaxHeight);

                _currentThreadNodesEndIndices[RootNodesLevelIndex] = MaxEntries;
                for (var i = 1; i < _currentThreadNodesEndIndices.Length; i++)
                    _currentThreadNodesEndIndices[i] = 0;

                _currentThreadResultEntries = SharedWriteData.ResultEntries
                    .GetSubArray(jobIndex * resultEntriesContainerCapacity, resultEntriesContainerCapacity);

                var nodesContainerStartIndex = jobIndex * ReadOnlyData.NodesContainerCapacity;
                for (var i = 0; i < MaxEntries; i++)
                    SharedWriteData.NodesContainer[nodesContainerStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

                var entriesStartIndex = jobIndex * ReadOnlyData.PerWorkerEntriesCount;
                var entriesEndIndex = entriesStartIndex + ReadOnlyData.PerWorkerEntriesCount;
                entriesEndIndex = math.min(entriesEndIndex, ReadOnlyData.EntriesTotalCount);

#if ENABLE_ASSERTS
                Assert.IsFalse(entriesEndIndex - entriesStartIndex > ReadOnlyData.PerWorkerEntriesCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.EntriesTotalCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.InputEntries.Length);
#endif

                for (var entryIndex = entriesStartIndex; entryIndex < entriesEndIndex; entryIndex++)
                    Insert(entryIndex);
            }

            private void Insert(int entryIndex)
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeInsertEntry.Auto();
#endif

#if ENABLE_ASSERTS
                Assert.IsTrue(RootNodesLevelIndex > -1);
#endif
                var startIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);
                var rootNodesCount = _currentThreadNodesEndIndices[RootNodesLevelIndex];
#if ENABLE_ASSERTS
                Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

                var entry = ReadOnlyData.InputEntries[entryIndex];

                ref var nodesContainer = ref SharedWriteData.NodesContainer;
                var targetNodeIndex = GetNodeIndexToInsert(in nodesContainer, startIndex, startIndex + MaxEntries,
                    in entry.Aabb);
#if ENABLE_ASSERTS
                Assert.IsTrue(targetNodeIndex > -1);
#endif

                var targetNode = nodesContainer[targetNodeIndex];
                var extraNode = ChooseLeaf(ref targetNode, RootNodesLevelIndex, in entry);
#if ENABLE_ASSERTS
                Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
                Assert.AreNotEqual(targetNode.EntriesCount, 0);
                Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

                nodesContainer[targetNodeIndex] = targetNode;

                if (extraNode.Aabb == TreeEntryTraits<RTreeNode>.InvalidEntry.Aabb)
                    return;

#if ENABLE_ASSERTS
                Assert.AreNotEqual(extraNode.Aabb, AABB.Empty);
                Assert.AreNotEqual(extraNode.EntriesCount, 0);
                Assert.AreNotEqual(extraNode.EntriesStartIndex, -1);
#endif

                var candidateNodeIndex = targetNodeIndex + 1;
                for (; candidateNodeIndex < startIndex + rootNodesCount; candidateNodeIndex++)
                    if (nodesContainer[candidateNodeIndex].Aabb == TreeEntryTraits<RTreeNode>.InvalidEntry.Aabb)
                        break;

                if (candidateNodeIndex < startIndex + rootNodesCount)
                {
                    nodesContainer[candidateNodeIndex] = extraNode;
                    return;
                }

#if ENABLE_ASSERTS
                Assert.IsTrue(
                    nodesContainer
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
#if ENABLE_PROFILING
                    using var _ = Profiling.RTreeLeafNodesUpdate.Auto();
#endif

                    if (node.EntriesCount == MaxEntries)
                    {
#if ENABLE_ASSERTS
                        Assert.IsFalse(_leafEntriesCounter > _currentThreadResultEntries.Length);
#endif

                        return SplitNode(ref node, ref _currentThreadResultEntries, ref _leafEntriesCounter, in entry,
                            in entry.Aabb, false);
                    }

                    if (node.EntriesStartIndex == -1)
                    {
#if ENABLE_ASSERTS
                        Assert.AreEqual(0, node.EntriesCount);
#endif
                        node.EntriesStartIndex = _leafEntriesCounter;
                        _leafEntriesCounter += MaxEntries;

                        for (var i = 1; i < MaxEntries; i++) // :TODO: try to make it vectorized
                            _currentThreadResultEntries[node.EntriesStartIndex + i] =
                                TreeEntryTraits<RTreeLeafEntry>.InvalidEntry;
                    }

                    _currentThreadResultEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                    node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
                    node.EntriesCount++;

                    return TreeEntryTraits<RTreeNode>.InvalidEntry;
                }

#if ENABLE_PROFILING
                using var __ = Profiling.RTreeNodesUpdate.Auto();
#endif

                var entriesCount = node.EntriesCount;
                var entriesStartIndex = node.EntriesStartIndex;
#if ENABLE_ASSERTS
                Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);
                Assert.IsTrue(entriesStartIndex > -1);

                var nodeLevelsRangeStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
                Assert.IsTrue(entriesStartIndex >= nodeLevelsRangeStartIndex);

                var nodeLevelsRangeCapacity =
 CalculateNodeLevelCapacity(MaxEntries, ReadOnlyData.TreeMaxHeight, nodeLevelIndex - 1);
                var entriesEndIndex = entriesStartIndex + entriesCount;
                Assert.IsTrue(entriesEndIndex < nodeLevelsRangeStartIndex + nodeLevelsRangeCapacity);
                Assert.IsTrue(entriesEndIndex <= nodeLevelsRangeStartIndex + _currentThreadNodesEndIndices[nodeLevelIndex - 1]);
#endif
                ref var nodesContainer = ref SharedWriteData.NodesContainer;

                var childNodeLevelIndex = nodeLevelIndex - 1;
                var childNodeIndex = GetNodeIndexToInsert(in nodesContainer, in node, in entry.Aabb);
#if ENABLE_ASSERTS
                Assert.IsTrue(childNodeIndex >= entriesStartIndex);
                Assert.IsTrue(childNodeIndex < entriesStartIndex + entriesCount);
#endif

                var targetChildNode = nodesContainer[childNodeIndex];
                var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, in entry);

                nodesContainer[childNodeIndex] = targetChildNode;

                if (extraChildNode.Aabb == TreeEntryTraits<RTreeNode>.InvalidEntry.Aabb)
                {
                    node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
#if ENABLE_ASSERTS
                    Assert.IsFalse(targetChildNode.EntriesStartIndex < 0);
                    Assert.IsTrue(targetChildNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
                    return TreeEntryTraits<RTreeNode>.InvalidEntry;
                }

                if (entriesCount == MaxEntries)
                {
                    var childNodesLevelStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
                    var childNodesLevelEndIndex =
                        childNodesLevelStartIndex + _currentThreadNodesEndIndices[childNodeLevelIndex];
#if ENABLE_ASSERTS
                    Assert.IsFalse(_leafEntriesCounter > _currentThreadResultEntries.Length);
#endif

                    var extraNode = SplitNode(ref node, ref nodesContainer, ref childNodesLevelEndIndex, in extraChildNode,
                        in extraChildNode.Aabb, nodeLevelIndex == RootNodesLevelIndex);
                    _currentThreadNodesEndIndices[childNodeLevelIndex] = childNodesLevelEndIndex - childNodesLevelStartIndex;

                    return extraNode;
                }

                nodesContainer[entriesStartIndex + entriesCount] = extraChildNode;

                node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
                node.EntriesCount++;

#if ENABLE_ASSERTS
                Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
                return TreeEntryTraits<RTreeNode>.InvalidEntry;
            }

            private void GrowTree(in RTreeNode newEntry)
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeGrow.Auto();
#endif

                var rootNodesStartIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);
                var rootNodesCount = _currentThreadNodesEndIndices[RootNodesLevelIndex];

#if ENABLE_ASSERTS
                Assert.AreEqual(MaxEntries, rootNodesCount);
                Assert.IsFalse(rootNodesCount + MaxEntries >
                               CalculateNodeLevelCapacity(MaxEntries, ReadOnlyData.TreeMaxHeight, RootNodesLevelIndex));
#endif

                var newRootNodeA = new RTreeNode
                {
                    Aabb = AABB.Empty,
                    EntriesStartIndex = rootNodesStartIndex,
                    EntriesCount = rootNodesCount
                };

                ref var nodesContainer = ref SharedWriteData.NodesContainer;

                var rootNodesLevelEndIndex = rootNodesStartIndex + rootNodesCount;
                var newRootNodeB = SplitNode(ref newRootNodeA, ref nodesContainer, ref rootNodesLevelEndIndex, in newEntry,
                    in newEntry.Aabb, true);

                _currentThreadNodesEndIndices[RootNodesLevelIndex] = rootNodesLevelEndIndex - rootNodesStartIndex;
                ++SharedWriteData.RootNodesLevelIndices[_jobIndex];

                _currentThreadNodesEndIndices[RootNodesLevelIndex] = MaxEntries;

                var newRootNodesStartIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);

                nodesContainer[newRootNodesStartIndex + 0] = newRootNodeA;
                nodesContainer[newRootNodesStartIndex + 1] = newRootNodeB;

                for (var i = 2; i < MaxEntries; i++) // :TODO: try to make it vectorized
                    nodesContainer[newRootNodesStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

#if ENABLE_ASSERTS
                var newRootNodes = nodesContainer.GetSubArray(newRootNodesStartIndex, MaxEntries).ToArray();
                Assert.IsTrue(newRootNodes.Count(n => n.Aabb != AABB.Empty) > 0);
                Assert.IsTrue(newRootNodes.Count(n => n.EntriesStartIndex != -1) > 0);
                Assert.IsTrue(newRootNodes.Count(n => n.EntriesCount > 0) > 0);
#endif
            }

            private static RTreeNode SplitNode<T>(ref RTreeNode splitNode, ref NativeArray<T> entries,
                ref int entriesEndIndex, in T newEntry, in AABB newEntryAabb, bool isRootLevel)
                where T : struct
            {
#if ENABLE_PROFILING
                using var _ = Profiling.RTreeSplitNode.Auto();
#endif

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

                var (indexA, indexB) = FindLargestEntriesPair(in entries, in newEntryAabb, startIndex, endIndex);
#if ENABLE_ASSERTS
                Assert.IsTrue(indexA > -1 && indexB > -1);
#endif

                // NativeSlice<>
                // UnsafeUtility.As<T, AABB>(ref newEntry)
                // UnsafeUtility.AsRef<T, AABB>(ref newEntry)
                // var nodeEntriesPtr = entries.GetUnsafeReadOnlyPtr();
                // var aabb = UnsafeUtility.ReadArrayElementWithStride<AABB>(nodeEntriesPtr, 0, UnsafeUtility.SizeOf<T>());
                // var aabb = entries.ReinterpretLoad<AABB>(0);

                var newNodeEntriesStartIndex = entriesEndIndex;
                entries[newNodeEntriesStartIndex] = indexB != endIndex ? entries[indexB] : newEntry;

                var newNode = new RTreeNode
                {
                    Aabb = indexB != endIndex ? GetAabb(entries, indexB) : newEntryAabb,

                    EntriesStartIndex = entriesEndIndex,
                    EntriesCount = 1
                };

                var invalidEntry = TreeEntryTraits<T>.InvalidEntry;
                entriesEndIndex += MaxEntries;

                for (var i = newNodeEntriesStartIndex + 1; i < entriesEndIndex; i++) // :TODO: try to make it vectorized
                    entries[i] = invalidEntry;

                (entries[startIndex], entries[indexA]) = (entries[indexA], entries[startIndex]);

                splitNode.EntriesCount = 1;
                splitNode.Aabb = GetAabb(in entries, startIndex);

                for (var i = 1; i <= MaxEntries; i++) // :TODO: try to make it vectorized
                {
                    if (startIndex + i == indexB)
                        continue;

                    var isNewEntry = i == MaxEntries;
                    var entry = isNewEntry ? newEntry : entries[startIndex + i];
                    var entityAabb = isNewEntry ? newEntryAabb : GetAabb(entries, startIndex + i);

                    var conjugatedAabbA = fix.AABBs_conjugate(in splitNode.Aabb, in entityAabb);
                    var conjugatedAabbB = fix.AABBs_conjugate(in newNode.Aabb, in entityAabb);

                    var isNewNodeTarget =
                        IsSecondNodeTarget(in splitNode.Aabb, in newNode.Aabb, in conjugatedAabbA, in conjugatedAabbB);
                    ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                    if (isNewNodeTarget || targetNode.EntriesCount != i)
                        entries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                    targetNode.EntriesCount++;
                    targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
                }

                while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                    FillNodes(ref entries, ref splitNode, ref newNode);

                for (var i = splitNode.EntriesCount; i < MaxEntries; i++) // :TODO: try to make it vectorized
                    entries[splitNode.EntriesStartIndex + i] = invalidEntry;

                for (var i = newNode.EntriesCount; i < MaxEntries; i++) // :TODO: try to make it vectorized
                    entries[newNode.EntriesStartIndex + i] = invalidEntry;

#if ENABLE_ASSERTS
                /*var invalidChildNodesCount = 0;
                for (var i = 0; i < splitNode.EntriesCount; i++)
                    invalidChildNodesCount += GetAabb(entries, i) != AABB.Empty ? 1 : 0;
                Assert.IsTrue(isRootLevel || invalidChildNodesCount >= MinEntries);

                invalidChildNodesCount = 0;
                for (var i = 0; i < newNode.EntriesCount; i++)
                    invalidChildNodesCount += GetAabb(entries, i) != AABB.Empty ? 1 : 0;
                Assert.IsTrue(isRootLevel || invalidChildNodesCount >= MinEntries);*/

                Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
                Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

                return newNode;
            }

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
                for (var i = entriesStartIndex; i < entriesEndIndex; i++) // :TODO: try to make it vectorized
                {
                    var entry = nodeEntries[i];
                    if (entry.EntriesCount < MinEntries)
                    {
                        if (i == entriesStartIndex)
                            return i;

#if ENABLE_ASSERTS
                        Assert.AreEqual(AABB.Empty, entry.Aabb);
                        Assert.AreEqual(-1, entry.EntriesStartIndex);
                        Assert.AreEqual(0, entry.EntriesCount);
#endif
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

            private static void FillNodes<T>(ref NativeArray<T> nodeEntries, [NoAlias] ref RTreeNode splitNode,
                [NoAlias] ref RTreeNode newNode)
                where T : struct
            {
                ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
                ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

                var sourceNodeEntriesCount = sourceNode.EntriesCount;
                var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
                var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

                var sourceNodeAabb = AABB.Empty;

                var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Empty, fix.MaxValue);
                for (var i = sourceNodeStartIndex; i < sourceNodeEndIndex; i++) // :TODO: try to make it vectorized
                {
                    var entryAabb = GetAabb(nodeEntries, i);
                    var conjugatedArea = GetConjugatedArea(in targetNode.Aabb, in entryAabb);
                    if (conjugatedArea > minArena)
                    {
                        sourceNodeAabb = fix.AABBs_conjugate(in sourceNodeAabb, in entryAabb);
                        continue;
                    }

                    sourceNodeAabb = fix.AABBs_conjugate(in sourceNodeAabb, in sourceEntryAabb);

                    (sourceEntryIndex, sourceEntryAabb, minArena) = (i, entryAabb, conjugatedArea);
                }

#if ENABLE_ASSERTS
                Assert.IsTrue(sourceEntryIndex > -1);
#endif

                var targetEntryIndex = targetNode.EntriesStartIndex + targetNode.EntriesCount;
                nodeEntries[targetEntryIndex] = nodeEntries[sourceEntryIndex];

                targetNode.Aabb = fix.AABBs_conjugate(in targetNode.Aabb, in sourceEntryAabb);
                targetNode.EntriesCount++;

                if (sourceEntryIndex != sourceNodeEndIndex - 1)
                    nodeEntries[sourceEntryIndex] = nodeEntries[sourceNodeEndIndex - 1];

                sourceNode.Aabb = sourceNodeAabb;
                sourceNode.EntriesCount--;
            }

            private static (int indexA, int indexB) FindLargestEntriesPair<T>(
                in NativeArray<T> entries,
                in AABB newEntryAabb,
                int startIndex,
                int endIndex)
                where T : struct
            {
                var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
                for (var i = startIndex; i < endIndex; i++) // :TODO: try to make it vectorized
                {
                    var aabbA = GetAabb(entries, i);
                    fix conjugatedArea;

                    for (var j = i + 1; j < endIndex; j++)
                    {
                        var aabbB = GetAabb(entries, j);
                        if (aabbB == AABB.Empty)
                            continue;

                        conjugatedArea = GetConjugatedArea(in aabbA, in aabbB);
                        if (conjugatedArea <= maxArena)
                            continue;

                        (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                    }

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

            private int GetNodeLevelStartIndex(int nodeLevelIndex) =>
                CalculateSubTreeNodeLevelStartIndex(MaxEntries, _jobIndex, nodeLevelIndex, ReadOnlyData.TreeMaxHeight,
                    ReadOnlyData.NodesContainerCapacity);

            private static (fix areaIncrease, fix sizeIncrease) GetAreaAndSizeIncrease(in AABB nodeAabb, in AABB conjugatedAabb)
            {
                var conjugatedArea = fix.AABB_area(in conjugatedAabb);
                var areaIncrease = conjugatedArea - fix.AABB_area(in nodeAabb);

                var size = nodeAabb.max - nodeAabb.min;
                var conjugatedSize = conjugatedAabb.max - conjugatedAabb.min;
                var sizeIncrease = fix.max(conjugatedSize.x - size.x, conjugatedSize.y - size.y);

                return (areaIncrease, sizeIncrease);
            }

            private static fix GetConjugatedArea(in AABB aabbA, in AABB aabbB) =>
                fix.AABB_area(fix.AABBs_conjugate(in aabbA, in aabbB));

            private static unsafe AABB GetAabb<T>(in NativeArray<T> entries, int index) where T : struct =>
                UnsafeUtility.ReadArrayElementWithStride<AABB>(entries.GetUnsafeReadOnlyPtr(), index,
                    UnsafeUtility.SizeOf<T>());
        }
    }
}
