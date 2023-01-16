using System.Collections.Generic;
using System.Linq;
using App;
using Game.Components;
using Game.Components.Tags;
using Game.Systems.RTree;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Systems
{
    public sealed partial class EntitiesAabbTree : IRTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int JobsCount = MaxEntries;

        private NativeList<RTreeLeafEntry> _entriesNativeList;
        private NativeArray<AABB> _resultAabb;
        private readonly NativeArray<AABB>[] _results;

        private readonly JobHandle[] _jobHandlers;

        private int RootNodesIndex => _nodesCountByLevel.Count - 1;

        public int SubTreesCount => 1;

        public int GetSubTreeHeight(int _) => _nodesCountByLevel.Count;

        public int GetSubTreeHeight() => _nodesCountByLevel.Count;

        public IEnumerable<RTreeNode> GetSubTreeRootNodes(int _) =>
            GetSubTreeHeight() > 0
                ? _nodes[RootNodesIndex].TakeWhile(n => n.Aabb != AABB.Empty)
                : Enumerable.Empty<RTreeNode>();

        public IEnumerable<RTreeNode> GetNodes(int _, int levelIndex, IEnumerable<int> indices) =>
            levelIndex < _nodes.Count
                ? indices.Select(i => _nodes[RootNodesIndex - levelIndex][i])
                : Enumerable.Empty<RTreeNode>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(int _, IEnumerable<int> indices) =>
            _leafEntries.Count != 0 ? indices.Select(i => _leafEntries[i]) : Enumerable.Empty<RTreeLeafEntry>();

        public EntitiesAabbTree()
        {
            InvalidEntry<RTreeNode>.Entry = new RTreeNode
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };
            InvalidEntry<RTreeNode>.Range = Enumerable.Repeat(InvalidEntry<RTreeNode>.Entry, MaxEntries).ToArray();

            InvalidEntry<RTreeLeafEntry>.Entry = new RTreeLeafEntry(AABB.Empty, -1);
            InvalidEntry<RTreeLeafEntry>.Range = Enumerable.Repeat(InvalidEntry<RTreeLeafEntry>.Entry, MaxEntries).ToArray();

            _entriesNativeList = new NativeList<RTreeLeafEntry>(MaxEntries, Allocator.Persistent);
            _resultAabb = new NativeArray<AABB>(1, Allocator.Persistent);

            _nodes.Add(Enumerable.Repeat(InvalidEntry<RTreeNode>.Entry, MaxEntries).ToList());

            _results = Enumerable
                // .Repeat(new NativeArray<AABB>(1, Allocator.Persistent), jobsCount)
                .Range(0, JobsCount)
                .Select(_ => new NativeArray<AABB>(1, Allocator.Persistent))
                .ToArray();

            _jobHandlers = Enumerable.Repeat(new JobHandle(), JobsCount).ToArray();
        }

        public void Dispose()
        {
            _entriesNativeList.Dispose();
            _resultAabb.Dispose();

            foreach (var nativeArray in _results)
                nativeArray.Dispose();
        }

        private static class InvalidEntry<T> where T : struct
        {
            public static T Entry;
            public static T[] Range;
        }

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result)
        {
            var rootNodesCount = _nodesCountByLevel[RootNodesIndex];
            for (var nodeIndex = 0; nodeIndex < rootNodesCount; nodeIndex++)
                QueryNodesByLine(p0, p1, result, RootNodesIndex, nodeIndex);
        }

        private void QueryNodesByLine(
            fix2 p0, fix2 p1,
            ICollection<RTreeLeafEntry> result,
            int levelIndex, int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!node.Aabb.CohenSutherlandLineClip(ref p0, ref p1))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex == 0)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    if (!IntersectedByLine(p0, p1, _leafEntries[i].Aabb))
                        continue;

                    result.Add(_leafEntries[i]);
                }

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByLine(p0, p1, result, levelIndex - 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, in AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result)
        {
            var rootNodesCount = _nodesCountByLevel[RootNodesIndex];
            for (var nodeIndex = 0; nodeIndex < rootNodesCount; nodeIndex++)
                QueryNodesByAabb(aabb, result, RootNodesIndex, nodeIndex);
        }

        private void QueryNodesByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result, int levelIndex,
            int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!fix.is_AABB_overlapped_by_AABB(aabb, node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex < 1)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                    result.Add(_leafEntries[i]);

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByAabb(aabb, result, levelIndex - 1, i);
        }

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep)
        {
            using var _ = Profiling.RTreeBuild.Auto();

            _nodesCountByLevel.Clear();
            _leafEntriesCount = 0;

            if (filter.IsEmpty())
                return;

            var entitiesCount = filter.GetEntitiesCount();

            Profiling.RTreeNativeArrayFill.Begin();
            if (_entriesNativeList.Length < entitiesCount)
                _entriesNativeList.ResizeUninitialized(entitiesCount);

            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);
                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                _entriesNativeList[index] = new RTreeLeafEntry(aabb, index);
            }

            Profiling.RTreeNativeArrayFill.End();

            /*Profiling.RTreeCalculateAabbJob.Begin();
            var job = new CalculateTotalAABBJob
            {
                Entries = _entriesNativeList,
                ResultAABB = _resultAabb
            };
            var jobHandle = job.Schedule();
            jobHandle.Complete();
            Profiling.RTreeCalculateAabbJob.End();

            Profiling.RTreeCalculateAabbJobPar.Begin();
            Profiling.RTreeAabbCalculate1.Begin();
            var entriesPerJob = entitiesCount / JobsCount;
            var extraEntriesCount = entitiesCount % JobsCount;

            for (var jobIndex = 0; jobIndex < _jobHandlers.Length; jobIndex++)
            {
                var array = _entriesNativeList.AsArray();
                var start = jobIndex * entriesPerJob;
                var length = entriesPerJob + (jobIndex == JobsCount - 1 ? extraEntriesCount : 0);
                var slice = array.Slice(start, length);
                var j = new CalculateTotalAABBJobPar
                {
                    Entries = slice,
                    ResultAABB = _results[jobIndex]
                };

                _jobHandlers[jobIndex] = j.Schedule();
            }
            Profiling.RTreeAabbCalculate1.End();

            var totalAabb = AABB.Empty;
            for (var jobIndex = 0; jobIndex < _jobHandlers.Length; jobIndex++)
            {
                ref var handler = ref _jobHandlers[jobIndex];
                handler.Complete();
                totalAabb = fix.AABBs_conjugate(totalAabb, _results[jobIndex][0]);
            }
            Profiling.RTreeCalculateAabbJobPar.End();

            // var totalAabb = _resultAabb[0];
            var subSize = (totalAabb.max - totalAabb.min) / new fix2(2);

            var subAabb = new AABB(totalAabb.min / new fix2(2), totalAabb.max / new fix2(2));*/

            _nodesCountByLevel.Add(MaxEntries);

            var rootNodes = _nodes[RootNodesIndex];
            for (var i = 0; i < MaxEntries; i++)
                rootNodes[i] = InvalidEntry<RTreeNode>.Entry;

            Profiling.RTreeInsert.Begin();
            for (var i = 0; i < entitiesCount; i++)
            {
                var entry = _entriesNativeList[i];
                Insert(entry);
            }

            Profiling.RTreeInsert.End();
        }
    }
}
