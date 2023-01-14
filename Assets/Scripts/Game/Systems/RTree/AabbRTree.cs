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

        private const int MaxInputEntriesCount = 993;

        private int _workersCount;
        private int _treeMaxHeight;

        private NativeArray<RTreeLeafEntry> _inputEntries;
        private NativeArray<RTreeLeafEntry> _resultEntries;

        private NativeArray<RTreeNode> _nodesContainer;
        private NativeArray<int> _nodesEndIndicesContainer;
        private NativeArray<int> _rootNodesLevelIndices;
        private NativeArray<int> _perThreadWorkerIndices;

        private int _perWorkerResultEntriesContainerCapacity;
        private int _perWorkerNodesContainerCapacity;

        private int _workersInUseCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _workersInUseCounter;
        private NativeArray<UnsafeAtomicCounter32> _workersInUseCounterContainer;

        private NativeArray<ThreadInsertJobResult> _perThreadJobResults;

        [NativeDisableUnsafePtrRestriction]
        private InsertJob _insertJob;

        [NativeDisableUnsafePtrRestriction]
        private JobHandle _jobHandle;

        private bool _isJobScheduled;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _perThreadJobResults[subTreeIndex].TreeHeight;

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

            // Calculate(82, 2);
            InitInsertJob();
        }

        private void Calculate(int entriesCount, int subTreesCount)
        {
            if (!_inputEntries.IsCreated || _inputEntries.Length < entriesCount)
            {
                if (_inputEntries.IsCreated)
                    _inputEntries.Dispose();

                _inputEntries = new NativeArray<RTreeLeafEntry>(entriesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            const int subTreeEntriesMinCount = MaxEntries * MinEntries * MinEntries;
            var maxSubTreesCount = (int) math.ceil((double) entriesCount / subTreeEntriesMinCount);
            subTreesCount = math.min(subTreesCount, maxSubTreesCount);

            var perSubTreeEntriesCount = math.ceil((double) entriesCount / subTreesCount);
            var treeMaxHeight =
                (int) math.ceil(math.log((perSubTreeEntriesCount * (MinEntries - 1) + MinEntries) / (2 * MinEntries - 1)) /
                                math.log(MinEntries));
            treeMaxHeight = math.max(treeMaxHeight - 1, 1);

            var perSubTreeLeafsCount = (int) math.pow(MaxEntries, treeMaxHeight + 1);
            var resultEntriesCount = perSubTreeLeafsCount * subTreesCount;

            if (!_resultEntries.IsCreated || _resultEntries.Length < resultEntriesCount)
            {
                if (_resultEntries.IsCreated)
                    _resultEntries.Dispose();

                _resultEntries = new NativeArray<RTreeLeafEntry>(resultEntriesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            var nodesContainerCapacity = _perWorkerNodesContainerCapacity * _workersCount;
            if (!_nodesContainer.IsCreated || _nodesContainer.Length < nodesContainerCapacity)
            {
                if (_nodesContainer.IsCreated)
                    _nodesContainer.Dispose();

                _nodesContainer = new NativeArray<RTreeNode>(nodesContainerCapacity, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }

            if (!_nodesEndIndicesContainer.IsCreated || _nodesEndIndicesContainer.Length < treeMaxHeight)
            {
                if (_nodesEndIndicesContainer.IsCreated)
                    _nodesEndIndicesContainer.Dispose();

                _nodesEndIndicesContainer = new NativeArray<int>(treeMaxHeight * subTreesCount, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
            }
        }

        private void InitInsertJob()
        {
            Debug.LogWarning(
                $"JobWorkerCount {JobsUtility.JobWorkerCount}, JobWorkerMaximumCount {JobsUtility.JobWorkerMaximumCount}, MaxJobThreadCount {JobsUtility.MaxJobThreadCount}");
            _workersCount = JobsUtility.JobWorkerCount;
            var perWorkerEntriesCount = math.ceil((double) MaxInputEntriesCount / _workersCount);

            _treeMaxHeight =
                (int) math.ceil(math.log((perWorkerEntriesCount * (MinEntries - 1) + MinEntries) / (2 * MinEntries - 1)) /
                                math.log(MinEntries));
            _treeMaxHeight = math.max(_treeMaxHeight - 1, 1);

            _inputEntries = new NativeArray<RTreeLeafEntry>(MaxInputEntriesCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _perWorkerResultEntriesContainerCapacity = (int) math.pow(MinEntries,
                math.ceil(math.log2(perWorkerEntriesCount * ((double) MaxEntries / MinEntries)) /
                          math.log2(MinEntries)));
            _resultEntries = new NativeArray<RTreeLeafEntry>(_perWorkerResultEntriesContainerCapacity * _workersCount,
                Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            _perWorkerNodesContainerCapacity =
                (int) (MaxEntries * (math.pow(MaxEntries, _treeMaxHeight) - 1) / (MaxEntries - 1));

            var nodesContainerCapacity = _perWorkerNodesContainerCapacity * _workersCount;
            _nodesContainer = new NativeArray<RTreeNode>(nodesContainerCapacity, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _nodesEndIndicesContainer = new NativeArray<int>(_treeMaxHeight * _workersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _rootNodesLevelIndices = new NativeArray<int>(_workersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _perThreadJobResults = new NativeArray<ThreadInsertJobResult>(_workersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            unsafe
            {
                _workersInUseCount = 0;
                fixed ( int* count = &_workersInUseCount ) _workersInUseCounter = new UnsafeAtomicCounter32(count);
            }

            _workersInUseCounterContainer = new NativeArray<UnsafeAtomicCounter32>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            _workersInUseCounterContainer[0] = _workersInUseCounter;

            var perThreadWorkerIndices = Enumerable
                .Repeat(-1, JobsUtility.MaxJobThreadCount)
                .ToArray();

            _perThreadWorkerIndices = new NativeArray<int>(perThreadWorkerIndices, Allocator.Persistent);

            _insertJob = new InsertJob
            {
                TreeMaxHeight = _treeMaxHeight,
                NodesContainerCapacity = _perWorkerNodesContainerCapacity,
                ResultEntriesContainerCapacity = _perWorkerResultEntriesContainerCapacity,
                InputEntries = _inputEntries.AsReadOnly(),
                NodesContainer = _nodesContainer,
                NodesEndIndicesContainer = _nodesEndIndicesContainer,
                RootNodesLevelIndices = _rootNodesLevelIndices,
                ResultEntries = _resultEntries,
                PerThreadJobResults = _perThreadJobResults,
                PerThreadWorkerIndices = _perThreadWorkerIndices,
                WorkersInUseCounter = _workersInUseCounterContainer
            };
        }

        public void Dispose()
        {
            _inputEntries.Dispose();
            _resultEntries.Dispose();

            _nodesContainer.Dispose();

            _rootNodesLevelIndices.Dispose();
            _nodesEndIndicesContainer.Dispose();

            _workersInUseCounterContainer.Dispose();

            _perThreadWorkerIndices.Dispose();
            _perThreadJobResults.Dispose();
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
            Assert.IsFalse(entitiesCount > _inputEntries.Length);

            Profiling.RTreeNativeArrayFill.Begin();
            foreach (var index in filter)
            {
                if (index > entitiesCount)
                    break;

                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _inputEntries[index] = new RTreeLeafEntry(aabb, index);
            }

            Profiling.RTreeNativeArrayFill.End();

            _workersInUseCounterContainer[0].Reset();

            for (var i = 0; i < _perThreadWorkerIndices.Length; i++)
                _perThreadWorkerIndices[i] = -1;

            var batchSize = (int) math.ceil((double) entitiesCount / _workersCount);
            _jobHandle = _insertJob.Schedule(entitiesCount, batchSize);
            _jobHandle.Complete();

            for (var i = 0; i < _perThreadJobResults.Length; i++)
            {
                var jobResult = _perThreadJobResults[i];
                if (jobResult.TreeHeight < 1)
                    continue;

                ++SubTreesCount;
            }

            _isJobScheduled = true;
        }
    }
}
