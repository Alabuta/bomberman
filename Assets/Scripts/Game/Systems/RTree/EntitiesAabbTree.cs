using System;
using System.Collections.Generic;
using System.Linq;
using App;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Collections;
using Unity.Jobs;

namespace Game.Systems.RTree
{
    public sealed partial class EntitiesAabbTree : IRTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int JobsCount = MaxEntries;

        private NativeList<RTreeLeafEntry> _entriesNativeList;
        private NativeArray<AABB> _resultAabb;
        private readonly NativeArray<AABB>[] _results;

        private readonly List<List<RTreeNode>> _nodes = new();
        private readonly List<int> _nodesCountByLevel = new(8);

        private readonly List<RTreeLeafEntry> _leafEntries = new(1024);
        private int _leafEntriesCount;

        private RTreeNode _tempNodeEntry;

        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;
        private static readonly Func<RTreeNode, AABB> GetNodeAabb = node => node.Aabb;

        private readonly JobHandle[] _jobHandlers;

        private int RootNodesIndex => _nodesCountByLevel.Count - 1;

        public int EntriesCap { get; set; } = -1;

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


        private void Insert(in RTreeLeafEntry entry)
        {
            var rootLevelIndex = RootNodesIndex;
            const int rootEntriesStartIndex = 0;

#if ENABLE_ASSERTS
            Assert.IsTrue(GetSubTreeHeight() > 0);
#endif
            var rootNodes = _nodes[rootLevelIndex];
            var rootNodesCount = _nodesCountByLevel[RootNodesIndex];
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            var nonEmptyNodesCount = 0;
            for (var i = 0; i < rootNodesCount; i++)
                nonEmptyNodesCount += rootNodes[i].Aabb != AABB.Empty ? 1 : 0;
#if ENABLE_ASSERTS
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.Take(rootNodesCount).Count(n => n.EntriesCount > 0));
#endif

            var nodeIndex = GetNodeIndexToInsert(rootNodes, GetNodeAabb, rootEntriesStartIndex,
                rootEntriesStartIndex + nonEmptyNodesCount, nonEmptyNodesCount < MinEntries, entry);
#if ENABLE_ASSERTS
            Assert.IsTrue(nodeIndex > -1);
#endif

            var targetNode = rootNodes[nodeIndex];
            var extraNode = ChooseLeaf(ref targetNode, rootLevelIndex, entry);
#if ENABLE_ASSERTS
            Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(targetNode.EntriesCount, 0);
            Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

            rootNodes[nodeIndex] = targetNode;

            if (!extraNode.HasValue)
                return;

#if ENABLE_ASSERTS
            Assert.AreNotEqual(extraNode.Value.Aabb, AABB.Empty);
            Assert.AreNotEqual(extraNode.Value.EntriesCount, 0);
            Assert.AreNotEqual(extraNode.Value.EntriesStartIndex, -1);
#endif

            var candidateNodeIndex = nodeIndex + 1;
            for (; candidateNodeIndex < MaxEntries; candidateNodeIndex++)
                if (rootNodes[candidateNodeIndex].Aabb == AABB.Empty)
                    break;

            // var candidateNodeIndex = rootNodes.FindIndex(n => n.Aabb == AABB.Empty);
            if (candidateNodeIndex != MaxEntries && candidateNodeIndex < rootNodesCount)
            {
                rootNodes[candidateNodeIndex] = extraNode.Value;
                return;
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(rootNodes
                .Take(rootNodesCount)
                .All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
#endif

            GrowTree(extraNode.Value);
        }

        private RTreeNode? ChooseLeaf(ref RTreeNode node, int nodeLevelIndex, in RTreeLeafEntry entry)
        {
            var isLeafLevel = nodeLevelIndex == 0;
            if (isLeafLevel)
            {
                using var _ = Profiling.RTreeLeafNodesUpdate.Auto();

                if (node.EntriesCount == MaxEntries)
                {
                    Profiling.RTreeB.Begin();
                    var splitNode = SplitNode(ref node, _leafEntries, _leafEntriesCount, entry, GetLeafEntryAabb);
                    Profiling.RTreeB.End();
                    _leafEntriesCount += MaxEntries;
                    return splitNode;
                }

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntriesCount;
                    _leafEntriesCount += MaxEntries;

                    if (_leafEntries.Count < _leafEntriesCount)
                        _leafEntries.AddRange(InvalidEntry<RTreeLeafEntry>.Range);
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                node.EntriesCount++;

                return null;
            }

            using var __ = Profiling.RTreeNodesUpdate.Auto();

            var entriesCount = node.EntriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);
#endif

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
#if ENABLE_ASSERTS
            Assert.IsTrue(entriesStartIndex > -1);
#endif

            var childNodeLevelIndex = nodeLevelIndex - 1;
            var childLevelNodes = _nodes[childNodeLevelIndex];
            var childNodeIndex =
                GetNodeIndexToInsert(childLevelNodes, GetNodeAabb, entriesStartIndex, entriesEndIndex, false, entry);
#if ENABLE_ASSERTS
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);
#endif

