using System.Collections.Generic;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Game.Systems
{
    internal struct TreeNode
    {
        public int EntitiesIndex;
        public EcsEntity[] Entities;

        public bool IsSubdivided;
        public TreeNode[] ChildrenNodes;
    }

    public sealed class LevelEntitiesTreeSystem : IEcsRunSystem
    {
        private const int MaxTreeDepth = 16;
        private const int NodeDivisions = 2;

        private const int NodeCapacity = 8;
        private const int ChildrenNodesCount = 4;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders;

        private TreeNode _treeRootNode;
        private int _treeCurrentDepth = -1;

        private readonly EcsFilter<TransformComponent>.Exclude<LevelTileComponent> _filter;
        private readonly HashSet<EcsEntity> _clearedTiles = new();

        public void Run()
        {
            UpdateTree();

            if (_filter.IsEmpty())
                return;

            var levelTiles = _world.LevelTiles;

            foreach (var index in _filter)
            {
                ref var entity = ref _filter.GetEntity(index);
                ref var transformComponent = ref _filter.Get1(index);
                var tileCoordinate = levelTiles.ToTileCoordinate(transformComponent.WorldPosition);

                var levelTileEntity = levelTiles[tileCoordinate];
                ref var levelTileComponent = ref levelTileEntity.Get<LevelTileComponent>();

                if (!_clearedTiles.Contains(levelTileEntity))
                {
                    levelTileComponent.EntitiesHolder?.Clear();
                    _clearedTiles.Add(levelTileEntity);
                }

                if (levelTileComponent.EntitiesHolder != null)
                    levelTileComponent.EntitiesHolder.Add(entity);
                else
                    levelTileComponent.EntitiesHolder = new HashSet<EcsEntity> { entity };
            }

            _clearedTiles.Clear();
        }

        private void UpdateTree()
        {
            if (_colliders.IsEmpty())
                return;

            _treeCurrentDepth = -1;

            var (rootExtent, rootOffset) = GetRootNodeBounds();
            Debug.LogWarning($"rootExtent {rootExtent} rootOffset {rootOffset}");

            foreach (var index in _colliders)
            {
                ref var entity = ref _colliders.GetEntity(index);

                ref var transformComponent = ref _colliders.Get1(index);
                var position = transformComponent.WorldPosition;

                var (entityExtent, entityOffset) = entity.GetEntityColliderExtentAndOffset();
                entityOffset += position;

                var isInserted = Insert(ref _treeRootNode, rootExtent, rootOffset, entity, entityExtent, entityOffset);
                Assert.IsTrue(isInserted);
            }
        }

        private bool Insert(ref TreeNode node, fix2 nodeExtent, fix2 nodeOffset, EcsEntity ecsEntity, fix2 entityExtent,
            fix2 entityOffset, int depth = 0)
        {
            if (depth >= MaxTreeDepth)
                return false;

            if (math.any(entityOffset + entityExtent > nodeOffset + nodeExtent))
                return false;

            if (math.any(entityOffset - entityExtent < nodeOffset - nodeExtent))
                return false;

            if (_treeCurrentDepth < depth)
            {
                node.Entities ??= new EcsEntity[NodeCapacity];
                node.EntitiesIndex = 0;

                node.ChildrenNodes ??= new TreeNode[ChildrenNodesCount];
                node.IsSubdivided = false;

                _treeCurrentDepth = math.min(_treeCurrentDepth + 1, depth);
            }

            if (node.EntitiesIndex < node.Entities.Length && !node.IsSubdivided)
            {
                node.Entities[node.EntitiesIndex++] = ecsEntity;
                return true;
            }

            node.IsSubdivided = true;

            for (var y = 0; y < NodeDivisions; ++y)
            for (var x = 0; x < NodeDivisions; ++x)
            {
                var childNodeIndex = x + y * NodeDivisions;

                var childNodeExtent = nodeExtent / (fix) NodeDivisions;
                var childNodeOffset = nodeOffset - childNodeExtent + nodeExtent * new fix2(x, y);

                if (Insert(ref node.ChildrenNodes[childNodeIndex], childNodeExtent, childNodeOffset, ecsEntity, entityExtent,
                        entityOffset, depth + 1))
                    return true;
            }

            return false;
        }

        private (fix2 extent, fix2 offset) GetRootNodeBounds()
        {
            var (min, max) = (new fix2(fix.MaxValue), new fix2(fix.MinValue));

            foreach (var index in _colliders)
            {
                ref var entity = ref _colliders.GetEntity(index);

                ref var transformComponent = ref _colliders.Get1(index);
                var position = transformComponent.WorldPosition;

                var (extent, offset) = entity.GetEntityColliderExtentAndOffset();

                min = fix2.min(min, position + offset - extent);
                max = fix2.max(max, position + offset + extent);
            }

            var rootNodeExtent = (max - min) / (fix) 2;
            return (rootNodeExtent, max - rootNodeExtent);
        }
    }
}
