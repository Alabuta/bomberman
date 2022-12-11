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
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Game.Systems
{
    internal static class InvalidEntry<T> where T : struct
    {
        public static T Entry;
    }

    public class AabbRTree : IRTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private const int RootNodeMinEntries = 2;

        private readonly int _leafEntriesMaxCount;

        private readonly List<NativeList<Node>> _nodes;

        private NativeList<RTreeLeafEntry> _leafEntries;

        private NativeList<RTreeLeafEntry> _entriesNativeList;

        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;
        private static readonly Func<Node, AABB> GetNodeAabb = node => node.Aabb;

        private int _rootNodesIndex = -1;

        public int TreeHeight => _rootNodesIndex + 1;

        public IEnumerable<Node> RootNodes =>
            TreeHeight > 0
                ? _nodes[_rootNodesIndex].ToArray().TakeWhile(n => n.Aabb != AABB.Empty)
                : Enumerable.Empty<Node>();

        public IEnumerable<Node> GetNodes(int levelIndex, IEnumerable<int> indices) =>
            levelIndex < TreeHeight
                ? indices.Select(i => _nodes[_rootNodesIndex - levelIndex][i])
                : Enumerable.Empty<Node>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(IEnumerable<int> indices) =>
            _leafEntries.Length != 0 ? indices.Select(i => _leafEntries[i]) : Enumerable.Empty<RTreeLeafEntry>();

        private bool IsLevelRoot(int nodeLevelIndex) =>
            nodeLevelIndex == _rootNodesIndex;

        public AabbRTree()
        {
            const int entriesCount = 993; // level setup to 65x63 blocks plus the hero

            // var maxTreeHeight = (int) math.ceil(math.log10((double) entriesCount / MinEntries) / math.log10(MaxEntries));
            var maxTreeHeight = (int) math.floor(math.log10((double) (entriesCount + 1) / 2) / math.log10(MinEntries));

            const double ratio = (double) MaxEntries / MinEntries;

            _leafEntriesMaxCount =
                (int) math.pow(MinEntries, math.ceil(math.log10(entriesCount * ratio) / math.log10(MinEntries)));
            _leafEntries = new NativeList<RTreeLeafEntry>(_leafEntriesMaxCount, Allocator.Persistent);

            InvalidEntry<Node>.Entry = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            _nodes = Enumerable.Range(0, maxTreeHeight - 1)
                .Select(levelIndex =>
                {
                    var capacity = (int) (math.pow(MinEntries, maxTreeHeight - levelIndex) * ratio);
                    // var capacity = (int) math.pow(MaxEntries, maxTreeHeight - levelIndex);
                    return new NativeList<Node>(capacity, Allocator.Persistent);
                })
                .Append(new NativeList<Node>(MaxEntries, Allocator.Persistent))
                .ToList();

            _entriesNativeList = new NativeList<RTreeLeafEntry>(_leafEntriesMaxCount, Allocator.Persistent);
        }

        public void Dispose()
        {
            _entriesNativeList.Dispose();

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

            if (filter.IsEmpty())
                return;

            _rootNodesIndex = -1;
            _leafEntries.Clear();

            foreach (var nodes in _nodes)
                nodes.Clear();

            var entitiesCount = filter.GetEntitiesCount();
#if !ENABLE_ASSERTS
            Assert.IsTrue(entitiesCount <= _leafEntriesMaxCount);
            Assert.IsTrue(math.log10((float) entitiesCount / MaxEntries) / math.log10(MaxEntries) <= _nodes.Count);
#endif

            /*Profiling.RTreeNativeArrayFill.Begin();
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

            var subSize = (totalAabb.max - totalAabb.min) / new fix2(2);*/

            ++_rootNodesIndex;

            var rootNodes = _nodes[_rootNodesIndex];
            for (var i = 0; i < MaxEntries; i++)
            {
                // rootNodes.AddNoResize(InvalidEntry<Node>.Entry);
                /*var min = totalAabb.min + subSize * new fix2(i % 2, i / 2);
                var max = min + subSize;*/
                rootNodes.AddNoResize(new Node
                {
                    Aabb = AABB.Empty, // new AABB(min, max),
                    EntriesCount = 0,
                    EntriesStartIndex = -1
                });
            }

            Profiling.RTreeInsert.Begin();
            for (var index = 0; index < entitiesCount; index++)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);
                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                var entry = new RTreeLeafEntry(aabb, index);
                // var entry = _entriesNativeList[index];
                Insert(entry);
            }

            Profiling.RTreeInsert.End();
        }

        private void Insert(in RTreeLeafEntry entry)
        {
            const int rootEntriesStartIndex = 0;

#if !ENABLE_ASSERTS
            Assert.IsTrue(TreeHeight > 0);
#endif
            var rootNodes = _nodes[_rootNodesIndex];
            var rootNodesCount = rootNodes.Length;
#if !ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            var nonEmptyNodesCount = 0;
            for (var i = 0; i < rootNodesCount; i++)
                nonEmptyNodesCount += rootNodes[i].Aabb != AABB.Empty ? 1 : 0;
#if !ENABLE_ASSERTS
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.ToArray().Take(rootNodesCount).Count(n => n.EntriesCount > 0));
#endif

            var nodeIndex = GetNodeIndexToInsert(rootNodes, GetNodeAabb, rootEntriesStartIndex,
                rootEntriesStartIndex + nonEmptyNodesCount, nonEmptyNodesCount < RootNodeMinEntries, entry.Aabb);
