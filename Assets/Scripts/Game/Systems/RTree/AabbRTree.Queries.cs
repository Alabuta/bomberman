using System.Collections.Generic;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems.RTree
{
    public partial class AabbRTree
    {
        public void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result)
        {
            for (var subTreeIndex = 0; subTreeIndex < SubTreesCount; subTreeIndex++)
            {
                var subTreeHeight = GetSubTreeHeight(subTreeIndex);
                if (subTreeHeight < 1)
                    continue;

                var subTreeNodeLevelStartIndex = CalculateSubTreeNodeLevelStartIndex(
                    MaxEntries,
                    subTreeIndex,
                    subTreeHeight - 1,
                    _subTreesMaxHeight,
                    _perWorkerNodesContainerCapacity);

                var rootNodesEndIndex = subTreeNodeLevelStartIndex + _rootNodesCounts[subTreeIndex];
                for (var nodeIndex = subTreeNodeLevelStartIndex; nodeIndex < rootNodesEndIndex; nodeIndex++)
                    QueryNodesByLine(p0, p1, result, subTreeIndex, _rootNodesLevelIndices[subTreeIndex], nodeIndex);
            }
        }

        private void QueryNodesByLine(fix2 p0, fix2 p1,
            ICollection<RTreeLeafEntry> result,
            int subTreeIndex,
            int levelIndex, int nodeIndex)
        {
            var node = _nodesContainer[nodeIndex];
#if ENABLE_RTREE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, node.Aabb);
#endif
            if (!node.Aabb.CohenSutherlandLineClip(ref p0, ref p1))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex == 0)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                {
                    var leafEntry = _resultEntries[subTreeIndex * _perWorkerResultEntriesContainerCapacity + i];
                    if (!IntersectedByLine(p0, p1, in leafEntry.Aabb))
                        continue;

                    result.Add(leafEntry);
                }

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByLine(p0, p1, result, subTreeIndex, levelIndex - 1, i);

            bool IntersectedByLine(fix2 a, fix2 b, in AABB aabb) =>
                aabb.CohenSutherlandLineClip(ref a, ref b);
        }

        public void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result)
        {
            for (var subTreeIndex = 0; subTreeIndex < SubTreesCount; subTreeIndex++)
            {
                var subTreeHeight = GetSubTreeHeight(subTreeIndex);
                if (subTreeHeight < 1)
                    continue;

                var subTreeNodeLevelStartIndex = CalculateSubTreeNodeLevelStartIndex(
                    MaxEntries,
                    subTreeIndex,
                    subTreeHeight - 1,
                    _subTreesMaxHeight,
                    _perWorkerNodesContainerCapacity);

                var rootNodesEndIndex = subTreeNodeLevelStartIndex + _rootNodesCounts[subTreeIndex];
                for (var nodeIndex = subTreeNodeLevelStartIndex; nodeIndex < rootNodesEndIndex; nodeIndex++)
                    QueryNodesByAabb(aabb, result, subTreeIndex, _rootNodesLevelIndices[subTreeIndex], nodeIndex);
            }
        }

        private void QueryNodesByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result, int subTreeIndex, int levelIndex,
            int nodeIndex)
        {
            var node = _nodesContainer[nodeIndex];
#if ENABLE_RTREE_ASSERTS
            Assert.AreNotEqual(AABB.Empty, node.Aabb);
#endif
            if (!fix.is_AABB_overlapped_by_AABB(in aabb, in node.Aabb))
                return;

            var entriesStartIndex = node.EntriesStartIndex;
            var entriesEndIndex = node.EntriesStartIndex + node.EntriesCount;

            if (levelIndex == 0)
            {
                for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                    result.Add(_resultEntries[subTreeIndex * _perWorkerResultEntriesContainerCapacity + i]);

                return;
            }

            for (var i = entriesStartIndex; i < entriesEndIndex; i++)
                QueryNodesByAabb(aabb, result, subTreeIndex, levelIndex - 1, i);
        }
    }
}
