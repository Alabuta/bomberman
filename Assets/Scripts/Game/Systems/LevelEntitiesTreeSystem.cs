using System.Collections.Generic;
using Game.Components;
using Game.Components.Entities;
using Game.Components.Tags;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    internal struct TreeNode /*<T> where T : struct*/
    {
        // public readonly List<(EcsEntity entity, TransformComponent transform, T collider)> Entities;
        public List<EcsEntity> Entities;
        public TreeNode[] ChildrenNodes;
    }

    public sealed class LevelEntitiesTreeSystem : IEcsRunSystem
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private readonly EcsFilter<TransformComponent, HasColliderTag> _colliders;

        private readonly EcsFilter<TransformComponent>.Exclude<LevelTileComponent> _filter;
        private readonly HashSet<EcsEntity> _clearedTiles = new();

        private readonly List<TreeNode> _tree = new();

        const int NodeCapacity = 4;

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

            var (rootExtent, rootOffset) = GetRootNodeBounds();
            Debug.LogWarning($"rootExtent {rootExtent} rootOffset {rootOffset}");

            var rootNode = new TreeNode();

            var entities = new List<EcsEntity>();
            var children = new TreeNode[4];

            var nodeSize = rootExtent;
            var nodeExtent = rootExtent / (fix) 2;

            foreach (var index in _colliders)
            {
                Insert(ref rootNode, rootExtent, rootOffset, index);

                ref var entity = ref _colliders.GetEntity(index);

                ref var transformComponent = ref _colliders.Get1(index);
                var position = transformComponent.WorldPosition;

                var (entityExtent, entityOffset) = entity.GetEntityColliderExtentAndOffset();
                entityOffset += position;

                if (math.any(entityExtent > nodeExtent))
                {
                    entities.Add(entity);
                }
                else
                {
                    const int divisions = 2;

                    for (var y = 0; y < divisions; y++)
                    {
                        for (var x = 0; x < divisions; x++)
                        {
                            var nodeMin = rootOffset + nodeSize * new fix2(x - 1, y - 1);
                            if (math.any(entityOffset - entityExtent < nodeMin))
                                continue;

                            var nodeMax = nodeMin + nodeSize;
                            if (math.any(entityOffset + entityExtent > nodeMax))
                                continue;

                            var nodeIndex = x + y * divisions;
                            /*children[nodeIndex] ??= new TreeNode();
                            children[nodeIndex]..Add();*/
                        }
                    }
                }

                _tree.Add(new TreeNode
                {
                    Entities = entities,
                    ChildrenNodes = children
                });
            }
        }

        private void Insert(ref TreeNode node, fix2 nodeExtent, fix2 rootOffset, int index, int depth = 0)
        {
            ref var entity = ref _colliders.GetEntity(index);

            ref var transformComponent = ref _colliders.Get1(index);
            var position = transformComponent.WorldPosition;

            var (entityExtent, entityOffset) = entity.GetEntityColliderExtentAndOffset();
            entityOffset += position;

            if (math.any(entityExtent > nodeExtent))
            {
                node.Entities.Add(entity);
            }
            else
            {
                const int divisions = 2;

                for (var y = 0; y < divisions; y++)
                {
                    for (var x = 0; x < divisions; x++)
                    {
                        var nodeMin = rootOffset + nodeSize * new fix2(x - 1, y - 1);
                        if (math.any(entityOffset - entityExtent < nodeMin))
                            continue;

                        var nodeMax = nodeMin + nodeSize;
                        if (math.any(entityOffset + entityExtent > nodeMax))
                            continue;

                        var nodeIndex = x + y * divisions;
                        /*children[nodeIndex] ??= new TreeNode();
                        children[nodeIndex]..Add();*/
                    }
                }
            }
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

            var rootNodeExtent = (max + min) / (fix) 2;
            return (rootNodeExtent, max - rootNodeExtent);
        }
    }
}
