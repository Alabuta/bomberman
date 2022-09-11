using System.Linq;
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

            const float alpha = 0.8f;
            _colors = new[]
            {
                Color.red * new Color(1, 1, 1, alpha),
                Color.blue * new Color(1, 1, 1, alpha),
                Color.yellow * new Color(1, 1, 1, alpha),
                Color.cyan * new Color(1, 1, 1, alpha),
                Color.magenta * new Color(1, 1, 1, alpha)
            };
        }

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;
            UnityEngine.Gizmos.color = Color.black * new Color(1, 1, 1, 0.2f);

            const int treeDepth = 0;

            if (_rTree.RootNodes.Any(i => i < 0))
                return;

            var rootNodes = _rTree.GetNodes(_rTree.RootNodes);
            foreach (var rootNode in rootNodes)
            {
                var aabb = rootNode.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));

                DrawChildren(rootNode, treeDepth + 1);
            }
        }

        private void DrawChildren(Node rootNode, int treeDepth)
        {
            /*if (treeDepth > 1)
                return;*/

            if (rootNode.IsLeafNode)
                return;

            var childNodes = _rTree.GetNodes(Enumerable.Range(rootNode.EntriesStartIndex, rootNode.EntriesCount));

            var i = -1;
            foreach (var childNode in childNodes)
            {
                var aabb = childNode.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = _colors[++i];
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));

                DrawChildren(childNode, treeDepth + 1);
            }
        }
    }
}
