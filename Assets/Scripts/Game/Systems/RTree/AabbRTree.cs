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

        private const double MaxMinEntriesRatio = (double) MaxEntries / MinEntries;

        private const int MaxInputEntriesCount = 993;
        private const int MaxWorkersCount = 1;

        private int WorkersCount;

        private int _leafEntriesMaxCount;

        private NativeArray<RTreeNode> _nodesContainer;
        private NativeArray<TreeLevelNodesRange> _nodeLevelsRanges;
        private NativeArray<int> _nodesEndIndicesContainer;

        private NativeList<RTreeLeafEntry> _leafEntries;
        private NativeArray<RTreeLeafEntry> _resultEntries;
        private NativeArray<RTreeLeafEntry> _resultEntries2;
        private NativeArray<ThreadInsertJobResult> _perThreadJobResults;

        private NativeArray<RTreeLeafEntry> _inputEntries;

        private int _workersInUseCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _workersInUseCounter;
        private NativeArray<UnsafeAtomicCounter32> _workersInUseCounterContainer;

        private NativeArray<int> _rootNodesLevelIndices;
        private NativeArray<bool> _threadsInitFlagsContainer;
        private NativeArray<int> _perThreadWorkerIndices;
        private int _perWorkerResultEntriesContainerCapacity;

        [NativeDisableUnsafePtrRestriction]
        private InsertJob _insertJob;

        [NativeDisableUnsafePtrRestriction]
        private JobHandle _jobHandle;

        private bool _isJobScheduled;
        private int _maxTreeHeight;
        private int _perWorkerNodesContainerCapacity;
        private int frameIndex;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _perThreadJobResults[subTreeIndex].TreeHeight;

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return Enumerable.Empty<RTreeNode>();

            var rootRange = _nodeLevelsRanges[subTreeHeight - 1];
            var offset = _perWorkerNodesContainerCapacity * subTreeIndex;
            return _nodesContainer
                .GetSubArray(offset + rootRange.StartIndex, MaxEntries)
                .TakeWhile(n => n.Aabb != AABB.Empty);
        }

        public IEnumerable<RTreeNode> GetNodes(int subTreeIndex, int levelIndex, IEnumerable<int> indices)
        {
            if (levelIndex >= GetSubTreeHeight(subTreeIndex))
                return Enumerable.Empty<RTreeNode>();

            var offset = _perWorkerNodesContainerCapacity * subTreeIndex;

            var currentThreadNodes = _nodesContainer.GetSubArray(offset, _perWorkerNodesContainerCapacity);
            // return indices.Select(i => currentThreadNodes[i]);
            return indices.Select(i => _nodesContainer[offset + i]);
        }

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

            var nodeLevelsRangesCount = treeMaxHeight * subTreesCount;
            if (!_nodeLevelsRanges.IsCreated || _nodeLevelsRanges.Length < nodeLevelsRangesCount)
            {
                if (_nodeLevelsRanges.IsCreated)
                    _nodeLevelsRanges.Dispose();

                var nodesFlatCount = (int) (MaxEntries * (math.pow(MaxEntries, treeMaxHeight) - 1) / (MaxEntries - 1));
                var prefix = math.pow(MaxEntries, treeMaxHeight + 1) / (MaxEntries - 1);

                var nodeLevelsRanges = Enumerable
                    .Range(0, subTreesCount)
                    .SelectMany(subTreeIndex =>
                    {
                        return Enumerable
                            .Range(0, treeMaxHeight)
                            .Select(levelIndex =>
                            {
                                var capacity = (int) math.pow(MaxEntries, treeMaxHeight - levelIndex);
                                var indexOffset = (int) (prefix * (1f - 1f / math.pow(MaxEntries, levelIndex)));
                                indexOffset += subTreeIndex * nodesFlatCount;

                                return new TreeLevelNodesRange(indexOffset, capacity);
                            });
                    })
                    .ToArray();

                _nodeLevelsRanges = new NativeArray<TreeLevelNodesRange>(nodeLevelsRanges, Allocator.Persistent);
            }

            var nodesCapacity = _nodeLevelsRanges[^1].StartIndex + MaxEntries;
            if (!_nodesContainer.IsCreated || _nodesContainer.Length < nodesCapacity)
            {
                if (_nodesContainer.IsCreated)
                    _nodesContainer.Dispose();

                _nodesContainer = new NativeArray<RTreeNode>(nodesCapacity, Allocator.Persistent,
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
            WorkersCount = JobsUtility.JobWorkerCount; //math.min(4, JobsUtility.JobWorkerCount);
            var perWorkerEntriesCount = math.ceil((double) MaxInputEntriesCount / WorkersCount);

            // var maxTreeHeight = (int) math.ceil(math.log2((double) entriesCount / MinEntries) / math.log2(MaxEntries));
            // var maxTreeHeight = (int) math.floor(math.log2((double) (MaxInputEntriesCount + 1) / 2) / math.log2(MinEntries));
            var maxTreeHeight =
                (int) math.ceil(math.log((perWorkerEntriesCount * (MinEntries - 1) + MinEntries) / (2 * MinEntries - 1)) /
                                math.log(MinEntries));
            maxTreeHeight = math.max(maxTreeHeight - 1, 1);
            _maxTreeHeight = maxTreeHeight;

            _inputEntries = new NativeArray<RTreeLeafEntry>(MaxInputEntriesCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _perWorkerResultEntriesContainerCapacity = (int) math.pow(MinEntries,
                math.ceil(math.log2(perWorkerEntriesCount * MaxMinEntriesRatio) /
                          math.log2(MinEntries)));
            _resultEntries = new NativeArray<RTreeLeafEntry>(_perWorkerResultEntriesContainerCapacity * WorkersCount,
                Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            _perWorkerNodesContainerCapacity =
                (int) (MaxEntries * (math.pow(MaxEntries, maxTreeHeight) - 1) / (MaxEntries - 1));
            var prefix = math.pow(MaxEntries, maxTreeHeight + 1) / (MaxEntries - 1);

            var nodeLevelsRanges = Enumerable
                .Range(0, WorkersCount)
                .SelectMany(subTreeIndex =>
                {
                    return Enumerable
                        .Range(0, maxTreeHeight)
                        .Select(levelIndex =>
                        {
                            var capacity = (int) math.pow(MaxEntries, maxTreeHeight - levelIndex);
                            var indexOffset = (int) (prefix * (1f - 1f / math.pow(MaxEntries, levelIndex)));
                            // indexOffset += subTreeIndex * perWorkerNodesContainerCapacity;

                            return new TreeLevelNodesRange(indexOffset, capacity);
                        });
                })
                .ToArray();

            _nodeLevelsRanges = new NativeArray<TreeLevelNodesRange>(nodeLevelsRanges, Allocator.Persistent);

            var nodesContainerCapacity = _perWorkerNodesContainerCapacity * WorkersCount;
            _nodesContainer = new NativeArray<RTreeNode>(nodesContainerCapacity, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _nodesEndIndicesContainer = new NativeArray<int>(maxTreeHeight * WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _rootNodesLevelIndices = new NativeArray<int>(WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _perThreadJobResults = new NativeArray<ThreadInsertJobResult>(WorkersCount, Allocator.Persistent,
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
                MaxTreeHeight = maxTreeHeight,
                NodesContainerCapacity = _perWorkerNodesContainerCapacity,
                ResultEntriesContainerCapacity = _perWorkerResultEntriesContainerCapacity,
                InputEntries = _inputEntries.AsReadOnly(),
                NodesContainer = _nodesContainer,
                NodesEndIndicesContainer = _nodesEndIndicesContainer,
                NodesRangeContainer = _nodeLevelsRanges,
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
            _nodeLevelsRanges.Dispose();

            _nodesEndIndicesContainer.Dispose();

            _perThreadJobResults.Dispose();

            _nodesContainer.Dispose();

            _resultEntries.Dispose();

            _leafEntries.Dispose();
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
            Assert.IsFalse(entitiesCount > MaxInputEntriesCount);
            /*Assert.IsFalse(entitiesCount > _inputEntries.Length);
            Assert.IsFalse(entitiesCount > _resultEntries.Length);*/

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

            /*for (var i = 0; i < _threadsInitFlagsContainer.Length; i++)
                _threadsInitFlagsContainer[i] = false;*/

            var batchSize = (int) math.ceil((double) entitiesCount / WorkersCount);
            _jobHandle = _insertJob.Schedule(entitiesCount, batchSize); //entitiesCount
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
