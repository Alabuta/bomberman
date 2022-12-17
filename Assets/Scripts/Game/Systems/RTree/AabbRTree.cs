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
        private const int RootNodeMinEntries = 2;

        private const double MaxMinEntriesRatio = (double) MaxEntries / MinEntries;

        private const int MaxInputEntriesCount = 993;

        private int WorkersCount;

        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;
        private static readonly Func<RTreeNode, AABB> GetNodeAabb = node => node.Aabb;

        private readonly int _leafEntriesMaxCount;

        private readonly List<NativeList<RTreeNode>> _nodes;

        private NativeArray<RTreeNode> _nodesContainer;
        private NativeArray<TreeLevelNodesRange> _nodeLevelsRanges;
        private NativeArray<int> _nodesEndIndicesContainer;

        private NativeList<RTreeLeafEntry> _leafEntries;
        private NativeArray<RTreeLeafEntry> _resultEntries;
        private NativeArray<ThreadInsertJobResult> _perThreadJobResults;

        private NativeArray<RTreeLeafEntry> _entries;

        private int _leafEntriesCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _leafEntriesCounter;
        private NativeArray<UnsafeAtomicCounter32> _leafEntriesCounterContainer;

        private int _workersInUseCount;
        [NativeDisableUnsafePtrRestriction]
        private UnsafeAtomicCounter32 _workersInUseCounter;
        private NativeArray<UnsafeAtomicCounter32> _workersInUseCounterContainer;

        private NativeArray<int> _rootNodesLevelIndices;
        private NativeArray<bool> _threadsInitFlagsContainer;
        private NativeArray<int> _perThreadWorkerIndices;

        private int _rootNodesIndex = -1;

        [NativeDisableUnsafePtrRestriction]
        private InsertJob _insertJob;

        [NativeDisableUnsafePtrRestriction]
        private JobHandle _jobHandle;
        private bool _isJobScheduled;

        public int TreeHeight2 => _rootNodesIndex + 1;

        public int SubTreesCount { get; private set; }

        public int GetSubTreeHeight(int subTreeIndex) =>
            SubTreesCount < 1 ? 0 : _perThreadJobResults[subTreeIndex].TreeHeight;

        public IEnumerable<RTreeNode> RootNodes2 =>
            TreeHeight2 > 0
                ? _nodes[_rootNodesIndex].TakeWhile(n => n.Aabb != AABB.Empty)
                : Enumerable.Empty<RTreeNode>();

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int subTreeIndex)
        {
            var subTreeHeight = GetSubTreeHeight(subTreeIndex);
            if (subTreeHeight < 1)
                return Enumerable.Empty<RTreeNode>();

            var rootRange = _nodeLevelsRanges[subTreeHeight - 1];
            return _nodesContainer
                .GetSubArray(rootRange.StartIndex, rootRange.Capacity)
                .TakeWhile(n => n.Aabb != AABB.Empty);
        }

        public IEnumerable<RTreeNode> GetNodes(int subTreeIndex, int levelIndex, IEnumerable<int> indices)
        {
            if (levelIndex < GetSubTreeHeight(subTreeIndex))
                return indices.Select(i => _nodesContainer[i]);

            return Enumerable.Empty<RTreeNode>();
        }

        public IEnumerable<RTreeLeafEntry> GetLeafEntries2(IEnumerable<int> indices) =>
            _leafEntries.Length != 0 ? indices.Select(i => _leafEntries[i]) : Enumerable.Empty<RTreeLeafEntry>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(IEnumerable<int> indices) =>
            _resultEntries.Length != 0 ? indices.Select(i => _resultEntries[i]) : Enumerable.Empty<RTreeLeafEntry>();

        private bool IsLevelRoot(int nodeLevelIndex) =>
            nodeLevelIndex == _rootNodesIndex;

        public AabbRTree()
        {
            InvalidEntry<RTreeNode>.Entry = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            _leafEntriesMaxCount =
                (int) math.pow(MinEntries,
                    math.ceil(math.log2(MaxInputEntriesCount * MaxMinEntriesRatio) / math.log2(MinEntries)));
            // _leafEntries = new NativeList<RTreeLeafEntry>(_leafEntriesMaxCount, Allocator.Persistent);

            /*var maxTreeHeight = (int) math.floor(math.log2((double) (EntriesCount + 1) / 2) / math.log2(MinEntries));

            _nodes = Enumerable.Range(0, maxTreeHeight - 1)
                .Select(levelIndex =>
                {
                    var capacity = (int) (math.pow(MinEntries, maxTreeHeight - levelIndex) * MaxMinEntriesRatio);
                    // var capacity = (int) math.pow(MaxEntries, maxTreeHeight - levelIndex);
                    return new NativeList<RTreeNode>(capacity, Allocator.Persistent);
                })
                .Append(new NativeList<RTreeNode>(MaxEntries, Allocator.Persistent))
                .ToList();*/

            InitInsertJob();
        }

        private int GetNodesContainerCapacityPerWorker(int workersCount)
        {
            var maxTreeHeight = (int) math.floor(math.log2((double) (MaxInputEntriesCount + 1) / 2) / math.log2(MinEntries));

            var entriesCountPerWorker = (double) MaxInputEntriesCount / workersCount;
            entriesCountPerWorker = math.ceil(math.log2(entriesCountPerWorker) / math.log2(MaxEntries));
            entriesCountPerWorker = math.pow(MaxEntries, entriesCountPerWorker);

            var treeHeight = (int) math.floor(math.log2((entriesCountPerWorker + 1) / 2) / math.log2(MinEntries));

            return 0;
        }

        private void InitInsertJob()
        {
            WorkersCount = 1; //math.min(4, JobsUtility.JobWorkerCount);

            /*
             *
             */

            // var maxTreeHeight = (int) math.ceil(math.log2((double) entriesCount / MinEntries) / math.log2(MaxEntries));
            var maxTreeHeight = (int) math.floor(math.log2((double) (MaxInputEntriesCount + 1) / 2) / math.log2(MinEntries));

            _entries = new NativeArray<RTreeLeafEntry>(_leafEntriesMaxCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _resultEntries = new NativeArray<RTreeLeafEntry>(_leafEntriesMaxCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            var prevLevelStartIndex = 0;
            var nodeLevelsRanges = Enumerable
                .Range(0, maxTreeHeight - 1)
                .Select(levelIndex =>
                {
                    var capacity = (int) (math.pow(MinEntries, maxTreeHeight - levelIndex) * MaxMinEntriesRatio);
                    // var capacity = (int) math.pow(MaxEntries, maxTreeHeight - levelIndex);

                    var range = new TreeLevelNodesRange(prevLevelStartIndex, capacity);
                    prevLevelStartIndex += capacity;

                    return range;
                })
                .ToArray();

            nodeLevelsRanges = nodeLevelsRanges
                .Append(new TreeLevelNodesRange(prevLevelStartIndex, MaxEntries))
                .ToArray();

            nodeLevelsRanges = Enumerable
                .Repeat(nodeLevelsRanges, WorkersCount)
                .SelectMany(r => r)
                .ToArray();

            _nodeLevelsRanges = new NativeArray<TreeLevelNodesRange>(nodeLevelsRanges, Allocator.Persistent);

            var nodesCapacity = prevLevelStartIndex + MaxEntries;

            _nodesContainer = new NativeArray<RTreeNode>(nodesCapacity * WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _nodesEndIndicesContainer = new NativeArray<int>(maxTreeHeight * WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _rootNodesLevelIndices = new NativeArray<int>(WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            _perThreadJobResults = new NativeArray<ThreadInsertJobResult>(WorkersCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);

            unsafe
            {
                _leafEntriesCount = 0;
                fixed ( int* count = &_leafEntriesCount ) _leafEntriesCounter = new UnsafeAtomicCounter32(count);
            }

            _leafEntriesCounterContainer = new NativeArray<UnsafeAtomicCounter32>(1, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            _leafEntriesCounterContainer[0] = _leafEntriesCounter;

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
                NodesContainerCapacity = nodesCapacity,
                Entries = _entries.AsReadOnly(),
                NodesContainer = _nodesContainer,
                NodesEndIndicesContainer = _nodesEndIndicesContainer,
                NodesRangeContainer = _nodeLevelsRanges,
                RootNodesLevelIndices = _rootNodesLevelIndices,
                ResultEntries = _resultEntries,
                PerThreadJobResults = _perThreadJobResults,
                LeafEntriesCounter = _leafEntriesCounterContainer,
                PerThreadWorkerIndices = _perThreadWorkerIndices,
                WorkersInUseCounter = _workersInUseCounterContainer
            };
        }

        public void Dispose()
        {
            _leafEntriesCounterContainer.Dispose();
            _entries.Dispose();
            _nodeLevelsRanges.Dispose();

            _nodesEndIndicesContainer.Dispose();

            _perThreadJobResults.Dispose();

            _nodesContainer.Dispose();

            _resultEntries.Dispose();

            foreach (var nodes in _nodes)
                nodes.Dispose();

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
            _rootNodesIndex = -1;
            SubTreesCount = 0;

            if (filter.IsEmpty())
                return;

            /*_leafEntries.Clear();

            foreach (var nodes in _nodes)
                nodes.Clear();*/

            var entitiesCount = filter.GetEntitiesCount();
#if ENABLE_ASSERTS
            Assert.IsTrue(entitiesCount <= _leafEntriesMaxCount);
            Assert.IsTrue(math.log2((float) entitiesCount / MaxEntries) / math.log2(MaxEntries) <= _nodes.Count);
#endif

            Profiling.RTreeNativeArrayFill.Begin();
            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);

                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _entries[index] = new RTreeLeafEntry(aabb, index);
            }

            Profiling.RTreeNativeArrayFill.End();

            _leafEntriesCounterContainer[0].Reset();
            _workersInUseCounterContainer[0].Reset();

            for (var i = 0; i < _perThreadWorkerIndices.Length; i++)
                _perThreadWorkerIndices[i] = -1;

            /*for (var i = 0; i < _threadsInitFlagsContainer.Length; i++)
                _threadsInitFlagsContainer[i] = false;*/

            var batchSize = entitiesCount; //(int) math.ceil((double) entitiesCount / WorkersCount);
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