#if !ENABLE_ASSERTS
            Assert.IsTrue(nodeIndex > -1);
#endif

            var targetNode = rootNodes[nodeIndex];
            var extraNode = ChooseLeaf(ref targetNode, _rootNodesIndex, entry);
#if !ENABLE_ASSERTS
            Assert.AreNotEqual(targetNode.Aabb, AABB.Empty);
            Assert.AreNotEqual(targetNode.EntriesCount, 0);
            Assert.AreNotEqual(targetNode.EntriesStartIndex, -1);
#endif

            rootNodes[nodeIndex] = targetNode;

            if (!extraNode.HasValue)
                return;

#if !ENABLE_ASSERTS
            Assert.AreNotEqual(extraNode.Value.Aabb, AABB.Empty);
            Assert.AreNotEqual(extraNode.Value.EntriesCount, 0);
            Assert.AreNotEqual(extraNode.Value.EntriesStartIndex, -1);
#endif

            var candidateNodeIndex = nodeIndex + 1;
            for (; candidateNodeIndex < MaxEntries; candidateNodeIndex++)
                if (rootNodes[candidateNodeIndex].Aabb == AABB.Empty)
                    break;

            if (candidateNodeIndex != MaxEntries && candidateNodeIndex < rootNodesCount)
            {
                rootNodes[candidateNodeIndex] = extraNode.Value;
                return;
            }

#if !ENABLE_ASSERTS
            Assert.IsTrue(rootNodes
                .ToArray()
                .Take(rootNodesCount)
                .All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));
#endif

            GrowTree(extraNode.Value);
        }

        private Node? ChooseLeaf(ref Node node, int nodeLevelIndex, in RTreeLeafEntry entry)
        {
            var isLeafLevel = nodeLevelIndex == 0;
            if (isLeafLevel)
            {
                using var _ = Profiling.RTreeLeafNodesUpdate.Auto();

                if (node.EntriesCount == MaxEntries)
                    return SplitNode(ref node, ref _leafEntries, entry, GetLeafEntryAabb);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Length;

                    for (var i = 0; i < MaxEntries; i++)
                        _leafEntries.AddNoResize(InvalidEntry<RTreeLeafEntry>.Entry);
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = entry;

                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                node.EntriesCount++;

                return null;
            }

            using var __ = Profiling.RTreeNodesUpdate.Auto();

            var entriesCount = node.EntriesCount;
#if !ENABLE_ASSERTS
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries || IsLevelRoot(nodeLevelIndex) &&
                entriesCount is >= RootNodeMinEntries and <= MaxEntries);
#endif

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
#if !ENABLE_ASSERTS
            Assert.IsTrue(entriesStartIndex > -1);
#endif

            var childNodeLevelIndex = nodeLevelIndex - 1;
            var childLevelNodes = _nodes[childNodeLevelIndex];
            var childNodeIndex =
                GetNodeIndexToInsert(childLevelNodes, GetNodeAabb, entriesStartIndex, entriesEndIndex, false, entry.Aabb);
