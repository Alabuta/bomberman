using System.Collections.Generic;
using System.Linq;
using Game.Systems;
using Game.Systems.RTree;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Gizmos
{
    public class AabbEntitiesTreeDrawer : MonoBehaviour
    {
#if UNITY_EDITOR
        [Range(1, 10)]
        public int TargetTreeLevel = 1;

        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private IRTree _rTree;

        private readonly List<Color> _colors;
        private readonly Dictionary<int, Color> _colorsInUse = new();

        private int _colorIndex = -1;

        public AabbEntitiesTreeDrawer()
        {
            const int count = 64;
            const int hueStep = 41;
            var hue = -hueStep;

            _colors = Enumerable
                .Range(0, count)
                .Select(i =>
                {
                    hue += hueStep;
                    return Color.HSVToRGB(hue % 360 / 360f, (i % 2 + 1) / 2f, 1);
                })
                .ToList();

            _colorsInUse.Clear();
        }

        public void SetRTree(IRTree rTree)
        {
            _rTree = rTree;
        }

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;

            for (var subTreeIndex = 0; subTreeIndex < _rTree.SubTreesCount; subTreeIndex++)
            {
                var nodes = _rTree.GetSubTreeRootNodes(subTreeIndex);
                foreach (var (node, i) in nodes.Select((n, i) => (n, i)))
                {
                    var aabb = node.Aabb;
                    if (!aabb.IsValid())
                        continue;

                    DrawChildren(node, subTreeIndex, 1, 17 * 23 + (int) math.pow(subTreeIndex + 1, 2) * i);
                }
            }
        }

        private void DrawChildren(RTreeNode node, int subTreeIndex, int levelIndex, int hashCode)
        {
            Assert.IsTrue(levelIndex <= _rTree.GetSubTreeHeight(subTreeIndex));

            var isTargetLevel = levelIndex == TargetTreeLevel;
            var isLeafsLevel = levelIndex == _rTree.GetSubTreeHeight(subTreeIndex);

            var color = GetDrawColor(hashCode);

            if (isTargetLevel || isLeafsLevel)
            {
                if (isTargetLevel)
                    DrawNodeLevel(node, color, subTreeIndex, levelIndex, hashCode);
                else
                    DrawLeafEntries(node, color * new Color(1, 1, 1, 0.99f));

                return;
            }

            DrawNodeLevel(node, color, subTreeIndex, levelIndex, hashCode);
        }

        private void DrawNodeLevel(RTreeNode node, Color color, int subTreeIndex, int levelIndex, int hashCode)
        {
            var childNodes = _rTree.GetNodes(subTreeIndex, levelIndex,
                Enumerable.Range(node.EntriesStartIndex, node.EntriesCount));

            foreach (var (childNode, i) in childNodes.Select((n, i) => (n, i)))
            {
                var isTargetLevel = levelIndex == TargetTreeLevel;
                var isPreLeafsLevel = levelIndex + 1 == _rTree.GetSubTreeHeight(subTreeIndex);

                switch (isTargetLevel)
                {
                    case false when !isPreLeafsLevel:
                        DrawChildren(childNode, subTreeIndex, levelIndex + 1, hashCode * 23 + i);
                        continue;

                    case true when !isPreLeafsLevel:
                        DrawParentNodeRect(node, color);
                        DrawNodeEntries(node, color * new Color(1, 1, 1, 0.99f), subTreeIndex, levelIndex);
                        break;

                    default:
                        DrawParentNodeRect(childNode, color);
                        DrawLeafEntries(childNode, color * new Color(1, 1, 1, 0.99f));
                        break;
                }
            }
        }

        private static void DrawParentNodeRect(RTreeNode node, Color color)
        {
            var aabb = node.Aabb;
            var size = aabb.max - aabb.min;
            var center = fix2.ToXY(aabb.min + size / new fix(2));

            UnityEngine.Gizmos.color = color;
            UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));

            var rightTopCorner = fix2.ToXY(aabb.max);
            var leftTopCorner = fix2.ToXY(new fix2(aabb.min.x, aabb.max.y));
            var leftBottomCorner = fix2.ToXY(aabb.min);
            var rightBottomCorner = fix2.ToXY(new fix2(aabb.max.x, aabb.min.y));

            const float width = 8f;
            Handles.DrawBezier(rightTopCorner, leftTopCorner, rightTopCorner, leftTopCorner, color, null, width);
            Handles.DrawBezier(leftTopCorner, leftBottomCorner, leftTopCorner, leftBottomCorner, color, null, width);
            Handles.DrawBezier(leftBottomCorner, rightBottomCorner, leftBottomCorner, rightBottomCorner, color, null, width);
            Handles.DrawBezier(rightBottomCorner, rightTopCorner, rightBottomCorner, rightTopCorner, color, null, width);
        }

        private void DrawNodeEntries(RTreeNode node, Color color, int subTreeIndex, int levelIndex)
        {
            var childNodes = _rTree.GetNodes(subTreeIndex, levelIndex,
                Enumerable.Range(node.EntriesStartIndex, node.EntriesCount));

            foreach (var entry in childNodes)
            {
                var aabb = entry.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = color;
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));
                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));
            }
        }

        private void DrawLeafEntries(RTreeNode node, Color color)
        {
            var leafEntries = _rTree.GetLeafEntries(Enumerable.Range(node.EntriesStartIndex, node.EntriesCount));

            foreach (var entry in leafEntries)
            {
                var aabb = entry.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = color;
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));
                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));
            }
        }

        private Color GetDrawColor(int hashCode)
        {
            if (_colorsInUse.TryGetValue(hashCode, out var color))
                return color;

            color = _colors[++_colorIndex % _colors.Count];
            _colorsInUse.Add(hashCode, color);

            return color;
        }
#endif
    }
}
