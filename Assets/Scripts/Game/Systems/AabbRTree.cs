using System;
using System.Collections.Generic;
using System.Linq;
using App;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using Unity.Collections;

namespace Game.Systems
{
    internal static class InvalidEntry<T> where T : struct
    {
        public static T Entry;
        public static NativeArray<T> Range;
        public static NativeArray<T> SubRange;
    }

    public class AabbRTree : IDisposable, IRTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        public const int MaxTreeHeight = 12; // log base MinEntries of 4096 objects

        private readonly List<NativeList<Node>> _nodes;
        private NativeList<int> _nodesCountByLevel = new(MaxTreeHeight, Allocator.Persistent);

        private NativeList<RTreeLeafEntry> _leafEntries = new(1024, Allocator.Persistent);
        private int _leafEntriesCount;

        private static readonly Func<RTreeLeafEntry, AABB> GetLeafEntryAabb = leafEntry => leafEntry.Aabb;
        private static readonly Func<Node, AABB> GetNodeAabb = node => node.Aabb;

        private int RootNodesIndex => _nodesCountByLevel.Length - 1;

        public int TreeHeight => _nodesCountByLevel.Length;

        public IEnumerable<Node> RootNodes =>
            TreeHeight > 0
                ? _nodes[RootNodesIndex].ToArray().TakeWhile(n => n.Aabb != AABB.Empty)
                : Enumerable.Empty<Node>();

        public IEnumerable<Node> GetNodes(int levelIndex, IEnumerable<int> indices) =>
            levelIndex < TreeHeight
                ? indices.Select(i => _nodes[RootNodesIndex - levelIndex][i])
                : Enumerable.Empty<Node>();

        public IEnumerable<RTreeLeafEntry> GetLeafEntries(IEnumerable<int> indices) =>
            _leafEntries.Length != 0 ? indices.Select(i => _leafEntries[i]) : Enumerable.Empty<RTreeLeafEntry>();

        public AabbRTree()
        {
            InvalidEntry<Node>.Entry = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = -1,
                EntriesCount = 0
            };

            InvalidEntry<Node>.Range = new NativeArray<Node>(
                Enumerable
                    .Repeat(InvalidEntry<Node>.Entry, MaxEntries)
                    .ToArray(), Allocator.Persistent);

            InvalidEntry<Node>.SubRange = new NativeArray<Node>(
                Enumerable
                    .Repeat(InvalidEntry<Node>.Entry, MaxEntries - MinEntries)
                    .ToArray(), Allocator.Persistent);

            InvalidEntry<RTreeLeafEntry>.Entry = new RTreeLeafEntry(AABB.Empty, -1);

            InvalidEntry<RTreeLeafEntry>.Range = new NativeArray<RTreeLeafEntry>(
                Enumerable
                    .Repeat(InvalidEntry<RTreeLeafEntry>.Entry, MaxEntries)
                    .ToArray(), Allocator.Persistent);

            InvalidEntry<RTreeLeafEntry>.SubRange = new NativeArray<RTreeLeafEntry>(
                Enumerable
                    .Repeat(InvalidEntry<RTreeLeafEntry>.Entry, MaxEntries - MinEntries)
                    .ToArray(), Allocator.Persistent);

            _nodes = Enumerable.Range(0, MaxTreeHeight)
                .Select(levelIndex =>
                {
                    /*var entriesCount = (int) math.pow(MaxEntries, MaxTreeHeight - levelIndex);
                    var nativeList = new NativeList<Node>(entriesCount, Allocator.Persistent);
                    nativeList.AddRange(InvalidEntry<Node>.Entry);
                    return nativeList;*/

                    var nativeList = new NativeList<Node>(4096, Allocator.Persistent);
                    nativeList.AddRange(InvalidEntry<Node>.Range);
                    return nativeList;
                })
                .ToList();

