using System;
using Game.Systems;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEngine;

namespace Gizmos
{
    public class RTreeDrawer : MonoBehaviour
    {
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private CollidersRectTreeSystem _rTree;
        private Color[] _colors;

        public void SetRTree(CollidersRectTreeSystem rTree)
        {
            _rTree = rTree;

            _colors = new[]
            {
                Color.cyan,
                Color.magenta,
                Color.yellow,
                Color.red
            };
        }

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;
            UnityEngine.Gizmos.color = Color.black;

            const int treeDepth = 0;

            foreach (var rootNode in _rTree.RootNodes)
            {
                var aabb = rootNode.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));

                DrawChildren(rootNode, treeDepth + 1);
            }
        }

        private void DrawChildren(BaseTreeNode rootNode, int treeDepth)
        {
            switch (rootNode)
            {
                case TreeLeafNode treeLeafNode:
                    DrawChildren(treeLeafNode, treeDepth);
                    break;

                case TreeRootNode treeRootNode:
                    DrawChildren(treeRootNode, treeDepth);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(rootNode));
            }
        }

        private void DrawChildren(TreeRootNode rootNode, int treeDepth)
        {
            UnityEngine.Gizmos.color = _colors[treeDepth];

            foreach (var entry in rootNode.Entries)
            {
                var (aabb, node) = entry;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));

                DrawChildren(node, treeDepth + 1);
            }
        }

        private static void DrawChildren(TreeLeafNode rootNode, int _)
        {
            UnityEngine.Gizmos.color = new Color(0, 0, 1, 0.4f);

            foreach (var entry in rootNode.Entries)
            {
                var (aabb, _) = entry;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));
            }
        }
    }
}
