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
using UnityEngine.Assertions;

namespace Game.Systems.RTree
{
    internal static class TreeEntryTraits<T> where T : struct
    {
        public static T InvalidEntry;
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
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int EntriesPerWorkerMinCount = MaxEntries * MinEntries;

        private const int InputEntriesStartCount = 128;

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

        [NativeDisableUnsafePtrRestriction]
        private InsertJob _insertJob;

        [NativeDisableUnsafePtrRestriction]
        private JobHandle _jobHandle;

        private static readonly Func<RTreeNode, AABB> GetNodeAabb = node => node.Aabb;
        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;

        public int EntriesCap { get; set; } = -1;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _rootNodesLevelIndices[subTreeIndex] + 1;

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return Enumerable.Empty<RTreeNode>();

            var offset = CalculateSubTreeNodeLevelStartIndex(subTreeIndex, subTreeHeight - 1, _subTreesMaxHeight,
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

        public AabbRTree()
        {
            TreeEntryTraits<RTreeNode>.InvalidEntry = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            InitInsertJob(InputEntriesStartCount, JobsUtility.JobWorkerCount);
        }

        public void Dispose()
        {
            _inputEntries.Dispose();
            _resultEntries.Dispose();

            _nodesContainer.Dispose();

            _nodesEndIndicesContainer.Dispose();
            _rootNodesLevelIndices.Dispose();
        }

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result)
        {
        }

        public void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result)
        {
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

            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount);
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

            _jobHandle = _insertJob.Schedule(_workersCount, 1);
            _jobHandle.Complete();

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
            {
                if (_rootNodesLevelIndices[i] < 0)
                    break;

                ++SubTreesCount;
            }
        }

        private void InitInsertJob(int entriesCount, int workersCount)
        {
            using var _ = Profiling.RTreeInitInsertJob.Auto();

            var treeStateHash = CalculateTreeStateHash(entriesCount, workersCount);
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

            var perWorkerEntriesCount = math.ceil((double) entriesCount / _workersCount);

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

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
                _rootNodesLevelIndices[i] = -1;

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
                    ResultEntries = _resultEntries,
                    NodesContainer = _nodesContainer,
                    NodesEndIndicesContainer = _nodesEndIndicesContainer,
                    RootNodesLevelIndices = _rootNodesLevelIndices
                }
            };
        }

        private static int CalculateSubTreeNodeLevelStartIndex(int subTreeIndex, int nodeLevelIndex, int treeMaxHeight,
            int perWorkerNodesContainerCapacity)
        {
            var index = math.pow(MaxEntries, treeMaxHeight + 1) / (MaxEntries - 1);
            index *= 1f - 1f / math.pow(MaxEntries, nodeLevelIndex);
            index += subTreeIndex * perWorkerNodesContainerCapacity;

            return (int) index;
        }

        private static int CalculateNodeLevelCapacity(int treeMaxHeight, int nodeLevelIndex) =>
            (int) math.pow(MaxEntries, treeMaxHeight - nodeLevelIndex);

        private static long CalculateTreeStateHash(int entriesCount, int workersCount) =>
            ((long) workersCount << 32) | (uint) entriesCount;
    }
}
