using System.Collections.Generic;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine.Assertions;

namespace Game.Systems
{
    public struct QuadTreeNode
    {
        public int Generation;

        public int EntitiesIndex;
        public readonly EcsEntity[] Entities;

        public bool IsSubdivided;
        public readonly QuadTreeNode[] ChildrenNodes;

        public QuadTreeNode(int generation, int nodeCapacity, int childrenNodesCount)
        {
            Generation = generation;

            EntitiesIndex = 0;
            Entities = new EcsEntity[nodeCapacity];

            IsSubdivided = false;
            ChildrenNodes = new QuadTreeNode[childrenNodesCount];
        }
    }

    public sealed class EntitiesTreeSystem : IEcsRunSystem
    {
        private const int MaxTreeDepth = 16;
        private const int NodeDivisions = 2;

        private const int NodeCapacity = 16;
        private const int ChildrenNodesCount = 4;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _filter;

        private int _treeGeneration;
        private QuadTreeNode _treeRootNode = new(0, NodeCapacity, ChildrenNodesCount);

        public QuadTreeNode TreeRootNode => _treeRootNode;

        public void Run()
        {
            UpdateTree();
        }

        public IReadOnlyList<EcsEntity> QueryEntities(fix2 offset, fix2 extent)
        {
            return null;
        }

        private void UpdateTree()
        {
            if (_filter.IsEmpty())
                return;

            ++_treeGeneration;

            _treeRootNode.EntitiesIndex = 0;
            _treeRootNode.IsSubdivided = false;

            var (rootExtent, rootOffset) = GetRootNodeBounds();

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);

                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                var isInserted = Insert(ref _treeRootNode, rootExtent, rootOffset, entity, position);
                Assert.IsTrue(isInserted);
            }
        }

        private bool Insert(ref QuadTreeNode node, fix2 nodeExtent, fix2 nodeOffset, EcsEntity entity, fix2 position,
            int depth = 0)
        {
            if (depth >= MaxTreeDepth)
                return false;

            if (!fix.is_point_inside_box(position, nodeExtent, nodeOffset))
                return false;

            if (node.EntitiesIndex < node.Entities.Length && !node.IsSubdivided)
            {
                node.Entities[node.EntitiesIndex++] = entity;
                return true;
            }

            node.IsSubdivided = true;

            var childNodeExtent = nodeExtent / (fix) NodeDivisions;

            for (var y = 0; y < NodeDivisions; ++y)
            for (var x = 0; x < NodeDivisions; ++x)
            {
                var childNodeOffset = nodeOffset - childNodeExtent + nodeExtent * new fix2(x, y);

                var childNodeIndex = x + y * NodeDivisions;
                ref var childNode = ref node.ChildrenNodes[childNodeIndex];

                if (childNode.Generation < _treeGeneration)
                {
                    if (childNode.Entities == null || childNode.ChildrenNodes == null)
                        childNode = new QuadTreeNode(_treeGeneration, NodeCapacity, ChildrenNodesCount);

                    else
                    {
                        childNode.Generation = _treeGeneration;
                        childNode.EntitiesIndex = 0;
                        childNode.IsSubdivided = false;
                    }
                }

                if (Insert(ref childNode, childNodeExtent, childNodeOffset, entity, position, depth + 1))
                    return true;
            }

            /*if (node2.EntitiesIndex >= node2.Entities.Length)
                return false;

            node2.Entities[node2.EntitiesIndex++] = entity;
            return true;*/
            return false;
        }

        private (fix2 extent, fix2 offset) GetRootNodeBounds()
        {
            var (min, max) = (new fix2(fix.MaxValue), new fix2(fix.MinValue));

            foreach (var index in _filter)
            {
                ref var transformComponent = ref _filter.Get1(index);
                var position = transformComponent.WorldPosition;

                min = fix2.min(min, position);
                max = fix2.max(max, position);
            }

            var rootNodeExtent = (max - min) / (fix) 2;
            return (rootNodeExtent, max - rootNodeExtent);
        }
    }
}
