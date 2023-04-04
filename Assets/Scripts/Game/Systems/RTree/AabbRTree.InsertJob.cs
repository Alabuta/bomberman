using System.Diagnostics;
using System.Runtime.CompilerServices;
using App;
using Math.FixedPointMath;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

#if ENABLE_RTREE_ASSERTS
using System.Linq;
using UnityEngine.Assertions;
#endif

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

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
        [ReadOnly]
        public int SubTreesCount;
#endif

        [ReadOnly]
        public NativeArray<RTreeLeafEntry>.ReadOnly InputEntries;
    }

    public struct InsertJobSharedWriteData
    {
        public NativeArray<RTreeLeafEntry> ResultEntries;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RTreeNode> NodesContainer;

        public NativeArray<int> NodesEndIndicesContainer;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> RootNodesLevelIndices;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> RootNodesCounts;

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
        [NativeDisableContainerSafetyRestriction]
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<UnsafeAtomicCounter32> CountersContainer;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<int> PerThreadWorkerIndices;
#endif
    }

    public partial class AabbRTree
    {
        public void Build(NativeArray<RTreeLeafEntry> inputEntries)
        {
            using var _ = Profiling.RTreeBuild.Auto();

            SubTreesCount = 0;

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
                _rootNodesLevelIndices[i] = -1;

            if (inputEntries.Length == 0)
                return;

            var entitiesCount = EntriesCap < MinEntries
                ? inputEntries.Length
                : math.min(inputEntries.Length, EntriesCap);

            Assert.IsFalse(entitiesCount < MinEntries);

#if NO_WORK_STEALING_RTREE_INSERT_JOB
            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount + 1, 1);
#else
            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount + 1, EntriesScheduleBatch);
#endif
            _insertJob.ReadOnlyData.InputEntries = inputEntries.GetSubArray(0, entitiesCount).AsReadOnly();
            Assert.IsFalse(entitiesCount > _insertJob.ReadOnlyData.InputEntries.Length);
            Assert.IsFalse(entitiesCount > inputEntries.Length);

            Profiling.RTreeInsertJobComplete.Begin();
#if NO_WORK_STEALING_RTREE_INSERT_JOB
            _insertJob
                .Schedule(_workersCount, 1)
                .Complete();
#else
            _workersInUseCounter.Reset();

            _insertJob
                .ScheduleBatch(entitiesCount, EntriesScheduleBatch)
                .Complete();
