using System;
using System.Collections.Generic;
using System.Linq;
using App;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
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

        public void Update()
        {
            using var _ = Profiling.RTreeUpdate.Auto();

            throw new NotImplementedException();
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