            /*Debug.LogWarning(Marshal.SizeOf(typeof(IntPtr))); // 8
            Debug.LogWarning(Marshal.SizeOf(typeof(fix2))); // 16
            // Debug.LogWarning(Marshal.SizeOf(typeof(RTreeLeafEntry))); // 264
            Debug.LogWarning(Marshal.SizeOf(typeof(RTreeLeafEntry))); // 40
            Debug.LogWarning(Marshal.SizeOf(typeof(Node))); // 40
            Debug.LogWarning(Marshal.SizeOf(typeof(AABB))); // 32
            Debug.LogWarning(Marshal.SizeOf(typeof(EcsEntity))); // 232
            Debug.LogWarning(_nodes.Select(n => n.Length).Sum()); // 240
            Debug.LogWarning(Marshal.SizeOf(typeof(Node)) * _nodes.Select(n => n.Length).Sum());
            Debug.LogWarning(_leafEntries.Length);
            Debug.LogWarning((Marshal.SizeOf(typeof(AABB)) + Marshal.SizeOf(typeof(EcsEntity))) * _leafEntries.Length);
            Debug.LogWarning(Marshal.SizeOf(typeof(NativeArray<RTreeLeafEntry>)));
            Debug.LogWarning(Marshal.SizeOf(typeof(NativeArray<Node>))); // 80
            Debug.LogWarning(Marshal.SizeOf(typeof(NativeArray<RTreeLeafEntry>))); // 80
            Debug.LogWarning(Marshal.SizeOf(typeof(NativeList<RTreeLeafEntry>))); // 72
            Debug.LogWarning(Marshal.SizeOf(typeof(NativeList<RTreeLeafEntry>))); // 72*/
        }

        public void Dispose()
        {
            foreach (var nodes in _nodes)
                nodes.Dispose();

            _nodesCountByLevel.Dispose();
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

            _nodesCountByLevel.Clear();
            _leafEntriesCount = 0;

            if (filter.IsEmpty())
                return;

            var entitiesCount = filter.GetEntitiesCount();

            _nodesCountByLevel.Add(MaxEntries);

            var rootNodes = _nodes[RootNodesIndex];
            // rootNodes.AddRangeNoResize(InvalidEntry<Node>.Range);
            for (var i = 0; i < MaxEntries; i++)
            {
                rootNodes[i] = InvalidEntry<Node>.Entry;
                // rootNodes.Add(InvalidEntry<Node>.Entry);
            }

            Profiling.RTreeInsert.Begin();
            for (var index = 0; index < entitiesCount; index++)
            {
                ref var entity = ref filter.GetEntity(index);
                ref var transformComponent = ref filter.Get1(index);
                var aabb = entity.GetEntityColliderAABB(transformComponent.WorldPosition);
                var entry = new RTreeLeafEntry(aabb, index);
                Insert(entry);
            }

            Profiling.RTreeInsert.End();
        }

        private void Insert(in RTreeLeafEntry entry)
        {
            var rootLevelIndex = RootNodesIndex;
            const int rootEntriesStartIndex = 0;

#if ENABLE_ASSERTS
            Assert.IsTrue(TreeHeight > 0);
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

        private Node? ChooseLeaf(ref Node node, int nodeLevelIndex, in RTreeLeafEntry entry)
        {
            var isLeafLevel = nodeLevelIndex == 0;
            if (isLeafLevel)
            {
                using var _ = Profiling.RTreeLeafNodesUpdate.Auto();

                if (node.EntriesCount == MaxEntries)
                {
                    Profiling.RTreeB.Begin();
                    var splitNode = SplitNode(ref node, ref _leafEntries, _leafEntriesCount, entry, GetLeafEntryAabb);
                    Profiling.RTreeB.End();
                    _leafEntriesCount += MaxEntries;
                    return splitNode;
                }

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntriesCount;
                    _leafEntriesCount += MaxEntries;

                    if (_leafEntries.Length < _leafEntriesCount)
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
            {
                var splitNode = SplitNode(ref node, ref childLevelNodes, _nodesCountByLevel[childNodeLevelIndex], newChildNode,
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

        private void GrowTree(in Node newEntry)
        {
            using var _ = Profiling.RTreeGrow.Auto();

            var rootNodes = _nodes[RootNodesIndex];
            var rootNodesCount = _nodesCountByLevel[RootNodesIndex];
            // rootNodes[0].Aabb = AABB.Empty;
#if ENABLE_ASSERTS
            Assert.AreEqual(MaxEntries, rootNodesCount);
#endif

            var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,
                EntriesStartIndex = 0,
                EntriesCount = rootNodesCount
            };
            /*_tempNodeEntry.EntriesStartIndex = 0;
            _tempNodeEntry.EntriesCount = rootNodesCount;*/

            var newRootNodeB = SplitNode(ref newRootNodeA, ref rootNodes, rootNodesCount, newEntry, GetNodeAabb);

            _nodesCountByLevel[RootNodesIndex] += MaxEntries;
            _nodesCountByLevel.Add(MaxEntries);

            if (_nodes.Count < TreeHeight)
            {
                var newRootNodes = new NativeList<Node>(MaxEntries, Allocator.Persistent);
                newRootNodes.Add(newRootNodeA);
                newRootNodes.Add(newRootNodeB);
                newRootNodes.AddRange(InvalidEntry<Node>.SubRange);
                _nodes.Add(newRootNodes);
            }
            else
            {
                var newRootNodes = _nodes[RootNodesIndex];
                if (newRootNodes.Length < MaxEntries)
                {
                    newRootNodes.Add(newRootNodeA);
                    newRootNodes.Add(newRootNodeB);
                    newRootNodes.AddRange(InvalidEntry<Node>.SubRange);
                }
                else
                {
                    newRootNodes[0] = newRootNodeA;
                    newRootNodes[1] = newRootNodeB;

                    for (var i = 2; i < MaxEntries; i++)
                        newRootNodes[i] = InvalidEntry<Node>.Entry;
                }
            }

#if ENABLE_ASSERTS
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.Aabb != AABB.Empty) >= MinEntries);
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.EntriesStartIndex != -1) >= MinEntries);
            Assert.IsTrue(_nodes[RootNodesIndex].Take(rootNodesCount).Count(n => n.EntriesCount > 0) >= MinEntries);
#endif
        }

        private static Node SplitNode<T>(ref Node splitNode, ref NativeList<T> nodeEntries, int nodeEntriesCount,
            in T newEntry, Func<T, AABB> getAabbFunc) where T : unmanaged
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
            var newNode = new Node
            {
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            var invalidEntry = InvalidEntry<T>.Entry;

            nodeEntriesCount += MaxEntries;
            if (nodeEntries.Length < nodeEntriesCount)
                nodeEntries.AddRange(InvalidEntry<T>.Range);

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

#if ENABLE_ASSERTS
            Assert.IsTrue(nodeEntries.Take(nodeEntriesCount).Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries * 2);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);
#endif

            return newNode;
        }

        private static int GetNodeIndexToInsert<T>(NativeArray<T> nodeEntries, Func<T, AABB> getAabbFunc, int entriesStartIndex,
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
