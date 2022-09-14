using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using JetBrains.Annotations;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public struct Node
    {
        public bool IsLeafNode;
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int RootNodesCount = 2;
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag /*, HasColliderTempTag*/> _filter;

        private readonly List<int> _rootNodes = new(MaxEntries);

        private readonly List<Node> _nodes = new(512);
        private readonly List<(AABB Aabb, EcsEntity Entity)> _leafEntries = new(512);

        private readonly (AABB Invalid, EcsEntity Null) _invalidLeafEntry = (AABB.Invalid, EcsEntity.Null);
        private readonly Node _invalidNodeEntry = new()
        {
            Aabb = AABB.Invalid,

            EntriesStartIndex = -1,
            EntriesCount = 0
        };

        public IEnumerable<int> RootNodes => _rootNodes;

        public IEnumerable<Node> GetNodes(IEnumerable<int> indices) =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : indices.Select(i => _nodes[i]);

        public void Run()
        {
            _rootNodes.Clear();
            _nodes.Clear();
            _leafEntries.Clear();

            if (_filter.IsEmpty())
                return;

            for (var i = 0; i < RootNodesCount; i++)
            {
                var nodeIndex = _nodes.Count;
                var leafNodesStartIndex = nodeIndex + 1;

                _rootNodes.Add(nodeIndex);

                _nodes.Add(new Node
                {
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = leafNodesStartIndex,
                    EntriesCount = 0
                });

                _nodes.AddRange(
                    Enumerable
                        .Repeat(new Node
                        {
                            IsLeafNode = true,
                            Aabb = AABB.Invalid,

                            EntriesStartIndex = -1,
                            EntriesCount = 0
                        }, MaxEntries)
                );
            }

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var aabb = entity.GetEntityColliderAABB(position);
                Insert(entity, aabb);
            }
        }

        private void Insert(EcsEntity entity, AABB aabb)
        {
            var (index, minArea) = (-1, fix.MaxValue);
            foreach (var i in _rootNodes)
            {
                var nodeAabb = _nodes[i].Aabb;
                if (nodeAabb == AABB.Invalid)
                {
                    index = i;
                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (index, minArea) = (i, conjugatedArea);
            }

            Assert.IsTrue(index > -1);

            var childNodes = ChooseLeaf(index, aabb, entity);

            _nodes[index] = childNodes[0];
            if (childNodes.Length == 1)
            {
                _nodes[index] = childNodes[0];
                return;
            }

            var newChildNode = childNodes[1];
            var newNodeIndex = _nodes.Count;

            _nodes.Add(newChildNode);

            if (_rootNodes.Count < MaxEntries)
            {
                _rootNodes.Add(newNodeIndex);
                return;
            }

            GrowTree(newNodeIndex);
        }

        private void GrowTree(int newChildNodeIndex)
        {
            // nodeIndex, false, _nodes, , , _invalidNodeEntry
            var (indexA, indexB) = FindLargestEntriesPair(_rootNodes, newChildNodeIndex, 0, _rootNodes.Count, GetRootNodeAabb);
            Assert.IsTrue(indexA > -1 && indexB > -1);

            AABB GetRootNodeAabb(int i) =>
                _nodes[_rootNodes[i]].Aabb;

            throw new NotImplementedException();
        }

        private Node[] ChooseLeaf(int nodeIndex, AABB aabb, EcsEntity entity)
        {
            var node = _nodes[nodeIndex];
            if (node.IsLeafNode)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitNode(nodeIndex, true, _leafEntries, (aabb, entity), GetLeafEntryAabb, _invalidLeafEntry);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;
                    _leafEntries.AddRange(Enumerable.Repeat(_invalidLeafEntry, MaxEntries));
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (Aabb: aabb, Entity: entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                node.EntriesCount++;

                return new[] { node };
            }

            var entriesCount = node.EntriesCount;
            var startIndex = node.EntriesStartIndex;
            var endIndex = startIndex + (entriesCount >= MinEntries ? entriesCount : MaxEntries);

            var (childNodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = startIndex; i < endIndex; i++)
            {
                var childNodeAabb = _nodes[i].Aabb;
                if (childNodeAabb == AABB.Invalid)
                {
                    childNodeIndex = i;
                    break;
                }

                var conjugatedArea = GetConjugatedArea(childNodeAabb, aabb);
                if (conjugatedArea >= minArea)
                    continue;

                (childNodeIndex, minArea) = (i, conjugatedArea);
            }

            Assert.IsTrue(childNodeIndex >= startIndex && startIndex <= endIndex);
            Assert.IsTrue(childNodeIndex - startIndex <= node.EntriesCount);

            if (childNodeIndex - startIndex == node.EntriesCount)
                node.EntriesCount++;

            var childNodes = ChooseLeaf(childNodeIndex, aabb, entity);

            _nodes[childNodeIndex] = childNodes[0];

            if (childNodes.Length == 1)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                return new[] { node };
            }

            var newChildNode = childNodes[1];

            if (entriesCount == MaxEntries)
                return SplitNode(nodeIndex, false, _nodes, newChildNode, GetNodeAabb, _invalidNodeEntry);

            _nodes[startIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return new[] { node };
        }

        private Node[] SplitNode<T>(int splitNodeIndex, bool isLeafNode, List<T> nodeEntries, T newEntry,
            Func<T, AABB> getAabbFunc, T invalidEntry)
        {
            var splitNode = _nodes[splitNodeIndex];

            var entriesCount = splitNode.EntriesCount;
            var startIndex = splitNode.EntriesStartIndex;
            var endIndex = startIndex + entriesCount;

            Assert.AreEqual(MaxEntries, entriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB) = FindLargestEntriesPair(nodeEntries, newEntry, startIndex, endIndex, getAabbFunc);
            Assert.IsTrue(indexA > -1 && indexB > -1);

            var newNodeStartEntry = indexB != endIndex ? nodeEntries[indexB] : newEntry;
            var newEntriesStartIndex = nodeEntries.Count;
            var newNode = new Node
            {
                IsLeafNode = isLeafNode,
                Aabb = getAabbFunc.Invoke(newNodeStartEntry),

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            nodeEntries.Add(newNodeStartEntry);
            nodeEntries.AddRange(Enumerable.Repeat(invalidEntry, MaxEntries - 1));

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

                var isNewNodeTarget = ChooseTargetNode(splitNodeAabb, newNodeAabb, conjugatedAabbA, conjugatedAabbB);
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (isNewNodeTarget || targetNode.EntriesCount != i)
                    nodeEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            if (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FulfillNodes(nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);

            return new[] { splitNode, newNode };
        }

        private static void FulfillNodes<T>(IList<T> nodeEntries, Func<T, AABB> getAabbFunc, ref Node splitNode,
            ref Node newNode)
        {
            ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
            ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

            var sourceNodeEntriesCount = sourceNode.EntriesCount;
            var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
            var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

            var targetNodeAabb = GetNodeAabb(targetNode);
            var sourceNodeAabb = AABB.Invalid;

            var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, AABB.Invalid, fix.MaxValue);
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

            Assert.IsTrue(sourceEntryIndex > -1);

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
            [CanBeNull] T newEntry,
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
                    var aabbB = getAabbFunc.Invoke(nodeEntries[i]);

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                if (newEntry == null)
                    continue;

                conjugatedArea = GetConjugatedArea(aabbA, getAabbFunc.Invoke(newEntry));
                if (conjugatedArea <= maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            return (indexA, indexB);
        }

        private static bool ChooseTargetNode(AABB splitNodeAabb, AABB newNodeAabb, AABB conjugatedAabbA, AABB conjugatedAabbB)
        {
            var (areaIncreaseA, deltaA) = GetAreaAndSizeIncrease(splitNodeAabb, conjugatedAabbA);
            var (areaIncreaseB, deltaB) = GetAreaAndSizeIncrease(newNodeAabb, conjugatedAabbB);

            if (areaIncreaseA > areaIncreaseB == deltaA > deltaB)
                return areaIncreaseA > areaIncreaseB;

            if (areaIncreaseA == areaIncreaseB)
                return deltaA > deltaB;

            if (deltaA == deltaB)
                return areaIncreaseA > areaIncreaseB;

            return true;
        }

        private static (fix areaIncrease, fix sizeIncrease) GetAreaAndSizeIncrease(AABB nodeAabb, AABB conjugatedAabb)
        {
            var conjugatedArea = fix.AABB_area(conjugatedAabb);
            var areaIncrease = conjugatedArea - fix.AABB_area(nodeAabb);

            var size = nodeAabb.max - nodeAabb.min;
            var conjugatedSize = conjugatedAabb.max - conjugatedAabb.min;
            var sizeIncrease = fix.max(conjugatedSize.x - size.x, conjugatedSize.y - size.y);

            return (areaIncrease, sizeIncrease);
        }

        private static AABB GetNodeAabb(Node node) =>
            node.Aabb;

        private static AABB GetLeafEntryAabb((AABB Aabb, EcsEntity Entity) leafEntry) =>
            leafEntry.Aabb;

        private static fix GetConjugatedArea(AABB aabbA, AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));
    }
}
