using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
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

        private readonly EcsFilter<TransformComponent, HasColliderTag, HasColliderTempTag> _filter;

        private readonly List<Node> _nodes = new(512);
        private readonly List<(AABB Aabb, EcsEntity Entity)> _leafEntries = new(512);

        private readonly int[] _rootNodes = new int[RootNodesCount];

        public IEnumerable<int> RootNodes => _rootNodes;

        public IEnumerable<Node> GetNodes(IEnumerable<int> indices) =>
            indices.Select(i => _nodes[i]);

        public IEnumerable<(AABB Aabb, EcsEntity Entity)> GetLeafEntries(IEnumerable<int> indices) =>
            indices.Select(i => _leafEntries[i]);

        public void Run()
        {
            _nodes.Clear();
            _leafEntries.Clear();

            if (_filter.IsEmpty())
                return;

            for (var i = 0; i < _rootNodes.Length; i++)
            {
                var nodeIndex = _nodes.Count;
                var leafNodesStartIndex = nodeIndex + 1;

                _rootNodes[i] = nodeIndex;

                _nodes.Add(new Node
                {
                    IsLeafNode = false,
                    Aabb = AABB.Invalid,

                    EntriesStartIndex = leafNodesStartIndex,
                    EntriesCount = 0
                });

                _nodes.AddRange(
                    Enumerable
                        .Range(0, MaxEntries)
                        .Select(_ => new Node
                        {
                            IsLeafNode = true,
                            Aabb = AABB.Invalid,

                            EntriesStartIndex = -1,
                            EntriesCount = 0
                        })
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

            var childNodes = ChooseLeaf(index, entity, aabb);
            if (childNodes.Length == 1)
            {
                _nodes[index] = childNodes[0];
                return;
            }

            // AdjustBounds
            // GrowTree
            throw new NotImplementedException();
        }

        private Node[] ChooseLeaf(int nodeIndex, EcsEntity entity, AABB aabb)
        {
            var node = _nodes[nodeIndex];
            if (node.IsLeafNode)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitLeafNode(nodeIndex, isLeafNode: true, entity, aabb);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;

                    _leafEntries.AddRange(
                        Enumerable
                            .Range(0, MaxEntries)
                            .Select(_ => (AABB.Invalid, EcsEntity.Null)));
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

            var childNodes = ChooseLeaf(childNodeIndex, entity, aabb);
            if (childNodes.Length == 1)
            {
                _nodes[childNodeIndex] = childNodes[0];

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);

                if (childNodeIndex - startIndex >= node.EntriesCount)
                    node.EntriesCount++;

                return new[] { node };
            }

            var newChildNode = childNodes[1];

            if (entriesCount == MaxEntries)
                // return SplitRootNode2(rootNode, b);
                throw new NotImplementedException();

            _nodes[startIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            return new[] { node };
        }

        private Node[] SplitLeafNode(int nodeIndexA, bool isLeafNode, EcsEntity entity, AABB aabb)
        {
            var splitNode = _nodes[nodeIndexA];

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

            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = startIndex; i < endIndex; i++)
            {
                var aabbA = _leafEntries[i].Aabb; // :TODO: leaf nodes vs root nodes
                fix conjugatedArea;

                for (var j = i + 1; j < endIndex; j++)
                {
                    var aabbB = _leafEntries[j].Aabb; // :TODO: leaf nodes vs root nodes

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, aabb);
                if (conjugatedArea < maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, endIndex, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexB > -1);

            var newNodeStartEntry = indexB != endIndex ? _leafEntries[indexB] : (Aabb: aabb, Entity: entity);
            var newEntriesStartIndex = _leafEntries.Count;
            var newNode = new Node
            {
                IsLeafNode = isLeafNode,
                Aabb = newNodeStartEntry.Aabb,

                EntriesStartIndex = newEntriesStartIndex,
                EntriesCount = 1
            };

            _leafEntries.Add(newNodeStartEntry);
            _leafEntries.AddRange(
                Enumerable
                    .Range(0, MaxEntries - 1)
                    .Select(_ => (AABB.Invalid, EcsEntity.Null)));

            (_leafEntries[startIndex], _leafEntries[indexA]) =
                (_leafEntries[indexA], _leafEntries[startIndex]);

            splitNode.EntriesCount = 1;
            splitNode.Aabb = _leafEntries[startIndex].Aabb;

            for (var i = 1; i <= MaxEntries; i++)
            {
                if (startIndex + i == indexB)
                    continue;

                var (entityAabb, ecsEntity) = i == MaxEntries ? (aabb, entity) : _leafEntries[startIndex + i];

                var conjugatedAabbA = fix.AABBs_conjugate(splitNode.Aabb, entityAabb);
                var conjugatedAreaA = fix.AABB_area(conjugatedAabbA);

                var conjugatedAabbB = fix.AABBs_conjugate(newNode.Aabb, entityAabb);
                var conjugatedAreaB = fix.AABB_area(conjugatedAabbB);

                var areaIncreaseA = conjugatedAreaA - fix.AABB_area(splitNode.Aabb);
                var areaIncreaseB = conjugatedAreaB - fix.AABB_area(newNode.Aabb);

                var isNewNodeTarget = areaIncreaseA > areaIncreaseB;
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (targetNode.EntriesCount == i && !isNewNodeTarget)
                {
                    targetNode.EntriesCount++;
                    continue;
                }

                _leafEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = (entityAabb, ecsEntity);

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);

            return new[] { splitNode, newNode };
        }
#if false
        private (TreeRootNode a, TreeRootNode b) SplitRootNode(TreeRootNode nodeA, BaseTreeNode childNode)
        {
            Assert.AreEqual(MaxEntries, nodeA.EntriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = 0; i < nodeA.EntriesCount; i++)
            {
                var (aabbA, _) = nodeA.Entries[i];
                fix conjugatedArea;

                for (var j = i + 1; j < nodeA.EntriesCount; j++)
                {
                    var (aabbB, _) = nodeA.Entries[j];

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea < maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, childNode.Aabb);
                if (conjugatedArea < maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, nodeA.EntriesCount, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexB > -1 && (indexA < nodeA.EntriesCount || indexB < nodeA.EntriesCount));

            var nodeBFirstEntry = indexB != nodeA.EntriesCount ? nodeA.Entries[indexB] : (childNode.Aabb, childNode);
            var nodeB = new TreeRootNode(_treeGeneration, MaxEntries)
            {
                EntriesCount = 1,
                Entries =
                {
                    [0] = nodeBFirstEntry
                },
                Aabb = nodeBFirstEntry.Aabb
            };

            (nodeA.Entries[0], nodeA.Entries[indexA]) = (nodeA.Entries[indexA], nodeA.Entries[0]);
            nodeA.EntriesCount = 1;
            nodeA.Aabb = nodeA.Entries[0].Aabb;

            for (var i = MaxEntries; i > 0; i--)
            {
                if (i == indexB)
                    continue;

                var (aabb, entity) = i == MaxEntries ? (childNode.Aabb, childNode) : nodeA.Entries[i];

                var conjugatedAabbA = fix.AABBs_conjugate(nodeA.Aabb, aabb);
                var conjugatedAreaA = fix.AABB_area(conjugatedAabbA);

                var conjugatedAabbB = fix.AABBs_conjugate(nodeB.Aabb, aabb);
                var conjugatedAreaB = fix.AABB_area(conjugatedAabbB);

                var areaIncreaseA = conjugatedAreaA - fix.AABB_area(nodeA.Aabb);
                var areaIncreaseB = conjugatedAreaB - fix.AABB_area(nodeB.Aabb);

                if (areaIncreaseA <= areaIncreaseB)
                {
                    nodeA.Entries[nodeA.EntriesCount++] = (aabb, entity);
                    nodeA.Aabb = conjugatedAabbA;
                }
                else
                {
                    nodeB.Entries[nodeB.EntriesCount++] = (aabb, entity);
                    nodeB.Aabb = conjugatedAabbB;
                }
            }

            Assert.IsTrue(nodeA.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(nodeB.EntriesCount is >= MinEntries and <= MaxEntries);

            return (nodeA, nodeB);
        }

        private (TreeLeafNode a, TreeLeafNode b) SplitLeafNode(TreeLeafNode nodeA, EcsEntity ecsEntity, AABB entityAabb)
        {
            Assert.AreEqual(MaxEntries, nodeA.EntriesCount);

            // Quadratic cost split
            // Search for pairs of entries A and B that would cause the largest area if placed in the same node
            // Put A and B entries in two different nodes
            // Then consider all other entries area increase relatively to two previous nodes' AABBs
            // Assign entry to the node with smaller AABB area increase
            // Repeat until all entries are assigned between two new nodes

            var (indexA, indexB, maxArena) = (-1, -1, fix.MinValue);
            for (var i = 0; i < nodeA.EntriesCount; i++)
            {
                var (aabbA, _) = nodeA.Entries[i];
                fix conjugatedArea;

                for (var j = i + 1; j < nodeA.EntriesCount; j++)
                {
                    var (aabbB, _) = nodeA.Entries[j];

                    conjugatedArea = GetConjugatedArea(aabbA, aabbB);
                    if (conjugatedArea <= maxArena)
                        continue;

                    (indexA, indexB, maxArena) = (i, j, conjugatedArea);
                }

                conjugatedArea = GetConjugatedArea(aabbA, entityAabb);
                if (conjugatedArea < maxArena)
                    continue;

                (indexA, indexB, maxArena) = (i, nodeA.EntriesCount, conjugatedArea);
            }

            Assert.IsTrue(indexA > -1 && indexB > -1 && (indexA < nodeA.EntriesCount || indexB < nodeA.EntriesCount));

            var nodeB = new TreeLeafNode(_treeGeneration, MaxEntries)
            {
                EntriesCount = 1,
                Entries =
                {
                    [0] = indexB != nodeA.EntriesCount ? nodeA.Entries[indexB] : (entityAabb, ecsEntity)
                }
            };
            nodeB.Aabb = nodeB.Entries[0].Aabb;

            (nodeA.Entries[0], nodeA.Entries[indexA]) = (nodeA.Entries[indexA], nodeA.Entries[0]);
            nodeA.EntriesCount = 1;
            nodeA.Aabb = nodeA.Entries[0].Aabb;

            for (var i = MaxEntries; i > 0; i--)
            {
                if (i == indexB)
                    continue;

                var (aabb, entity) = i == MaxEntries ? (entityAabb, ecsEntity) : nodeA.Entries[i];

                var conjugatedAabbA = fix.AABBs_conjugate(nodeA.Aabb, aabb);
                var conjugatedAreaA = fix.AABB_area(conjugatedAabbA);

                var conjugatedAabbB = fix.AABBs_conjugate(nodeB.Aabb, aabb);
                var conjugatedAreaB = fix.AABB_area(conjugatedAabbB);

                var areaIncreaseA = conjugatedAreaA - fix.AABB_area(nodeA.Aabb);
                var areaIncreaseB = conjugatedAreaB - fix.AABB_area(nodeB.Aabb);

                if (areaIncreaseA <= areaIncreaseB)
                {
                    nodeA.Entries[nodeA.EntriesCount++] = (aabb, entity);
                    nodeA.Aabb = conjugatedAabbA;
                }
                else
                {
                    nodeB.Entries[nodeB.EntriesCount++] = (aabb, entity);
                    nodeB.Aabb = conjugatedAabbB;
                }
            }

            Assert.IsTrue(nodeA.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(nodeB.EntriesCount is >= MinEntries and <= MaxEntries);

            return (nodeA, nodeB);
        }
#endif
        private static fix GetConjugatedArea(AABB aabbA, AABB aabbB) =>
            fix.AABB_area(fix.AABBs_conjugate(aabbA, aabbB));

        private void AdjustTreeBounds()
        {
            throw new NotImplementedException();
        }
    }
}
