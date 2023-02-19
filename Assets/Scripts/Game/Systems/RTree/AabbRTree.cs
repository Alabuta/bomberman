using System;
using System.Collections.Generic;
using System.Linq;
using App;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Systems.RTree
{
    internal static class TreeEntryTraits<T> where T : struct
    {
        public static readonly T InvalidEntry;

        static TreeEntryTraits()
        {
            TreeEntryTraits<RTreeNode>.InvalidEntry = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            TreeEntryTraits<RTreeLeafEntry>.InvalidEntry = new RTreeLeafEntry(AABB.Empty, 0);
        }
    }

    public readonly struct RTreeLeafEntry
    {
        public readonly AABB Aabb;
        public readonly int Index;

        public RTreeLeafEntry(AABB aabb, int index)
        {
            Aabb = aabb;
            Index = index;
        }
    }

    public struct RTreeNode
    {
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

    public partial class AabbRTree : IRTree
    {
        internal const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int EntriesPerWorkerMinCount = MaxEntries * MinEntries;

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
        private const int EntriesScheduleBatch = 4;
#endif

        private const int InputEntriesStartCount = 256;

        private long _treeStateHash;

        private int _workersCount;
        private int _subTreesMaxHeight;

        private NativeArray<RTreeLeafEntry> _resultEntries;

        private NativeArray<RTreeNode> _nodesContainer;
        private NativeArray<int> _nodesEndIndicesContainer;
        private NativeArray<int> _rootNodesLevelIndices;
        private NativeArray<int> _rootNodesCounts;

        private int _perWorkerResultEntriesContainerCapacity;
        private int _perWorkerNodesContainerCapacity;

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
        private int _workersInUseCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _workersInUseCounter;

        private NativeArray<UnsafeAtomicCounter32> _countersContainer;

        private NativeArray<int> _perThreadWorkerIndices;
#endif

        [NativeDisableUnsafePtrRestriction]
        private InsertJob _insertJob;

        public int EntriesCap { get; set; } = -1;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _rootNodesLevelIndices[subTreeIndex] + 1;

        public AabbRTree()
        {
            Assert.IsFalse(MaxEntries < 4);
            Assert.AreEqual(MaxEntries / 2, MinEntries);
            Assert.AreEqual(MinEntries * 2, MaxEntries);

#if NO_WORK_STEALING_RTREE_INSERT_JOB
            InitInsertJob(InputEntriesStartCount, JobsUtility.JobWorkerCount, 1);
#else
            InitInsertJob(InputEntriesStartCount, JobsUtility.JobWorkerCount + 1, EntriesScheduleBatch);
#endif
        }

        public void Dispose()
        {
            _resultEntries.Dispose();

            _nodesContainer.Dispose();

            _nodesEndIndicesContainer.Dispose();
            _rootNodesLevelIndices.Dispose();
            _rootNodesCounts.Dispose();

#if !NO_WORK_STEALING_RTREE_INSERT_JOB
            _workersInUseCounter.Reset();
            _countersContainer.Dispose();
            _perThreadWorkerIndices.Dispose();
#endif
        }

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

        public IReadOnlyList<RTreeNode> GetSubTreeRootNodes(int subTreeIndex) =>
            GetSubTreeRootNodesImpl(subTreeIndex).ToArray();

        public IEnumerable<RTreeNode> GetNodes(int subTreeIndex, int levelIndex, IEnumerable<int> indices) =>
            levelIndex < GetSubTreeHeight(subTreeIndex)
                ? indices.Select(i => _nodesContainer[i])
                : Enumerable.Empty<RTreeNode>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(int subTreeIndex, IEnumerable<int> indices)
        {
            var subTreeStartIndexOffset = subTreeIndex * _perWorkerResultEntriesContainerCapacity;
            return _resultEntries.Length != 0
                ? indices.Select(i => _resultEntries[subTreeStartIndexOffset + i])
                : Enumerable.Empty<RTreeLeafEntry>();
        }

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result)
        {
            for (var subTreeIndex = 0; subTreeIndex < SubTreesCount; subTreeIndex++)
            {
                var subTreeHeight = GetSubTreeHeight(subTreeIndex);
                if (subTreeHeight < 1)
                    continue;

                var subTreeNodeLevelStartIndex = CalculateSubTreeNodeLevelStartIndex(
                    MaxEntries,
                    subTreeIndex,
                    subTreeHeight - 1,
                    _subTreesMaxHeight,
                    _perWorkerNodesContainerCapacity);

                var rootNodesEndIndex = subTreeNodeLevelStartIndex + _rootNodesCounts[subTreeIndex];
                for (var nodeIndex = subTreeNodeLevelStartIndex; nodeIndex < rootNodesEndIndex; nodeIndex++)
                    QueryNodesByLine(p0, p1, result, subTreeIndex, _rootNodesLevelIndices[subTreeIndex], nodeIndex);
            }
        }

        private void QueryNodesByLine(fix2 p0, fix2 p1,
            ICollection<RTreeLeafEntry> result,
            int subTreeIndex,
            int levelIndex, int nodeIndex)
        {
            var node = _nodesContainer[nodeIndex];
#if ENABLE_RTREE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, node.Aabb);
#endif
            if (!node.Aabb.CohenSutherlandLineClip(ref p0, ref p1))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex == 0)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    var leafEntry = _resultEntries[subTreeIndex * _perWorkerResultEntriesContainerCapacity + i];
                    if (!IntersectedByLine(p0, p1, in leafEntry.Aabb))
                        continue;

                    result.Add(leafEntry);
                }

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByLine(p0, p1, result, subTreeIndex, levelIndex - 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, in AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result)
        {
            for (var subTreeIndex = 0; subTreeIndex < SubTreesCount; subTreeIndex++)
            {
                var subTreeHeight = GetSubTreeHeight(subTreeIndex);
                if (subTreeHeight < 1)
                    continue;

                var subTreeNodeLevelStartIndex = CalculateSubTreeNodeLevelStartIndex(
                    MaxEntries,
                    subTreeIndex,
                    subTreeHeight - 1,
                    _subTreesMaxHeight,
                    _perWorkerNodesContainerCapacity);

                var rootNodesEndIndex = subTreeNodeLevelStartIndex + _rootNodesCounts[subTreeIndex];
                for (var nodeIndex = subTreeNodeLevelStartIndex; nodeIndex < rootNodesEndIndex; nodeIndex++)
                    QueryNodesByAabb(aabb, result, subTreeIndex, _rootNodesLevelIndices[subTreeIndex], nodeIndex);
            }
        }

        private void QueryNodesByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result, int subTreeIndex, int levelIndex,
            int nodeIndex)
        {
            var node = _nodesContainer[nodeIndex];
#if ENABLE_RTREE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, node.Aabb);
#endif
            if (!fix.is_AABB_overlapped_by_AABB(in aabb, in node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex == 0)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                    result.Add(_resultEntries[subTreeIndex * _perWorkerResultEntriesContainerCapacity + i]);

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByAabb(aabb, result, subTreeIndex, levelIndex - 1, i);
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
#if NO_WORK_STEALING_RTREE_INSERT_JOB
                    SubTreesCount = _workersCount,
#endif
                    EntriesTotalCount = entriesCount
                },
                SharedWriteData = new InsertJobSharedWriteData
                {
#if !NO_WORK_STEALING_RTREE_INSERT_JOB
                    CountersContainer = _countersContainer,
                    PerThreadWorkerIndices = _perThreadWorkerIndices,
#endif
                    ResultEntries = _resultEntries,
                    NodesContainer = _nodesContainer,
                    NodesEndIndicesContainer = _nodesEndIndicesContainer,
                    RootNodesLevelIndices = _rootNodesLevelIndices,
                    RootNodesCounts = _rootNodesCounts
                }
            };
        }

        private NativeArray<RTreeNode>.ReadOnly GetSubTreeRootNodesImpl(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return new NativeArray<RTreeNode>(Array.Empty<RTreeNode>(), Allocator.TempJob).AsReadOnly();

            var offset = CalculateSubTreeNodeLevelStartIndex(MaxEntries, subTreeIndex, subTreeHeight - 1, _subTreesMaxHeight,
                _perWorkerNodesContainerCapacity);

            return _nodesContainer.GetSubArray(offset, _rootNodesCounts[subTreeIndex]).AsReadOnly();
        }

        private static int CalculateSubTreeNodeLevelStartIndex(int maxEntries, int subTreeIndex, int nodeLevelIndex,
            int treeMaxHeight, int perWorkerNodesContainerCapacity)
        {
            var index = math.pow(maxEntries, treeMaxHeight + 1) / (maxEntries - 1);
            index *= 1f - 1f / math.pow(maxEntries, nodeLevelIndex);
            index += subTreeIndex * perWorkerNodesContainerCapacity;

            return (int) index;
        }

        private static int CalculateNodeLevelCapacity(int maxEntries, int treeMaxHeight, int nodeLevelIndex) =>
            (int) math.pow(maxEntries, treeMaxHeight - nodeLevelIndex);

        private static long CalculateTreeStateHash(int entriesCount, int workersCount, int batchSize) =>
            (entriesCount, workersCount, batchSize).GetHashCode();
    }
}