#endif
            Profiling.RTreeInsertJobComplete.End();

            foreach (var index in _rootNodesLevelIndices)
            {
                if (index < 0)
                    break;

                ++SubTreesCount;
            }
        }

        private void InitInsertJob(int entriesCount, int workersCount, int batchSize)
        {
            using var _ = Profiling.RTreeInitInsertJob.Auto();

            var treeStateHash = CalculateTreeStateHash(entriesCount, workersCount, batchSize);
            if (treeStateHash == _treeStateHash)
                return;

            _treeStateHash = treeStateHash;

            var maxWorkersCount = (int) math.ceil((double) entriesCount / EntriesPerWorkerMinCount);
            _workersCount = math.min(workersCount, maxWorkersCount);

            var perWorkerEntriesCount = math.ceil((double) entriesCount / (_workersCount * batchSize)) * batchSize;

            _subTreesMaxHeight =
                (int) math.ceil(math.log((perWorkerEntriesCount * (MinEntries - 1) + MinEntries) / (2 * MinEntries - 1)) /
                                math.log(MinEntries));
            _subTreesMaxHeight = math.max(_subTreesMaxHeight - 1, 1);

            _perWorkerResultEntriesContainerCapacity = (int) math.ceil(perWorkerEntriesCount / MinEntries) * MaxEntries;
            var resultEntriesCount = _perWorkerResultEntriesContainerCapacity * _workersCount;

            if (!_resultEntries.IsCreated || _resultEntries.Length < resultEntriesCount)
            {
                if (_resultEntries.IsCreated)
                    _resultEntries.Dispose();

                _resultEntries = new NativeArray<RTreeLeafEntry>(resultEntriesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            _perWorkerNodesContainerCapacity =
                (int) (MaxEntries * (math.pow(MaxEntries, _subTreesMaxHeight) - 1) / (MaxEntries - 1));

            var nodesContainerCapacity = _perWorkerNodesContainerCapacity * _workersCount;
            if (!_nodesContainer.IsCreated || _nodesContainer.Length < nodesContainerCapacity)
            {
                if (_nodesContainer.IsCreated)
                    _nodesContainer.Dispose();

                _nodesContainer = new NativeArray<RTreeNode>(nodesContainerCapacity, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            var nodesEndIndicesContainerCapacity = _subTreesMaxHeight * _workersCount;
            if (!_nodesEndIndicesContainer.IsCreated || _nodesEndIndicesContainer.Length < nodesEndIndicesContainerCapacity)
            {
                if (_nodesEndIndicesContainer.IsCreated)
                    _nodesEndIndicesContainer.Dispose();

                _nodesEndIndicesContainer = new NativeArray<int>(nodesEndIndicesContainerCapacity, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            if (!_rootNodesLevelIndices.IsCreated || _rootNodesLevelIndices.Length < _workersCount)
            {
                if (_rootNodesLevelIndices.IsCreated)
                    _rootNodesLevelIndices.Dispose();

                _rootNodesLevelIndices = new NativeArray<int>(_workersCount, Allocator.Persistent);
            }

            if (!_rootNodesCounts.IsCreated || _rootNodesCounts.Length < _workersCount)
            {
                if (_rootNodesCounts.IsCreated)
                    _rootNodesCounts.Dispose();

                _rootNodesCounts = new NativeArray<int>(_workersCount, Allocator.Persistent);
            }

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
            if (!_countersContainer.IsCreated)
            {
                unsafe
                {
                    _workersInUseCount = 0;
                    fixed ( int* count = &_workersInUseCount ) _workersInUseCounter = new UnsafeAtomicCounter32(count);
                }

                _countersContainer = new NativeArray<UnsafeAtomicCounter32>(1, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
                _countersContainer[0] = _workersInUseCounter;
            }

            if (!_perThreadWorkerIndices.IsCreated || _perThreadWorkerIndices.Length < JobsUtility.MaxJobThreadCount)
            {
                var perThreadWorkerIndices = Enumerable
                    .Repeat(-1, JobsUtility.MaxJobThreadCount)
                    .ToArray();

                _perThreadWorkerIndices = new NativeArray<int>(perThreadWorkerIndices, Allocator.Persistent);
            }
#endif

            _insertJob = new InsertJob
            {
                ReadOnlyData = new InsertJobReadOnlyData
                {
                    TreeMaxHeight = _subTreesMaxHeight,
                    PerWorkerEntriesCount = (int) perWorkerEntriesCount,
                    NodesContainerCapacity = _perWorkerNodesContainerCapacity,
                    ResultEntriesContainerCapacity = _perWorkerResultEntriesContainerCapacity,
#if !NO_WORK_STEALING_RTREE_INSERT_JOB
                    SubTreesCount = _workersCount,
#endif
                    EntriesTotalCount = entriesCount
                },
                SharedWriteData = new InsertJobSharedWriteData
                {
                    ResultEntries = _resultEntries,
                    NodesContainer = _nodesContainer,
                    NodesEndIndicesContainer = _nodesEndIndicesContainer,
                    RootNodesLevelIndices = _rootNodesLevelIndices,
                    RootNodesCounts = _rootNodesCounts,
#if !NO_WORK_STEALING_RTREE_INSERT_JOB
                    CountersContainer = _countersContainer,
                    PerThreadWorkerIndices = _perThreadWorkerIndices
#endif
                }
            };
        }

#if USE_BURST_FOR_RTREE_JOBS
        [BurstCompile(CompileSynchronously = true, OptimizeFor = OptimizeFor.Performance, DisableSafetyChecks = true)]
#endif
        internal struct InsertJob :
#if NO_WORK_STEALING_RTREE_INSERT_JOB
            IJobParallelFor
#else
            IWorkStealingJob
#endif
        {
            [ReadOnly]
            public InsertJobReadOnlyData ReadOnlyData;

            public InsertJobSharedWriteData SharedWriteData;

            [NativeDisableContainerSafetyRestriction]
            private NativeArray<int> _currentWorkerNodesEndIndices;

            [NativeDisableContainerSafetyRestriction]
            private NativeArray<RTreeLeafEntry> _currentThreadResultEntries;

            private int _subTreeIndex;
            private int _leafEntriesCounter;

            private int RootNodesLevelIndex => SharedWriteData.RootNodesLevelIndices[_subTreeIndex];

#if NO_WORK_STEALING_RTREE_INSERT_JOB
            public void Execute(int subTreeIndex)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeInsertJob.Auto();
#endif

                _subTreeIndex = subTreeIndex;
                _leafEntriesCounter = 0;

                SharedWriteData.RootNodesLevelIndices[subTreeIndex] = 0;
                SharedWriteData.RootNodesCounts[_subTreeIndex] = 0;

                var treeMaxHeight = ReadOnlyData.TreeMaxHeight;
                var resultEntriesContainerCapacity = ReadOnlyData.ResultEntriesContainerCapacity;

                _currentWorkerNodesEndIndices = SharedWriteData.NodesEndIndicesContainer
                    .GetSubArray(subTreeIndex * treeMaxHeight, treeMaxHeight);

                _currentWorkerNodesEndIndices[RootNodesLevelIndex] = MaxEntries;
                for (var i = 1; i < _currentWorkerNodesEndIndices.Length; i++)
                    _currentWorkerNodesEndIndices[i] = 0;

                _currentThreadResultEntries = SharedWriteData.ResultEntries
                    .GetSubArray(subTreeIndex * resultEntriesContainerCapacity, resultEntriesContainerCapacity);

                var nodesContainerStartIndex = subTreeIndex * ReadOnlyData.NodesContainerCapacity;
                for (var i = 0; i < MaxEntries; i++)
                    SharedWriteData.NodesContainer[nodesContainerStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

                var entriesStartIndex = subTreeIndex * ReadOnlyData.PerWorkerEntriesCount;
                var entriesEndIndex = entriesStartIndex + ReadOnlyData.PerWorkerEntriesCount;
                entriesEndIndex = math.min(entriesEndIndex, ReadOnlyData.EntriesTotalCount);

#if ENABLE_RTREE_ASSERTS
                Assert.IsFalse(entriesEndIndex - entriesStartIndex > ReadOnlyData.PerWorkerEntriesCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.EntriesTotalCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.InputEntries.Length);
#endif

                for (var entryIndex = entriesStartIndex; entryIndex < entriesEndIndex; entryIndex++)
                    Insert(entryIndex);
            }
#else
            public void Execute(ref PerWorkerData perWorkerData, int entriesStartIndex, int count)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeInsertJob.Auto();
#endif

                _currentWorkerNodesEndIndices = perWorkerData.CurrentThreadNodesEndIndices;
                _currentThreadResultEntries = perWorkerData.CurrentThreadResultEntries;
                _subTreeIndex = perWorkerData.WorkerIndex;

                var entriesEndIndex = entriesStartIndex + count;
                entriesEndIndex = math.min(entriesEndIndex, ReadOnlyData.EntriesTotalCount);

#if ENABLE_RTREE_ASSERTS
                Assert.IsFalse(entriesEndIndex - entriesStartIndex > ReadOnlyData.PerWorkerEntriesCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.EntriesTotalCount);
                Assert.IsFalse(entriesEndIndex > ReadOnlyData.InputEntries.Length);
#endif

                for (var entryIndex = entriesStartIndex; entryIndex < entriesEndIndex; entryIndex++)
                    Insert(entryIndex);
            }
#endif

            private unsafe void Insert(int entryIndex)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeInsertEntry.Auto();
#endif

#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(RootNodesLevelIndex > -1);
#endif
                var startIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);
                var rootNodesCount = _currentWorkerNodesEndIndices[RootNodesLevelIndex];
#if ENABLE_RTREE_ASSERTS
                Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

                var entry = ReadOnlyData.InputEntries[entryIndex];

                ref var nodesContainer = ref SharedWriteData.NodesContainer;
                var targetNodeIndex =
                    GetNodeIndexToInsert(in nodesContainer, startIndex, startIndex + MaxEntries, in entry.Aabb);
#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(targetNodeIndex > -1);
#endif

                ref var targetNode =
                    ref UnsafeUtility.ArrayElementAsRef<RTreeNode>(nodesContainer.GetUnsafePtr(), targetNodeIndex);
                var extraNode = ChooseLeaf(ref targetNode, RootNodesLevelIndex, in entry);
#if ENABLE_RTREE_ASSERTS
                Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
                Assert.AreNotEqual(targetNode.EntriesCount, 0);
                Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

                if (extraNode.Aabb == TreeEntryTraits<RTreeNode>.InvalidEntry.Aabb)
                {
                    if (SharedWriteData.RootNodesCounts[_subTreeIndex] < 1)
                        SharedWriteData.RootNodesCounts[_subTreeIndex] = 1;

                    return;
                }

#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(SharedWriteData.RootNodesCounts[_subTreeIndex] > 0);
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
                    ++SharedWriteData.RootNodesCounts[_subTreeIndex];
                    return;
                }

#if ENABLE_RTREE_ASSERTS
                CheckInsert(nodesContainer, startIndex, rootNodesCount, targetNodeIndex);
#endif

                GrowTree(in extraNode);
            }

#if ENABLE_RTREE_ASSERTS
            [BurstDiscard]
            [Conditional("ENABLE_RTREE_ASSERTS")]
            private static void CheckInsert(NativeArray<RTreeNode> nodesContainer, int startIndex, int rootNodesCount,
                int targetNodeIndex)
            {
                Assert.IsTrue(
                    nodesContainer
                        .GetSubArray(startIndex, math.max(rootNodesCount, targetNodeIndex - startIndex + 1))
                        .All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
            }
#endif

            private unsafe RTreeNode ChooseLeaf(ref RTreeNode node, int nodeLevelIndex, in RTreeLeafEntry entry)
            {
                var isLeafLevel = nodeLevelIndex == 0;
                if (isLeafLevel)
                {
#if ENABLE_RTREE_PROFILING
                    using var _ = Profiling.RTreeLeafNodesUpdate.Auto();
#endif

                    if (node.EntriesCount == MaxEntries)
                    {
#if ENABLE_RTREE_ASSERTS
                        Assert.IsFalse(_leafEntriesCounter > _currentThreadResultEntries.Length);
#endif
                        return SplitNode(
                            ref node,
                            ref _currentThreadResultEntries,
                            ref _leafEntriesCounter,
                            in entry,
                            in entry.Aabb,
                            false);
                    }

                    if (node.EntriesStartIndex == -1)
                    {
#if ENABLE_RTREE_ASSERTS
                        Assert.AreEqual(0, node.EntriesCount);
#endif
                        node.EntriesStartIndex = _leafEntriesCounter;
                        _leafEntriesCounter += MaxEntries;

                        for (var i = 1; i < MaxEntries; i++)
                            _currentThreadResultEntries[node.EntriesStartIndex + i] =
                                TreeEntryTraits<RTreeLeafEntry>.InvalidEntry;
                    }

                    _currentThreadResultEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                    node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
                    node.EntriesCount++;

                    return TreeEntryTraits<RTreeNode>.InvalidEntry;
                }

#if ENABLE_RTREE_PROFILING
                using var __ = Profiling.RTreeNodesUpdate.Auto();
#endif

                var entriesCount = node.EntriesCount;
                var entriesStartIndex = node.EntriesStartIndex;
#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);
                Assert.IsTrue(entriesStartIndex > -1);

                var nodeLevelsRangeStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
                Assert.IsTrue(entriesStartIndex >= nodeLevelsRangeStartIndex);

                var nodeLevelsRangeCapacity =
                    CalculateNodeLevelCapacity(MaxEntries, ReadOnlyData.TreeMaxHeight, nodeLevelIndex - 1);
                var entriesEndIndex = entriesStartIndex + entriesCount;
                Assert.IsTrue(entriesEndIndex < nodeLevelsRangeStartIndex + nodeLevelsRangeCapacity);
                Assert.IsTrue(entriesEndIndex <= nodeLevelsRangeStartIndex + _currentWorkerNodesEndIndices[nodeLevelIndex - 1]);
#endif
                ref var nodesContainer = ref SharedWriteData.NodesContainer;

                var childNodeLevelIndex = nodeLevelIndex - 1;
                var childNodeIndex = GetNodeIndexToInsert(in nodesContainer, in node, in entry.Aabb);
#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(childNodeIndex >= entriesStartIndex);
                Assert.IsTrue(childNodeIndex < entriesStartIndex + entriesCount);
#endif

                ref var targetChildNode =
                    ref UnsafeUtility.ArrayElementAsRef<RTreeNode>(nodesContainer.GetUnsafePtr(), childNodeIndex);
                var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, in entry);

                if (extraChildNode.Aabb == TreeEntryTraits<RTreeNode>.InvalidEntry.Aabb)
                {
                    node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
#if ENABLE_RTREE_ASSERTS
                    Assert.IsFalse(targetChildNode.EntriesStartIndex < 0);
                    Assert.IsTrue(targetChildNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
                    return TreeEntryTraits<RTreeNode>.InvalidEntry;
                }

                if (entriesCount == MaxEntries)
                {
                    var childNodesLevelStartIndex = GetNodeLevelStartIndex(nodeLevelIndex - 1);
                    var childNodesLevelEndIndex =
                        childNodesLevelStartIndex + _currentWorkerNodesEndIndices[childNodeLevelIndex];
#if ENABLE_RTREE_ASSERTS
                    Assert.IsFalse(_leafEntriesCounter > _currentThreadResultEntries.Length);
#endif

                    var extraNode = SplitNode(
                        ref node,
                        ref nodesContainer,
                        ref childNodesLevelEndIndex,
                        in extraChildNode,
                        in extraChildNode.Aabb,
                        nodeLevelIndex == RootNodesLevelIndex);

                    _currentWorkerNodesEndIndices[childNodeLevelIndex] = childNodesLevelEndIndex - childNodesLevelStartIndex;

                    return extraNode;
                }

                nodesContainer[entriesStartIndex + entriesCount] = extraChildNode;

                node.Aabb = fix.AABBs_conjugate(in node.Aabb, in entry.Aabb);
                node.EntriesCount++;

#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
                return TreeEntryTraits<RTreeNode>.InvalidEntry;
            }

            private void GrowTree(in RTreeNode newEntry)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeGrow.Auto();
#endif

                var rootNodesStartIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);
                var rootNodesCount = _currentWorkerNodesEndIndices[RootNodesLevelIndex];

#if ENABLE_RTREE_ASSERTS
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
                var newRootNodeB = SplitNode(
                    ref newRootNodeA,
                    ref nodesContainer,
                    ref rootNodesLevelEndIndex,
                    in newEntry,
                    in newEntry.Aabb,
                    true);

                _currentWorkerNodesEndIndices[RootNodesLevelIndex] = rootNodesLevelEndIndex - rootNodesStartIndex;
                ++SharedWriteData.RootNodesLevelIndices[_subTreeIndex];
                SharedWriteData.RootNodesCounts[_subTreeIndex] = 2;

                _currentWorkerNodesEndIndices[RootNodesLevelIndex] = MaxEntries;

                var newRootNodesStartIndex = GetNodeLevelStartIndex(RootNodesLevelIndex);

                nodesContainer[newRootNodesStartIndex + 0] = newRootNodeA;
                nodesContainer[newRootNodesStartIndex + 1] = newRootNodeB;

                for (var i = 2; i < MaxEntries; i++)
                    nodesContainer[newRootNodesStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

#if ENABLE_RTREE_ASSERTS
                CheckGrowTree(nodesContainer, newRootNodesStartIndex);
#endif
            }

#if ENABLE_RTREE_ASSERTS
            [BurstDiscard]
            private static void CheckGrowTree(NativeArray<RTreeNode> nodesContainer, int newRootNodesStartIndex)
            {
                var newRootNodes = nodesContainer.GetSubArray(newRootNodesStartIndex, MaxEntries).ToArray();
                Assert.IsTrue(newRootNodes.Count(n => n.Aabb != AABB.Empty) > 0);
                Assert.IsTrue(newRootNodes.Count(n => n.EntriesStartIndex != -1) > 0);
                Assert.IsTrue(newRootNodes.Count(n => n.EntriesCount > 0) > 0);
            }
#endif

            private static RTreeNode SplitNode<T>(
                ref RTreeNode splitNode,
                ref NativeArray<T> entries,
                ref int entriesEndIndex,
                in T newEntry,
                in AABB newEntryAabb,
                bool isRootLevel
            )
                where T : struct
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeSplitNode.Auto();
#endif

                var entriesCount = splitNode.EntriesCount;
                var startIndex = splitNode.EntriesStartIndex;
                var endIndex = startIndex + entriesCount;

#if ENABLE_RTREE_ASSERTS
                Assert.AreEqual(MaxEntries, entriesCount);
#endif

                // Quadratic cost split
                // Search for pairs of entries A and B that would cause the largest area if placed in the same node
                // Put A and B entries in two different nodes
                // Then consider all other entries area increase relatively to two previous nodes' AABBs
                // Assign entry to the node with smaller AABB area increase
                // Repeat until all entries are assigned between two new nodes

                var (indexA, indexB) = FindLargestEntriesPair(in entries, in newEntryAabb, startIndex, endIndex);
#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(indexA > -1 && indexB > -1);
#endif
                var newNodeEntriesStartIndex = entriesEndIndex;
                entries[newNodeEntriesStartIndex] = indexB != endIndex ? entries[indexB] : newEntry;

                var newNode = new RTreeNode
                {
                    Aabb = indexB != endIndex ? GetAabbUnsafe(in entries, indexB) : newEntryAabb,

                    EntriesStartIndex = entriesEndIndex,
                    EntriesCount = 1
                };

                var invalidEntry = TreeEntryTraits<T>.InvalidEntry;
                entriesEndIndex += MaxEntries;

                for (var i = newNodeEntriesStartIndex + 1; i < entriesEndIndex; i++)
                    entries[i] = invalidEntry;

                (entries[startIndex], entries[indexA]) = (entries[indexA], entries[startIndex]);

                splitNode.EntriesCount = 1;
                splitNode.Aabb = GetAabbUnsafe(in entries, startIndex);

                for (var i = 1; i <= MaxEntries; i++)
                {
                    if (startIndex + i == indexB)
                        continue;

                    var isNewEntry = i == MaxEntries;
                    var entry = isNewEntry ? newEntry : entries[startIndex + i];
                    var entityAabb = isNewEntry ? newEntryAabb : GetAabbUnsafe(in entries, startIndex + i);

                    var conjugatedAabbA = fix.AABBs_conjugate(in entityAabb, in splitNode.Aabb);
                    var conjugatedAabbB = fix.AABBs_conjugate(in entityAabb, in newNode.Aabb);

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

                for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                    entries[splitNode.EntriesStartIndex + i] = invalidEntry;

                for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                    entries[newNode.EntriesStartIndex + i] = invalidEntry;

#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
                Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

                return newNode;
            }

            private static int GetNodeIndexToInsert(
                in NativeArray<RTreeNode> nodeEntries,
                in RTreeNode parentNode,
                in AABB newEntryAabb)
            {
                var entriesStartIndex = parentNode.EntriesStartIndex;
                var entriesEndIndex = entriesStartIndex + parentNode.EntriesCount;

                return GetNodeIndexToInsert(in nodeEntries, entriesStartIndex, entriesEndIndex, in newEntryAabb);
            }

            private static int GetNodeIndexToInsert(
                in NativeArray<RTreeNode> nodeEntries,
                int entriesStartIndex,
                int entriesEndIndex,
                in AABB newEntryAabb)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeGetNodeIndexToInsert.Auto();
#endif
                var (nodeIndex, minArea) = (-1, fix.MaxValue);
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    var entry = nodeEntries[i];
                    if (entry.EntriesCount < MinEntries)
                    {
                        if (i == entriesStartIndex)
                            return i;

#if ENABLE_RTREE_ASSERTS
                        Assert.AreEqual(AABB.Empty, entry.Aabb);
                        Assert.AreEqual(-1, entry.EntriesStartIndex);
                        Assert.AreEqual(0, entry.EntriesCount);
#endif
                        break;
                    }

#if ENABLE_RTREE_ASSERTS
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

            private static void FillNodes<T>(
                ref NativeArray<T> nodeEntries,
                [NoAlias] ref RTreeNode splitNode,
                [NoAlias] ref RTreeNode newNode)
                where T : struct
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeFillNodes.Auto();
#endif
                ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
                ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

                var sourceNodeEntriesCount = sourceNode.EntriesCount;
                var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
                var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

                var sourceNodeAabb = AABB.Empty;

                var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Empty, fix.MaxValue);
                for (var i = sourceNodeStartIndex; i < sourceNodeEndIndex; i++)
                {
                    var entryAabb = GetAabbUnsafe(in nodeEntries, i);
                    var conjugatedArea = GetConjugatedArea(in targetNode.Aabb, in entryAabb);
                    if (conjugatedArea > minArena)
                    {
                        sourceNodeAabb = fix.AABBs_conjugate(in sourceNodeAabb, in entryAabb);
                        continue;
                    }

                    sourceNodeAabb = fix.AABBs_conjugate(in sourceNodeAabb, in sourceEntryAabb);

                    (sourceEntryIndex, sourceEntryAabb, minArena) = (i, entryAabb, conjugatedArea);
                }

#if ENABLE_RTREE_ASSERTS
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
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeFindLargestPair.Auto();
#endif
                var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
                for (var i = startIndex; i < endIndex; i++)
                {
                    var aabbA = GetAabbUnsafe(in entries, i);
                    fix conjugatedArea;

                    for (var j = i + 1; j < endIndex; j++)
                    {
                        var aabbB = GetAabbUnsafe(in entries, j);
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

            private static bool IsSecondNodeTarget(
                in AABB nodeAabbA,
                in AABB nodeAabbB,
                in AABB conjugatedAabbA,
                in AABB conjugatedAabbB)
            {
#if ENABLE_RTREE_PROFILING
                using var _ = Profiling.RTreeIsSecondNodeTarget.Auto();
#endif
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetNodeLevelStartIndex(int nodeLevelIndex) =>
                CalculateSubTreeNodeLevelStartIndex(MaxEntries, _subTreeIndex, nodeLevelIndex, ReadOnlyData.TreeMaxHeight,
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

            private static unsafe AABB GetAabbUnsafe<T>(in NativeArray<T> entries, int index) where T : struct =>
                UnsafeUtility.ReadArrayElementWithStride<AABB>(entries.GetUnsafeReadOnlyPtr(), index,
                    UnsafeUtility.SizeOf<T>());
        }
    }
}
