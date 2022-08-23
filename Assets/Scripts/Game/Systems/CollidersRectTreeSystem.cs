using System;
using System.Linq;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public abstract class BaseTreeNode
    {
        public int Generation;
        public int EntriesCount;

        public AABB Aabb;

        public BaseTreeNode(int generation)
        {
            Generation = generation;
            EntriesCount = 0;
            Aabb = AABB.Invalid;
        }
    }

    public class TreeRootNode : BaseTreeNode
    {
        public readonly (AABB Aabb, BaseTreeNode ChildNode)[] Entries;

        public TreeRootNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Rect, BaseTreeNode ChildNode)[maxEntries];
        }
    }

    public class TreeLeafNode : BaseTreeNode
    {
        public readonly (AABB Aabb, EcsEntity Entity)[] Entries;

        public TreeLeafNode(int generation, int maxEntries) : base(generation)
        {
            Entries = new (AABB Aabb, EcsEntity Entity)[maxEntries];
        }
    }

    public sealed class CollidersRectTreeSystem : IEcsRunSystem
    {
        private const int MaxEntries = 8;
        private const int MinEntries = MaxEntries / 2;

        private int _treeGeneration;

        private BaseTreeNode _root;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _filter;

        private readonly int[] _intersectedMBRs = new int[MaxEntries];
        private readonly fix[] _areas = new fix[MaxEntries];

        public void Run()
        {
            if (_filter.IsEmpty())
                return;

            _root = new TreeRootNode(++_treeGeneration, MaxEntries);

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var aabb = entity.GetEntityColliderAABB(position);
                Insert(_root, entity, aabb);
            }
        }

        private void Insert(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            var splitNodes = ChooseLeaf(node, entity, aabb);

            // AdjustBounds
            // GrowTree
            throw new NotImplementedException();
        }

        private (BaseTreeNode a, BaseTreeNode b)? ChooseLeaf(BaseTreeNode node, EcsEntity entity, AABB aabb)
        {
            if (node is TreeLeafNode leafNode)
            {
                if (leafNode.EntriesCount >= leafNode.Entries.Length)
                    return SplitLeafNode(leafNode, entity, aabb);

                leafNode.Entries[leafNode.EntriesCount++] = (aabb, entity);
                leafNode.Aabb = fix.AABBs_conjugate(leafNode.Aabb, aabb);
                SortNodeEntities(leafNode);

                return null;
            }

            if (node is TreeRootNode rootNode)
            {
                /*Array.Fill(_intersectedMBRs, -1);
                Assert.IsTrue(rootNode.EntriesCount <= _intersectedMBRs.Length);

                for (var i = 0; i < rootNode.EntriesCount; i++)
                {
                    var (childNodeAabb, _) = rootNode.Entries[i];
                    if (!fix.is_AABB_overlapped_by_AABB(childNodeAabb, aabb))
                        continue;

                    _intersectedMBRs[i] = i;
                }

                Array.Sort(_intersectedMBRs);*/

                /*Array.Fill(_areas, fix.MaxValue);
                for (var i = 0; i < rootNode.EntriesCount; i++)
                {
                    var (childNodeAabb, _) = rootNode.Entries[i];
                    var newAabb = fix.AABBs_conjugate(childNodeAabb, aabb);
                    var newAabbArea = fix.AABB_area(newAabb);
                    _areas[i] = newAabbArea;
                }

                Array.Sort(_areas, rootNode.Entries);*/

                // SortNodeEntities(rootNode);

                var (index, minArea, minAreaIncrease) = (-1, fix.MaxValue, fix.MaxValue);
                for (var i = 0; i < rootNode.EntriesCount; i++)
                {
                    var (childNodeAabb, _) = rootNode.Entries[i];

                    var childNodeArea = fix.AABB_area(childNodeAabb);
                    var conjugatedAabb = fix.AABBs_conjugate(childNodeAabb, aabb);
                    var areaIncrease = fix.AABB_area(conjugatedAabb) - childNodeArea;

                    if (areaIncrease > minAreaIncrease)
                        continue;

                    if (areaIncrease == minAreaIncrease && childNodeArea >= minArea)
                        continue;

                    minArea = childNodeArea;
                    minAreaIncrease = areaIncrease;
                    index = i;
                }

                Assert.IsTrue(index > -1 && index < rootNode.EntriesCount);

                var (_, childNode) = rootNode.Entries[index];

                var splitNodes = ChooseLeaf(childNode, entity, aabb);
                if (splitNodes == null)
                {
                    node.Aabb = fix.AABBs_conjugate(node.Aabb, aabb);

                    SortNodeEntities(rootNode);

                    return null;
                }

                var (a, b) = splitNodes.Value;
                if (rootNode.EntriesCount < rootNode.Entries.Length)
                {
                    rootNode.Entries[index] = (a.Aabb, a);
                    rootNode.Entries[rootNode.EntriesCount++] = (b.Aabb, b);
                    rootNode.Aabb = fix.AABBs_conjugate(rootNode.Aabb, aabb);

                    SortNodeEntities(rootNode);

                    return null;
                }

                // linear split
            }

            throw new NotSupportedException();
        }

        private (TreeLeafNode a, TreeLeafNode b) SplitLeafNode(TreeLeafNode leafNodeA, EcsEntity ecsEntity, AABB entityAabb)
        {
            Assert.AreEqual(MaxEntries, leafNodeA.EntriesCount);

            var (areaA, indexA) = (fix.MinValue, -1);
            var (areaB, indexB) = (fix.MinValue, -1);

            /*for (var i = 0; i < leafNodeA.EntriesCount; i++)
            {
                var (aabb, _) = leafNodeA.Entries[i];
                var entityAabbArea = fix.AABB_area(aabb);

                if (entityAabbArea > areaA)
                {
                    (areaB, indexB) = (areaA, indexA);
                    (areaA, indexA) = (entityAabbArea, i);
                }
                else if (entityAabbArea > areaB)
                    (areaB, indexB) = (entityAabbArea, i);
            }

            var ecsEntityAabbArea = fix.AABB_area(entityAabb);
            if (ecsEntityAabbArea > areaA)
            {
                indexB = indexA;
                indexA = -1;
            }
            else if (ecsEntityAabbArea > areaB)
                indexB = -1;*/

            var leafNodeB = new TreeLeafNode(_treeGeneration, MaxEntries);
            leafNodeB.Entries[leafNodeB.EntriesCount] = indexB != -1 ? leafNodeA.Entries[indexB] : (entityAabb, ecsEntity);

            (leafNodeA.Entries[0], leafNodeA.Entries[indexA]) =
                (leafNodeA.Entries[indexA], leafNodeA.Entries[indexB != -1 ? indexB : 0]);

            /*var leafNodeB = new TreeLeafNode(_treeGeneration, MaxEntries);
            Assert.AreEqual(leafNodeB.EntriesCount, 0);
            leafNodeB.Entries[leafNodeB.EntriesCount] = (aabb, entity);
            leafNodeB.Aabb = fix.AABBs_conjugate(leafNodeB.Aabb, aabb);

            var moveCount = math.max(MinEntries, leafNodeA.EntriesCount / 2) - 1;
            Assert.IsTrue(moveCount > 0);
            while (moveCount > 0 && leafNodeA.EntriesCount > 0)
            {
                var (aabbA, entityA) = leafNodeA.Entries[--leafNodeA.EntriesCount];

                leafNodeB.Entries[++leafNodeB.EntriesCount] = (aabbA, entityA);
                leafNodeB.Aabb = fix.AABBs_conjugate(leafNodeB.Aabb, aabbA);

                moveCount--;
            }*/

            Assert.IsTrue(leafNodeA.EntriesCount > 0);
            Assert.IsTrue(leafNodeB.EntriesCount > 0);

            leafNodeA.Aabb = GetNodeAABB(leafNodeA);

            /*SortNodeEntities(leafNodeA);
            SortNodeEntities(leafNodeB);*/

            var isLeafBSmaller = fix.AABB_area(leafNodeB.Aabb) < fix.AABB_area(leafNodeA.Aabb);
            return isLeafBSmaller ? (leafNodeB, leafNodeA) : (leafNodeA, leafNodeB);
        }

        private static void SortNodeEntities(BaseTreeNode node)
        {
            switch (node)
            {
                case TreeLeafNode leafNode:
                    Array.Sort(leafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;

                case TreeRootNode nonLeafNode:
                    Array.Sort(nonLeafNode.Entries, (a, b) => fix.AABB_area(a.Aabb).CompareTo(fix.AABB_area(b.Aabb)));
                    break;
            }
        }

        private static AABB GetNodeAABB(BaseTreeNode node)
        {
            return node switch
            {
                TreeLeafNode leaf => leaf.Entries.Aggregate(AABB.Invalid, (a, p) => fix.AABBs_conjugate(a, p.Aabb)),
                TreeRootNode nonLeaf => nonLeaf.Entries.Aggregate(AABB.Invalid, (a, p) => fix.AABBs_conjugate(a, p.Aabb)),
                _ => AABB.Invalid
            };
        }

        private void AdjustTreeBounds()
        {
            throw new NotImplementedException();
        }
    }
}
