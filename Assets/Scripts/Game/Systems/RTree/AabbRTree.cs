using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using App;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

        private const int EntriesScheduleBatch = 4;

        private const int InputEntriesStartCount = 256;

        private long _treeStateHash;

        private int _workersCount;
        private int _subTreesMaxHeight;

        private NativeArray<RTreeLeafEntry> _inputEntries;
        private NativeArray<RTreeLeafEntry> _resultEntries;

        private NativeArray<RTreeNode> _nodesContainer;
        private NativeArray<int> _nodesEndIndicesContainer;
        private NativeArray<int> _rootNodesLevelIndices;

        private int _perWorkerResultEntriesContainerCapacity;
        private int _perWorkerNodesContainerCapacity;

        private int _workersInUseCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _workersInUseCounter;

        private NativeArray<UnsafeAtomicCounter32> _countersContainer;

        private NativeArray<int> _perThreadWorkerIndices;

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

            InitInsertJob(InputEntriesStartCount, JobsUtility.JobWorkerCount, EntriesScheduleBatch);
        }

        public void Dispose()
        {
            _inputEntries.Dispose();
            _resultEntries.Dispose();

            _nodesContainer.Dispose();

            _nodesEndIndicesContainer.Dispose();
            _rootNodesLevelIndices.Dispose();

            _countersContainer.Dispose();

            _perThreadWorkerIndices.Dispose();
        }

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter)
        {
            using var _ = Profiling.RTreeBuild.Auto();

            SubTreesCount = 0;

            if (filter.IsEmpty())
                return;

            var entitiesCount = EntriesCap < MinEntries
                ? filter.GetEntitiesCount()
                : math.min(filter.GetEntitiesCount(), EntriesCap);

            Assert.IsFalse(entitiesCount < MinEntries);

#if !NO_WORK_STEALING_JOBS
            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount + 1, EntriesScheduleBatch);
#else
            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount, 1);
#endif
            Assert.IsFalse(entitiesCount > _inputEntries.Length);

            Profiling.RTreeNativeArrayFill.Begin();
            foreach (var index in filter)
            {
                if (index >= entitiesCount)
                    break;

                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _inputEntries[index] = new RTreeLeafEntry(aabb, index);
            }

            Profiling.RTreeNativeArrayFill.End();

            _countersContainer[0].Reset();

            Profiling.RTreeInsertJobComplete.Begin();
#if !NO_WORK_STEALING_JOBS
            _insertJob
                .ScheduleBatch(entitiesCount, EntriesScheduleBatch)
                .Complete();
#else
            _insertJob
                .Schedule(_workersCount, 1)
                .Complete();
#endif
            Profiling.RTreeInsertJobComplete.End();

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
            {
                if (_rootNodesLevelIndices[i] < 0)
                    break;

                ++SubTreesCount;
            }
        }

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return Enumerable.Empty<RTreeNode>();

            var offset = CalculateSubTreeNodeLevelStartIndex(MaxEntries, subTreeIndex, subTreeHeight - 1, _subTreesMaxHeight,
                _perWorkerNodesContainerCapacity);

            return _nodesContainer
                .GetSubArray(offset, MaxEntries)
                .TakeWhile(n => n.Aabb != AABB.Empty);
        }

        public IEnumerable<RTreeNode> GetNodes(int subTreeIndex, int levelIndex, IEnumerable<int> indices) =>
            levelIndex < GetSubTreeHeight(subTreeIndex)
                ? indices.Select(i => _nodesContainer[i])
                : Enumerable.Empty<RTreeNode>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(int subTreeIndex, IEnumerable<int> indices) =>
            _resultEntries.Length != 0
                ? indices.Select(i => _resultEntries[subTreeIndex * _perWorkerResultEntriesContainerCapacity + i])
                : Enumerable.Empty<RTreeLeafEntry>();

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result)
        {
        }

        public void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result)
        {
        }

        private void InitInsertJob(int entriesCount, int workersCount, int batchSize)
        {
            using var _ = Profiling.RTreeInitInsertJob.Auto();

            var treeStateHash = CalculateTreeStateHash(entriesCount, workersCount, batchSize);
            if (treeStateHash == _treeStateHash)
                return;

            _treeStateHash = treeStateHash;

            if (!_inputEntries.IsCreated || _inputEntries.Length < entriesCount)
            {
                if (_inputEntries.IsCreated)
                    _inputEntries.Dispose();

                _inputEntries = new NativeArray<RTreeLeafEntry>(entriesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            var maxWorkersCount = (int) math.ceil((double) entriesCount / EntriesPerWorkerMinCount);
            _workersCount = math.min(workersCount, maxWorkersCount);

            var perWorkerEntriesCount = math.ceil((double) entriesCount / (_workersCount * batchSize)) * batchSize * 16;

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

                _rootNodesLevelIndices = new NativeArray<int>(_workersCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

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

            _insertJob = new InsertJob
            {
                ReadOnlyData = new InsertJobReadOnlyData
                {
                    TreeMaxHeight = _subTreesMaxHeight,
                    PerWorkerEntriesCount = (int) perWorkerEntriesCount,
                    NodesContainerCapacity = _perWorkerNodesContainerCapacity,
                    ResultEntriesContainerCapacity = _perWorkerResultEntriesContainerCapacity,
                    EntriesTotalCount = entriesCount,
                    InputEntries = _inputEntries.AsReadOnly()
                },
                SharedWriteData = new InsertJobSharedWriteData
                {
                    CountersContainer = _countersContainer,
                    PerThreadWorkerIndices = _perThreadWorkerIndices,

                    ResultEntries = _resultEntries,
                    NodesContainer = _nodesContainer,
                    NodesEndIndicesContainer = _nodesEndIndicesContainer,
                    RootNodesLevelIndices = _rootNodesLevelIndices
                }
            };
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