            var targetChildNode = _nodes[childNodeLevelIndex][childNodeIndex];
            var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, entry);

            childLevelNodes[childNodeIndex] = targetChildNode;

            if (!extraChildNode.HasValue)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                return null;
            }

            var newChildNode = extraChildNode.Value;
            if (entriesCount == MaxEntries)
            {
                var splitNode = SplitNode(ref node, childLevelNodes, _nodesCountByLevel[childNodeLevelIndex], newChildNode,
                    GetNodeAabb);
                _nodesCountByLevel[childNodeLevelIndex] += MaxEntries;
                return splitNode;
            }

            childLevelNodes[entriesStartIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
            node.EntriesCount++;

#if ENABLE_ASSERTS
            Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries);
#endif
            return null;
        }

        private void GrowTree(in RTreeNode newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodes = _nodes[RootNodesIndex];
            var rootNodesCount = _nodesCountByLevel[RootNodesIndex];
            // rootNodes[0].Aabb = AABB.Empty;
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            /*var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = 0,
                EntriesCount = rootNodesCount
            };*/
            _tempNodeEntry.EntriesStartIndex = 0;
            _tempNodeEntry.EntriesCount = rootNodesCount;

            var newRootNodeB = SplitNode(ref _tempNodeEntry, rootNodes, rootNodesCount, newEntry, GetNodeAabb);

            _nodesCountByLevel[RootNodesIndex] += MaxEntries;
            _nodesCountByLevel.Add(MaxEntries);

            if (_nodes.Count < GetSubTreeHeight())
            {
                var newRootNodes = new List<RTreeNode> { _tempNodeEntry, newRootNodeB };
                newRootNodes.AddRange(Enumerable.Repeat(InvalidEntry<RTreeNode>.Entry, MaxEntries - 2));
                _nodes.Add(newRootNodes);
            }
            else
            {
                var newRootNodes = _nodes[RootNodesIndex];
                if (newRootNodes.Count < MaxEntries)
                {
                    newRootNodes.Add(_tempNodeEntry);
                    newRootNodes.Add(newRootNodeB);
                    newRootNodes.AddRange(Enumerable.Repeat(InvalidEntry<RTreeNode>.Entry, MaxEntries - 2));
                }
                else
                {
                    newRootNodes[0] = _tempNodeEntry;
                    newRootNodes[1] = newRootNodeB;

                    for (var i = 2; i < MaxEntries; i++)
                        newRootNodes[i] = InvalidEntry<RTreeNode>.Entry;
                }
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.Aabb != AABB.Empty) >= MinEntries);
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.EntriesStartIndex != -1) >= MinEntries);
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.EntriesCount > 0) >= MinEntries);
#endif
        }

        private static RTreeNode SplitNode<T>(ref RTreeNode splitNode, List<T> nodeEntries, int nodeEntriesCount,
            in T newEntry, Func<T, AABB> getAabbFunc) where T : struct
        {
            using var _ = Profiling.RTreeSplitNode.Auto();

            var entriesCount = splitNode.EntriesCount;
            var startIndex = splitNode.EntriesStartIndex;
            var endIndex = startIndex + entriesCount;

#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, entriesCount);
#endif

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB) = FindLargestEntriesPair(nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
#if ENABLE_ASSERTS
            Assert.IsTrue(indexA > -1 && indexB > -1);
#endif

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntriesCount;
            var newNode = new RTreeNode
            {
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            var invalidEntry = InvalidEntry<T>.Entry;

            nodeEntriesCount += MaxEntries;
            if (nodeEntries.Count < nodeEntriesCount)
                nodeEntries.AddRange(Enumerable.Repeat(invalidEntry, MaxEntries));

            nodeEntries[newEntriesStartIndex] = newNodeStartEntry;

            (nodeEntries[startIndex], nodeEntries[indexA]) = (nodeEntries[indexA], nodeEntries[startIndex]);

            splitNode.EntriesCount = 1;
            splitNode.Aabb = getAabbFunc.Invoke(nodeEntries[startIndex]);

            for (var i = 1; i <= MaxEntries; i++)
            {
                if (startIndex + i == indexB)
                    continue;

                var entry = i == MaxEntries ? newEntry : nodeEntries[startIndex + i];
                var entityAabb = getAabbFunc.Invoke(entry);

                var splitNodeAabb = GetNodeAabb(splitNode);
                var newNodeAabb = GetNodeAabb(newNode);

                var conjugatedAabbA = fix.AABBs_conjugate(splitNodeAabb, entityAabb);
                var conjugatedAabbB = fix.AABBs_conjugate(newNodeAabb, entityAabb);

                var isNewNodeTarget = IsSecondNodeTarget(splitNodeAabb, newNodeAabb, conjugatedAabbA, conjugatedAabbB);
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (isNewNodeTarget || targetNode.EntriesCount != i)
                    nodeEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[splitNode.EntriesStartIndex + i] = invalidEntry;

            for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[newNode.EntriesStartIndex + i] = invalidEntry;

#if ENABLE_ASSERTS
            Assert.IsTrue(nodeEntries.Take(nodeEntriesCount).Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries * 2);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private static int GetNodeIndexToInsert<T>(IReadOnlyList<T> nodeEntries, Func<T, AABB> getAabbFunc,
            int entriesStartIndex,
            int entriesEndIndex, bool isFillCase, in RTreeLeafEntry newEntry)
            where T : struct
        {
            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesStartIndex + MaxEntries; i++)
            {
                var nodeAabb = getAabbFunc(nodeEntries[i]);
                if (i >= entriesEndIndex)
                {
                    if (isFillCase)
                        nodeIndex = i;

                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, newEntry.Aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
        }

        private static void FillNodes<T>(IList<T> nodeEntries, Func<T, AABB> getAabbFunc, ref RTreeNode splitNode,
            ref RTreeNode newNode)
        {
            ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
            ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

            var sourceNodeEntriesCount = sourceNode.EntriesCount;
            var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
            var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

            var targetNodeAabb = GetNodeAabb(targetNode);
            var sourceNodeAabb = AABB.Empty;

            var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Empty, fix.MaxValue);
            for (var i = sourceNodeStartIndex; i < sourceNodeEndIndex; i++)
            {
                var entry = nodeEntries[i];
                var entryAabb = getAabbFunc.Invoke(entry);

                var conjugatedArea = GetConjugatedArea(targetNodeAabb, entryAabb);
                if (conjugatedArea > minArena)
                {
                    sourceNodeAabb = fix.AABBs_conjugate(sourceNodeAabb, entryAabb);
                    continue;
                }

                sourceNodeAabb = fix.AABBs_conjugate(sourceNodeAabb, sourceEntryAabb);

                (sourceEntryIndex, sourceEntryAabb, minArena) = (i, entryAabb, conjugatedArea);
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(sourceEntryIndex > -1);
#endif

            var targetEntryIndex = targetNode.EntriesStartIndex + targetNode.EntriesCount;
            nodeEntries[targetEntryIndex] = nodeEntries[sourceEntryIndex];

            targetNode.Aabb = fix.AABBs_conjugate(targetNode.Aabb, sourceEntryAabb);
            targetNode.EntriesCount++;

            if (sourceEntryIndex != sourceNodeEndIndex - 1)
                nodeEntries[sourceEntryIndex] = nodeEntries[sourceNodeEndIndex - 1];

            sourceNode.Aabb = sourceNodeAabb;
            sourceNode.EntriesCount--;
        }

        private static (int indexA, int indexB) FindLargestEntriesPair<T>(
            IReadOnlyList<T> nodeEntries,
            T newEntry,
            int startIndex,
            int endIndex,
            Func<T, AABB> getAabbFunc)
        {
            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = startIndex; i < endIndex; i++)
            {
                var aabbA = getAabbFunc.Invoke(nodeEntries[i]);
                fix conjugatedArea;

                for (var j = i + 1; j < endIndex; j++)
                {
                    var aabbB = getAabbFunc.Invoke(nodeEntries[j]);
                    if (aabbB == AABB.Empty)
                        continue;

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                var newEntryAabb = getAabbFunc.Invoke(newEntry);
                if (newEntryAabb == AABB.Empty)
                    continue;

                conjugatedArea = GetConjugatedArea(aabbA, newEntryAabb);
                if (conjugatedArea <= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            return (indexA, indexB);
        }

        private static bool IsSecondNodeTarget(in AABB nodeAabbA, in AABB nodeAabbB, in AABB conjugatedAabbA,
            in AABB conjugatedAabbB)
        {
            var (areaIncreaseA, deltaA) = GetAreaAndSizeIncrease(nodeAabbA, conjugatedAabbA);
            var (areaIncreaseB, deltaB) = GetAreaAndSizeIncrease(nodeAabbB, conjugatedAabbB);

            if (areaIncreaseA > areaIncreaseB == deltaA > deltaB)
                return areaIncreaseA > areaIncreaseB;

            if (areaIncreaseA == areaIncreaseB)
                return deltaA > deltaB;

            if (deltaA == deltaB)
                return areaIncreaseA > areaIncreaseB;

            return true;
        }

        private static (fix areaIncrease, fix sizeIncrease) GetAreaAndSizeIncrease(in AABB nodeAabb, in AABB conjugatedAabb)
        {
            var conjugatedArea = fix.AABB_area(conjugatedAabb);
            var areaIncrease = conjugatedArea - fix.AABB_area(nodeAabb);

            var size = nodeAabb.max - nodeAabb.min;
            var conjugatedSize = conjugatedAabb.max - conjugatedAabb.min;
            var sizeIncrease = fix.max(conjugatedSize.x - size.x, conjugatedSize.y - size.y);

            return (areaIncrease, sizeIncrease);
        }

        private static fix GetConjugatedArea(in AABB aabbA, in AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));
    }
}
