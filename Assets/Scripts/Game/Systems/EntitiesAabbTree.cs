using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public struct Node
    {
        public AABB Aabb;

        public int EntriesStartIndex;
        public int EntriesCount;
    }

    public sealed class EntitiesAabbTree
    {
        private const int MaxEntries = 4;
        private const int MinEntries = MaxEntries / 2;

        private readonly List<List<Node>> _nodes = new();
        private readonly List<(AABB Aabb, EcsEntity Entity)> _leafEntries = new(512);

        private readonly Node _invalidNodeEntry = new()
        {
            Aabb = AABB.Empty,
            EntriesStartIndex = -1,
            EntriesCount = 0
        };

        private readonly (AABB Aabb, EcsEntity Entity) _invalidLeafEntry = new(AABB.Empty, EcsEntity.Null);

        public int TreeHeight => _nodes.Count;

        public IEnumerable<Node> RootNodes =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : _nodes[0].TakeWhile(n => n.Aabb != AABB.Empty);

        public IEnumerable<Node> GetNodes(int levelIndex, IEnumerable<int> indices) =>
            _nodes.Count == 0 ? Enumerable.Empty<Node>() : indices.Select(i => _nodes[levelIndex][i]);

        public IEnumerable<(AABB Aabb, EcsEntity Entity)> GetLeafEntries(IEnumerable<int> indices) =>
            _leafEntries.Count == 0 ? Enumerable.Empty<(AABB Aabb, EcsEntity Entity)>() : indices.Select(i => _leafEntries[i]);

        public void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep)
        {
            _nodes.Clear();
            _leafEntries.Clear();

            if (filter.IsEmpty())
                return;

            _nodes.Add(
                Enumerable
                    .Repeat(new Node
                    {
                        Aabb = AABB.Empty,

                        EntriesStartIndex = -1,
                        EntriesCount = 0
                    }, MaxEntries)
                    .ToList());

            foreach (var index in filter)
            {
                ref var entity = ref filter.GetEntity(index);

                ref var transformComponent = ref filter.Get1(index);
                var currentPosition = transformComponent.WorldPosition;

                var hasPrevFrameDataComponent = entity.Has<PrevFrameDataComponent>();
                ref var prevFrameDataComponent = ref entity.Get<PrevFrameDataComponent>();

                var lastPosition = hasPrevFrameDataComponent
                    ? prevFrameDataComponent.LastWorldPosition
                    : transformComponent.WorldPosition;

                var position = lastPosition + (currentPosition - lastPosition) * simulationSubStep;

                var aabb = entity.GetEntityColliderAABB(position);
                Insert(entity, aabb);
            }

            CheckLevel();
        }

        public void QueryByLine(fix2 p0, fix2 p1, ICollection<(AABB Aabb, EcsEntity Entity)> result)
        {
            const int levelIndex = 0;

            var topLevelNodes = _nodes[levelIndex];
            for (var nodeIndex = 0; nodeIndex < topLevelNodes.Count; nodeIndex++)
                QueryNodesByLine(p0, p1, result, levelIndex, nodeIndex);
        }

        private void QueryNodesByLine(
            fix2 p0, fix2 p1,
            ICollection<(AABB Aabb, EcsEntity Entity)> result,
            int levelIndex, int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!node.Aabb.CohenSutherlandLineClip(ref p0, ref p1))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex + 1 == TreeHeight)
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
                QueryNodesByLine(p0, p1, result, levelIndex + 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result)
        {
            const int levelIndex = 0;

            var topLevelNodes = _nodes[levelIndex];
            for (var nodeIndex = 0; nodeIndex < topLevelNodes.Count; nodeIndex++)
                QueryNodesByAabb(aabb, result, levelIndex, nodeIndex);
        }

        private void QueryNodesByAabb(AABB aabb, ICollection<(AABB Aabb, EcsEntity Entity)> result, int levelIndex,
            int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];
            if (!fix.is_AABB_overlapped_by_AABB(aabb, node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex + 1 == TreeHeight)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                    result.Add(_leafEntries[i]);

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByAabb(aabb, result, levelIndex + 1, i);
        }

        private void Insert(EcsEntity entity, AABB aabb)
        {
            const int rootLevelIndex = 0;
            const int rootEntriesStartIndex = 0;

            var rootNodes = _nodes[rootLevelIndex];
            Assert.AreEqual(MaxEntries, rootNodes.Count);

            var nonEmptyNodesCount = rootNodes.Count(n => n.Aabb != AABB.Empty);
            Assert.AreEqual(nonEmptyNodesCount, rootNodes.Count(n => n.EntriesCount > 0));

            var nodeIndex = GetNodeIndexToInsert(rootLevelIndex, rootEntriesStartIndex,
                rootEntriesStartIndex + nonEmptyNodesCount, nonEmptyNodesCount < MinEntries, aabb);
            Assert.IsTrue(nodeIndex > -1);

            CheckRefs();
            CheckLevel();

            var expectedAabb = fix.AABBs_conjugate(rootNodes[nodeIndex].Aabb, aabb);

            var (a, b) = ChooseLeaf(rootLevelIndex, nodeIndex, aabb, entity);
            Assert.IsTrue(a.HasValue);
            Assert.AreNotEqual(AABB.Empty, a.Value.Aabb);

            rootNodes[nodeIndex] = a.Value;

            Assert.AreEqual(CalculateNodeAabb(rootNodes[nodeIndex], 0), rootNodes[nodeIndex].Aabb);

            if (!b.HasValue)
            {
                Assert.AreEqual(expectedAabb, rootNodes[nodeIndex].Aabb);
                CheckRefs();
                CheckLevel();
                return;
            }

            Assert.AreNotEqual(AABB.Empty, b.Value.Aabb);

            var candidateNodeIndex = rootNodes.FindIndex(n => n.Aabb == AABB.Empty);
            if (candidateNodeIndex != -1)
            {
                rootNodes[candidateNodeIndex] = b.Value;
                Assert.AreEqual(CalculateNodeAabb(rootNodes[candidateNodeIndex], 0), rootNodes[candidateNodeIndex].Aabb);
                CheckRefs();
                CheckLevel();
                return;
            }

            Assert.IsTrue(rootNodes.All(n => n.Aabb != AABB.Empty && n.EntriesStartIndex != -1 && n.EntriesCount > 0));

            CheckLevel();
            CheckRefs();
            GrowTree(b.Value);
            CheckLevel();
            CheckRefs();
        }

        private void CheckRefs(int levelIndex = 0)
        {
            return;

            CheckDuplicateRefs(levelIndex);
            CheckDeadRefs(levelIndex);
        }

        private void CheckDeadRefs(int levelIndex)
        {
            while (levelIndex < TreeHeight)
            {
                var levelNodes = _nodes[levelIndex];

                foreach (var levelNode in levelNodes)
                {
                    Assert.AreEqual(levelNode.EntriesStartIndex == -1, levelNode.EntriesCount == 0);
                    Assert.AreEqual(levelNode.EntriesStartIndex == -1, levelNode.Aabb == AABB.Empty);
                    Assert.AreEqual(levelNode.EntriesCount == 0, levelNode.Aabb == AABB.Empty);

                    if (levelNode.EntriesStartIndex == -1)
                        continue;

                    Assert.IsTrue(levelNode.EntriesCount <= MaxEntries);

                    var indices = Enumerable.Range(levelNode.EntriesStartIndex + levelNode.EntriesCount,
                        MaxEntries - levelNode.EntriesCount);

                    foreach (var index in indices)
                    {
                        if (levelIndex + 1 == TreeHeight)
                        {
                            Assert.AreEqual(AABB.Empty, _leafEntries[index].Aabb);
                            Assert.AreEqual(EcsEntity.Null, _leafEntries[index].Entity);
                        }
                        else
                        {
                            Assert.AreEqual(AABB.Empty, _nodes[levelIndex + 1][index].Aabb);
                            Assert.AreEqual(-1, _nodes[levelIndex + 1][index].EntriesStartIndex);
                            Assert.AreEqual(0, _nodes[levelIndex + 1][index].EntriesCount);
                        }
                    }
                }

                levelIndex++;
            }
        }

        private void CheckDuplicateRefs(int levelIndex = 0)
        {
            while (levelIndex < TreeHeight)
            {
                var set = new SortedSet<int>();
                var levelNodes = _nodes[levelIndex];

                foreach (var levelNode in levelNodes)
                {
                    Assert.AreEqual(levelNode.EntriesStartIndex == -1, levelNode.EntriesCount == 0);
                    Assert.AreEqual(levelNode.EntriesStartIndex == -1, levelNode.Aabb == AABB.Empty);
                    Assert.AreEqual(levelNode.EntriesCount == 0, levelNode.Aabb == AABB.Empty);

                    if (levelNode.EntriesStartIndex == -1)
                        continue;

                    Assert.IsTrue(levelNode.EntriesCount <= MaxEntries);

                    var indices = Enumerable.Range(levelNode.EntriesStartIndex, levelNode.EntriesCount)
                        .ToArray();

                    foreach (var index in indices)
                    {
                        Assert.IsFalse(set.Contains(index));
                        set.Add(index);
                    }
                }

                levelIndex++;
            }
        }

        private (Node? a, Node? b) ChooseLeaf(int nodeLevelIndex, int nodeIndex, AABB aabb, EcsEntity entity)
        {
            CheckLevel(nodeLevelIndex);
            var levelNodes = _nodes[nodeLevelIndex];
            var node = levelNodes[nodeIndex];

            var isLeafLevel = nodeLevelIndex + 1 == TreeHeight;
            if (isLeafLevel)
            {
                if (node.EntriesCount == MaxEntries)
                    return SplitNode(nodeLevelIndex, nodeIndex, _leafEntries, (aabb, entity), GetLeafEntryAabb,
                        _invalidLeafEntry);

                if (node.EntriesStartIndex == -1)
                {
                    node.EntriesStartIndex = _leafEntries.Count;
                    _leafEntries.AddRange(Enumerable.Repeat(_invalidLeafEntry, MaxEntries));
                }

                _leafEntries[node.EntriesStartIndex + node.EntriesCount] = (Aabb: aabb, Entity: entity);

                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                node.EntriesCount++;

                Assert.AreEqual(CalculateNodeAabb(node, nodeLevelIndex), node.Aabb);
                return (a: node, b: null);
            }

            var entriesCount = node.EntriesCount;
            Assert.IsTrue(entriesCount is >= MinEntries and <= MaxEntries);

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = entriesStartIndex + entriesCount;
            Assert.IsTrue(entriesStartIndex > -1);

            var childNodeLevelIndex = nodeLevelIndex + 1;
            var childNodeIndex = GetNodeIndexToInsert(childNodeLevelIndex, entriesStartIndex, entriesEndIndex, false, aabb);
            Assert.IsTrue(childNodeIndex >= entriesStartIndex);
            Assert.IsTrue(childNodeIndex < entriesEndIndex);

            var node0 = CalculateNodeAabb(node, nodeLevelIndex);
            Assert.AreEqual(node0, node.Aabb);
            var (a, b) = ChooseLeaf(childNodeLevelIndex, childNodeIndex, aabb, entity);
            Assert.IsTrue(a.HasValue);
            Assert.AreEqual(CalculateNodeAabb(a.Value, childNodeLevelIndex), a.Value.Aabb);

            var childLevelNodes = _nodes[childNodeLevelIndex];
            childLevelNodes[childNodeIndex] = a.Value;

            if (!b.HasValue)
            {
                node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
                Assert.AreEqual(CalculateNodeAabb(node, nodeLevelIndex), node.Aabb);
                return (a: node, b: null);
            }

            var newChildNode = b.Value;
            Assert.AreEqual(CalculateNodeAabb(newChildNode, childNodeLevelIndex), b.Value.Aabb);
            if (entriesCount == MaxEntries)
                return SplitNode(nodeLevelIndex, nodeIndex, childLevelNodes, newChildNode, GetNodeAabb, _invalidNodeEntry);

            childLevelNodes[entriesStartIndex + node.EntriesCount] = newChildNode;

            node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);
            node.EntriesCount++;

            Assert.AreEqual(CalculateNodeAabb(node, nodeLevelIndex), node.Aabb);
            return (a: node, b: null);
        }

        private void GrowTree(Node newEntry)
        {
            CheckRefs();

            var topLevelNodes = _nodes[0];
            Assert.AreEqual(MaxEntries, topLevelNodes.Count);

            var (childNodeIndexA, childNodeIndexB) =
                FindLargestEntriesPair(topLevelNodes, newEntry, 0, MaxEntries, GetNodeAabb);
            Assert.IsTrue(childNodeIndexA > -1 && childNodeIndexB > -1 && childNodeIndexA < childNodeIndexB);

            var childNodesStartIndexA = 0;
            var childNodesStartIndexB = topLevelNodes.Count;
            var childNodesEndIndexB = topLevelNodes.Count + MaxEntries;
            Assert.AreEqual(MaxEntries, childNodesStartIndexB);

            var childNodeA = topLevelNodes[childNodeIndexA];
            var childNodeB = childNodeIndexB == MaxEntries ? newEntry : topLevelNodes[childNodeIndexB];

            topLevelNodes.AddRange(Enumerable.Repeat(_invalidNodeEntry, MaxEntries));

            var aabbA = childNodeA.Aabb;
            var aabbB = childNodeB.Aabb;

            var newRootNodeA = new Node
            {
                Aabb = AABB.Empty,

                EntriesStartIndex = childNodesStartIndexA,
                EntriesCount = 0
            };

            var newRootNodeB = new Node
            {
                Aabb = aabbB,

                EntriesStartIndex = childNodesStartIndexB,
                EntriesCount = 1
            };

            topLevelNodes[childNodesStartIndexB++] = childNodeB;

            for (var rootNodeIndexC = 0; rootNodeIndexC <= MaxEntries; rootNodeIndexC++)
            {
                if (rootNodeIndexC == childNodeIndexA)
                {
                    topLevelNodes[childNodesStartIndexA] = childNodeA;
                    newRootNodeA.Aabb = fix.AABBs_conjugate(newRootNodeA.Aabb, childNodeA.Aabb);

                    ++childNodesStartIndexA;
                    ++newRootNodeA.EntriesCount;

                    continue;
                }

                if (rootNodeIndexC == childNodeIndexB)
                    continue;

                var nodeC = rootNodeIndexC == MaxEntries ? newEntry : topLevelNodes[rootNodeIndexC];
                var aabbC = nodeC.Aabb;

                var conjugatedAabbA = fix.AABBs_conjugate(aabbA, aabbC);
                var conjugatedAabbB = fix.AABBs_conjugate(aabbB, aabbC);

                var isSecondNodeTarget = IsSecondNodeTarget(aabbA, aabbB, conjugatedAabbA, conjugatedAabbB);
                if (isSecondNodeTarget && childNodesStartIndexB < childNodesEndIndexB)
                {
                    topLevelNodes[childNodesStartIndexB++] = nodeC;

                    newRootNodeB.Aabb = fix.AABBs_conjugate(newRootNodeB.Aabb, aabbC);
                    ++newRootNodeB.EntriesCount;
                }
                else
                {
                    topLevelNodes[childNodesStartIndexA++] = nodeC;

                    newRootNodeA.Aabb = fix.AABBs_conjugate(newRootNodeA.Aabb, aabbC);
                    ++newRootNodeA.EntriesCount;
                }
            }

            /*for (var i = childNodesStartIndexA; i < MaxEntries; i++)
                topLevelNodes[i] = _invalidNodeEntry;*/

            while (newRootNodeA.EntriesCount < MinEntries || newRootNodeB.EntriesCount < MinEntries)
                FillNodes(topLevelNodes, GetNodeAabb, ref newRootNodeA, ref newRootNodeB);

            for (var i = newRootNodeA.EntriesCount; i < MaxEntries; i++)
                topLevelNodes[newRootNodeA.EntriesStartIndex + i] = _invalidNodeEntry;

            for (var i = newRootNodeB.EntriesCount; i < MaxEntries; i++)
                topLevelNodes[newRootNodeB.EntriesStartIndex + i] = _invalidNodeEntry;

            Assert.IsTrue(newRootNodeA.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newRootNodeB.EntriesCount is >= MinEntries and <= MaxEntries);

            var newRootNodes = new List<Node>(new[] { newRootNodeA, newRootNodeB });
            newRootNodes.AddRange(Enumerable
                .Repeat(new Node
                {
                    Aabb = AABB.Empty,

                    EntriesStartIndex = -1,
                    EntriesCount = 0
                }, MaxEntries - newRootNodes.Count));

            _nodes.Insert(0, newRootNodes);

            Assert.IsTrue(_nodes[0].Count(n => n.Aabb != AABB.Empty) >= MinEntries);
            Assert.IsTrue(_nodes[0].Count(n => n.EntriesStartIndex != -1) >= MinEntries);
            Assert.IsTrue(_nodes[0].Count(n => n.EntriesCount > 0) >= MinEntries);

            CheckRefs();
            CheckLevel();
        }

        private void CheckLevel(int levelIndex = 0)
        {
            return;

            var nodes = _nodes[levelIndex];

            for (var i = 0; i < nodes.Count; i++)
            {
                Assert.AreEqual(nodes[i].EntriesStartIndex == -1, nodes[i].EntriesCount == 0);
                Assert.AreEqual(nodes[i].EntriesStartIndex == -1, nodes[i].Aabb == AABB.Empty);

                if (nodes[i].EntriesStartIndex < 0 || nodes[i].EntriesCount < 1)
                    continue;

                var aabb = CalculateLeafNodesAABB(levelIndex, i);
                Assert.AreEqual(aabb, nodes[i].Aabb);
            }
        }

        private AABB CalculateLeafNodesAABB(int levelIndex, int nodeIndex)
        {
            var node = _nodes[levelIndex][nodeIndex];

            var startIndex = node.EntriesStartIndex;
            var endIndex = startIndex + node.EntriesCount;

            if (node.Aabb == AABB.Empty || startIndex == -1 || endIndex == 0)
                return AABB.Empty;

            if (levelIndex + 1 == TreeHeight)
            {
                var leafNodesAabb = AABB.Empty;

                for (var i = startIndex; i < endIndex; i++)
                {
                    Assert.AreNotEqual(EcsEntity.Null, _leafEntries[i].Entity);
                    Assert.AreNotEqual(AABB.Empty, _leafEntries[i].Aabb);
                    leafNodesAabb = fix.AABBs_conjugate(leafNodesAabb, _leafEntries[i].Aabb);
                }

                return leafNodesAabb;
            }

            var aabb = AABB.Empty;

            for (var i = startIndex; i < endIndex; i++)
            {
                Assert.AreNotEqual(AABB.Empty, _nodes[levelIndex + 1][i].Aabb);
                aabb = fix.AABBs_conjugate(aabb, CalculateLeafNodesAABB(levelIndex + 1, i));
            }

            return aabb;
        }

        private AABB CalculateNodeAabb(Node node, int levelIndex)
        {
            var startIndex = node.EntriesStartIndex;
            var endIndex = startIndex + node.EntriesCount;

            if (node.Aabb == AABB.Empty || startIndex == -1 || endIndex == 0)
                return AABB.Empty;

            var aabb = AABB.Empty;

            if (levelIndex + 1 == TreeHeight)
            {
                for (var i = startIndex; i < endIndex; i++)
                {
                    Assert.AreNotEqual(EcsEntity.Null, _leafEntries[i].Entity);
                    Assert.AreNotEqual(AABB.Empty, _leafEntries[i].Aabb);
                    aabb = fix.AABBs_conjugate(aabb, _leafEntries[i].Aabb);
                }

                return aabb;
            }

            var childNodes = _nodes[levelIndex + 1];
            for (var i = startIndex; i < endIndex; i++)
            {
                var childNodeAabb = CalculateNodeAabb(childNodes[i], levelIndex + 1);
                aabb = fix.AABBs_conjugate(aabb, childNodeAabb);
            }

            return aabb;
        }

        private int GetNodeIndexToInsert(int nodeLevel, int entriesStartIndex, int entriesEndIndex, bool isFillCase,
            AABB newEntryAabb)
        {
            var levelNodes = _nodes[nodeLevel];

            var (nodeIndex, minArea) = (-1, fix.MaxValue);
            for (var i = entriesStartIndex; i < entriesStartIndex + MaxEntries; i++)
            {
                var nodeAabb = levelNodes[i].Aabb;
                if (i >= entriesEndIndex)
                {
                    if (isFillCase)
                        nodeIndex = i;

                    break;
                }

                var conjugatedArea = GetConjugatedArea(nodeAabb, newEntryAabb);
                if (conjugatedArea >= minArea)
                    continue;

                (nodeIndex, minArea) = (i, conjugatedArea);
            }

            return nodeIndex;
        }

        private (Node a, Node b) SplitNode<T>(int splitNodeLevel, int splitNodeIndex, List<T> nodeEntries,
            T newEntry, Func<T, AABB> getAabbFunc, T invalidEntry)
        {
            var levelNodes = _nodes[splitNodeLevel];
            var splitNode = levelNodes[splitNodeIndex];

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

                var isNewNodeTarget = IsSecondNodeTarget(splitNodeAabb, newNodeAabb, conjugatedAabbA, conjugatedAabbB);
                ref var targetNode = ref isNewNodeTarget ? ref newNode : ref splitNode;

                if (isNewNodeTarget || targetNode.EntriesCount != i)
                    nodeEntries[targetNode.EntriesStartIndex + targetNode.EntriesCount] = entry;

                targetNode.EntriesCount++;
                targetNode.Aabb = isNewNodeTarget ? conjugatedAabbB : conjugatedAabbA;
            }

            /*for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[startIndex + i] = invalidEntry;*/

            while (splitNode.EntriesCount < MinEntries || newNode.EntriesCount < MinEntries)
                FillNodes(nodeEntries, getAabbFunc, ref splitNode, ref newNode);

            for (var i = splitNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[splitNode.EntriesStartIndex + i] = invalidEntry;

            for (var i = newNode.EntriesCount; i < MaxEntries; i++)
                nodeEntries[newNode.EntriesStartIndex + i] = invalidEntry;

            CalculateNodeAabb(splitNode, splitNodeLevel);
            CalculateNodeAabb(newNode, splitNodeLevel);

            Assert.IsTrue(nodeEntries.Count(n => getAabbFunc(n) != AABB.Empty) >= MinEntries * 2);

            Assert.IsTrue(splitNode.EntriesCount is >= MinEntries and <= MaxEntries);
            Assert.IsTrue(newNode.EntriesCount is >= MinEntries and <= MaxEntries);

            Assert.AreEqual(CalculateNodeAabb(splitNode, splitNodeLevel), splitNode.Aabb);
            Assert.AreEqual(CalculateNodeAabb(newNode, splitNodeLevel), newNode.Aabb);

            return (a: splitNode, b: newNode);
        }

        private static void FillNodes<T>(IList<T> nodeEntries, Func<T, AABB> getAabbFunc, ref Node splitNode,
            ref Node newNode)
        {
            ref var sourceNode = ref splitNode.EntriesCount < MinEntries ? ref newNode : ref splitNode;
            ref var targetNode = ref splitNode.EntriesCount < MinEntries ? ref splitNode : ref newNode;

            var sourceNodeEntriesCount = sourceNode.EntriesCount;
            var sourceNodeStartIndex = sourceNode.EntriesStartIndex;
            var sourceNodeEndIndex = sourceNodeStartIndex + sourceNodeEntriesCount;

            var targetNodeAabb = GetNodeAabb(targetNode);
            var sourceNodeAabb = AABB.Empty;

            var (sourceEntryIndex, sourceEntryAabb, minArena) = (-1, Invalid: AABB.Empty, fix.MaxValue);
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

        private static bool IsSecondNodeTarget(AABB nodeAabbA, AABB nodeAabbB, AABB conjugatedAabbA, AABB conjugatedAabbB)
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
