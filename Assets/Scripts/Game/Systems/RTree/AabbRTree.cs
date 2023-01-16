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
    internal static class InvalidEntry<T> where T : struct
    {
        public static T Entry;
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

    public class AabbRTree : IRTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int InputEntriesStartCount = 128;

        private long _treeStateHash;

        private int _subTreesCount;
        private int _treeMaxHeight;

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

        private bool _isJobScheduled;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _rootNodesLevelIndices[subTreeIndex] + 1;

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return Enumerable.Empty<RTreeNode>();

            var offset = math.pow(MaxEntries, _treeMaxHeight + 1) / (MaxEntries - 1);
            offset *= 1f - 1f / math.pow(MaxEntries, subTreeHeight - 1);
            offset += subTreeIndex * _perWorkerNodesContainerCapacity;

            return _nodesContainer
                .GetSubArray((int) offset, MaxEntries)
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
            InvalidEntry<RTreeNode>.Entry = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            InitInsertJob(InputEntriesStartCount, JobsUtility.JobWorkerCount);
        }

        private void InitInsertJob(int entriesCount, int workersCount)
        {
            var treeStateHash = GetTreeStateHash(entriesCount, workersCount);
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

            const int subTreeEntriesMinCount = MaxEntries * MinEntries * MinEntries;
            var maxSubTreesCount = (int) math.ceil((double) entriesCount / subTreeEntriesMinCount);
            _subTreesCount = math.min(workersCount, maxSubTreesCount);

            var perWorkerEntriesCount = math.ceil((double) entriesCount / _subTreesCount);

            _treeMaxHeight =
                (int) math.ceil(math.log((perWorkerEntriesCount * (MinEntries - 1) + MinEntries) / (2 * MinEntries - 1)) /
                                math.log(MinEntries));
            _treeMaxHeight = math.max(_treeMaxHeight - 1, 1);

            _perWorkerResultEntriesContainerCapacity = (int) math.pow(MinEntries,
                math.ceil(math.log2(perWorkerEntriesCount * MaxEntries / MinEntries) / math.log2(MinEntries)));
            var resultEntriesCount = _perWorkerResultEntriesContainerCapacity * _subTreesCount;

            if (!_resultEntries.IsCreated || _resultEntries.Length < resultEntriesCount)
            {
                if (_resultEntries.IsCreated)
                    _resultEntries.Dispose();

                _resultEntries = new NativeArray<RTreeLeafEntry>(resultEntriesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            _perWorkerNodesContainerCapacity =
                (int) (MaxEntries * (math.pow(MaxEntries, _treeMaxHeight) - 1) / (MaxEntries - 1));

            var nodesContainerCapacity = _perWorkerNodesContainerCapacity * _subTreesCount;
            if (!_nodesContainer.IsCreated || _nodesContainer.Length < nodesContainerCapacity)
            {
                if (_nodesContainer.IsCreated)
                    _nodesContainer.Dispose();

                _nodesContainer = new NativeArray<RTreeNode>(nodesContainerCapacity, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            var nodesEndIndicesContainerCapacity = _treeMaxHeight * _subTreesCount;
            if (!_nodesEndIndicesContainer.IsCreated || _nodesEndIndicesContainer.Length < nodesEndIndicesContainerCapacity)
            {
                if (_nodesEndIndicesContainer.IsCreated)
                    _nodesEndIndicesContainer.Dispose();

                _nodesEndIndicesContainer = new NativeArray<int>(nodesEndIndicesContainerCapacity, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            if (!_rootNodesLevelIndices.IsCreated || _rootNodesLevelIndices.Length < _subTreesCount)
            {
                if (_rootNodesLevelIndices.IsCreated)
                    _rootNodesLevelIndices.Dispose();

                _rootNodesLevelIndices = new NativeArray<int>(_subTreesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
                _rootNodesLevelIndices[i] = -1;

            _insertJob = new InsertJob
            {
                InsertJobReadOnlyData = new InsertJobReadOnlyData
                {
                    TreeMaxHeight = _treeMaxHeight,
                    PerWorkerEntriesCount = (int) perWorkerEntriesCount,
                    NodesContainerCapacity = _perWorkerNodesContainerCapacity,
                    ResultEntriesContainerCapacity = _perWorkerResultEntriesContainerCapacity,
                    EntriesTotalCount = entriesCount,
                    InputEntries = _inputEntries.AsReadOnly()
                },
                InsertJobSharedWriteData = new InsertJobSharedWriteData
                {
                    ResultEntries = _resultEntries,
                    NodesContainer = _nodesContainer,
                    NodesEndIndicesContainer = _nodesEndIndicesContainer,
                    RootNodesLevelIndices = _rootNodesLevelIndices
                }
            };
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

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep)
        {
            using var _ = Profiling.RTreeBuild.Auto();

            /*if (_isJobScheduled)
            {
                _jobHandle.Complete();
                _rootNodesIndex = _perThreadJobResults[0].TreeHeight - 1;

                _isJobScheduled = false;
            }*/
            SubTreesCount = 0;

            if (filter.IsEmpty())
                return;

            var entitiesCount = filter.GetEntitiesCount();

            InitInsertJob(entitiesCount, JobsUtility.JobWorkerCount);
            Assert.IsFalse(entitiesCount > _inputEntries.Length);

            Profiling.RTreeNativeArrayFill.Begin();
            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _inputEntries[index] = new RTreeLeafEntry(aabb, index);
            }

            Profiling.RTreeNativeArrayFill.End();

            _jobHandle = _insertJob.Schedule(_subTreesCount, 1);
            _jobHandle.Complete();

            for (var i = 0; i < _rootNodesLevelIndices.Length; i++)
            {
                if (_rootNodesLevelIndices[i] < 0)
                    break;

                ++SubTreesCount;
            }

            _isJobScheduled = true;
        }

        private static long GetTreeStateHash(int entriesCount, int workersCount) =>
            ((long) workersCount << 32) | (uint) entriesCount;
    }
}