#if !ENABLE_ASSERTS
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);
#endif

            var targetChildNode = childLevelNodes[childNodeIndex];
            var extraChildNode = ChooseLeaf(ref targetChildNode, childNodeLevelIndex, entry);

            childLevelNodes[childNodeIndex] = targetChildNode;

            if (!extraChildNode.HasValue)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
                return null;
            }

            var newChildNode = extraChildNode.Value;
            if (entriesCount == MaxEntries)
                return SplitNode(ref node, ref childLevelNodes, newChildNode, GetNodeAabb);

            childLevelNodes[entriesStartIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, entry.Aabb);
            node.EntriesCount++;

#if !ENABLE_ASSERTS
            Assert.IsTrue(node.EntriesCount is >= MinEntries and <= MaxEntries || IsLevelRoot(nodeLevelIndex) &&
                node.EntriesCount is >= RootNodeMinEntries and <= MaxEntries);
#endif
            return null;
        }

        private void GrowTree(in Node newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodes = _nodes[_rootNodesIndex];
            var rootNodesCount = rootNodes.Length;
#if !ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = 0,
                EntriesCount = rootNodesCount
            };

            var newRootNodeB = SplitNode(ref newRootNodeA, ref rootNodes, newEntry, GetNodeAabb);

            ++_rootNodesIndex;

            Assert.IsFalse(_nodes.Count < TreeHeight);
            var newRootNodes = _nodes[_rootNodesIndex];
            Assert.AreEqual(0, newRootNodes.Length);

            newRootNodes.AddNoResize(newRootNodeA);
            newRootNodes.AddNoResize(newRootNodeB);

            for (var i = 2; i < MaxEntries; i++)
                newRootNodes.AddNoResize(InvalidEntry<Node>.Entry);

#if !ENABLE_ASSERTS
            Assert.IsTrue(_nodes[_rootNodesIndex]
                .ToArray()
                .Take(rootNodesCount)
                .Count(n => n.Aabb != AABB.Empty) >= RootNodeMinEntries);

            Assert.IsTrue(_nodes[_rootNodesIndex]
                .ToArray()
                .Take(rootNodesCount)
                .Count(n => n.EntriesStartIndex != -1) >= RootNodeMinEntries);

            Assert.IsTrue(_nodes[_rootNodesIndex]
                .ToArray()
                .Take(rootNodesCount)
                .Count(n => n.EntriesCount > 0) >= RootNodeMinEntries);
#endif
        }

        private static Node SplitNode<T>(ref Node splitNode, ref NativeList<T> nodeEntries, in T newEntry,
            Func<T, AABB> getAabbFunc) where T : unmanaged
        {
            using var _ = Profiling.RTreeSplitNode.Auto();

            var entriesCount = splitNode.EntriesCount;
            var startIndex = splitNode.EntriesStartIndex;
            var endIndex = startIndex + entriesCount;

#if !ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, entriesCount);
#endif

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB) = FindLargestEntriesPair(nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
#if !ENABLE_ASSERTS
            Assert.IsTrue(indexA > -1 && indexB > -1);
#endif

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntries.Length;
            var newNode = new Node
            {
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            var invalidEntry = InvalidEntry<T>.Entry;

            for (var i = 0; i < MaxEntries; i++)
                nodeEntries.AddNoResize(invalidEntry);

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

            var nativeArray = nodeEntries.AsArray();
            while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(ref nativeArray, getAabbFunc, ref splitNode, ref newNode);

            for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[splitNode.EntriesStartIndex + i] = invalidEntry;

            for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[newNode.EntriesStartIndex + i] = invalidEntry;

#if !ENABLE_ASSERTS
            Assert.IsTrue(nodeEntries
                .ToArray()
                .Take(nodeEntries.Length)
                .Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries * 2);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private static int GetNodeIndexToInsert<T>(NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc, int entriesStartIndex,
            int entriesEndIndex, bool isFillCase, in AABB aabb)
            where T : struct
        {
            if (isFillCase)
                return entriesEndIndex;

            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesStartIndex + MaxEntries; i++)
            {
                if (i >= entriesEndIndex)
                    break;

                var nodeAabb = getAabbFunc(nodeEntries[i]);
                var conjugatedArea = GetConjugatedArea(nodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
        }

        private static void FillNodes<T>(ref NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc, ref Node splitNode,
            ref Node newNode)
            where T : struct
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

#if !ENABLE_ASSERTS
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
            in NativeArray<T> nodeEntries,
            T newEntry,
            int startIndex,
            int endIndex,
            Func<T, AABB> getAabbFunc)
            where T : struct
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
