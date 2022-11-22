using System.Collections.Generic;
using System.Linq;
using Game.Systems;
using Leopotam.Ecs;
using Level;
using Math.FixedPointMath;
using UnityEditor;
using UnityEngine;

namespace Gizmos
{
    public class AabbEntitiesTreeDrawer : MonoBehaviour
    {
#if UNITY_EDITOR
        private readonly EcsWorld _ecsWorld;
        private readonly World _world;

        private EntitiesAabbTree _rTree;

        private readonly List<Color> _colors;
        private readonly Dictionary<int, Color> _colorsInUse = new();

        public AabbEntitiesTreeDrawer()
        {
            const int count = 60;
            const int hueStep = 64;
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

        public void SetRTree(EntitiesAabbTree rTree)
        {
            _rTree = rTree;
        }

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;

            var nodes = _rTree.RootNodes;
            foreach (var (node, i) in nodes.Select((n, i) => (n, i)))
            {
                var aabb = node.Aabb;
                if (!aabb.IsValid())
                    continue;

                DrawChildren(node, 1, 17 * 23 + i);
            }
        }

        private void DrawChildren(Node node, int levelIndex, int hashCode)
        {
            if (levelIndex >= _rTree.TreeHeight)
                return;

            var childNodes = _rTree.GetNodes(levelIndex, Enumerable.Range(node.EntriesStartIndex, node.EntriesCount));

            foreach (var (childNode, i) in childNodes.Select((n, i) => (n, i)))
            {
                var childHashCode = hashCode * 23 + i;

                if (levelIndex + 1 != _rTree.TreeHeight)
                {
                    DrawChildren(childNode, levelIndex + 1, childHashCode);
                    continue;
                }

                if (!_colorsInUse.TryGetValue(childHashCode, out var color))
                {
                    color = _colors.LastOrDefault();
                    _colorsInUse.Add(childHashCode, color);
                    _colors.RemoveAt(_colors.Count - 1);
                }

                var aabb = childNode.Aabb;
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
                Handles.DrawBezier(leftBottomCorner, rightBottomCorner, leftBottomCorner, rightBottomCorner, color, null,
                    width);
                Handles.DrawBezier(rightBottomCorner, rightTopCorner, rightBottomCorner, rightTopCorner, color, null, width);

                DrawLeafEntries(childNode, color * new Color(1, 1, 1, 0.99f));
            }
        }

        private void DrawLeafEntries(Node node, Color color)
        {
            var leafEntries = _rTree.GetLeafEntries(Enumerable.Range(node.EntriesStartIndex, node.EntriesCount));

            foreach (var (aabb, _) in leafEntries)
            {
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = color;
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));
                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));
            }
        }
#endif
    }
}
