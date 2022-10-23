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

        private CollidersRectTree _rTree;
        private Color[] _colors;

        public void SetRTree(CollidersRectTree rTree)
        {
            _rTree = rTree;

            _colors = new[]
            {
                Color.red,
                Color.blue,
                Color.yellow,
                Color.cyan,
                Color.white,
                Color.magenta
            };
        }

        private void OnDrawGizmos()
        {
            UnityEngine.Gizmos.matrix = Matrix4x4.identity;

            const int treeDepth = 0;

            var rootNodes = _rTree.RootNodes;
            foreach (var rootNode in rootNodes)
            {
                var aabb = rootNode.Aabb;
                if (aabb == AABB.Invalid)
                    continue;

                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = Color.black * new Color(1, 1, 1, 0.4f);
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));

                UnityEngine.Gizmos.color = Color.black;
                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));

                DrawChildren(rootNode, treeDepth + 1);
            }
        }

        private void DrawChildren(Node rootNode, int treeDepth)
        {
            /*if (treeDepth > 1)
                return;*/

            if (treeDepth >= _rTree.TreeHeight)
                return;

            var childNodes = _rTree.GetNodes(treeDepth, Enumerable.Range(rootNode.EntriesStartIndex, rootNode.EntriesCount));

            var i = -1;
            foreach (var childNode in childNodes)
            {
                var aabb = childNode.Aabb;
                var size = aabb.max - aabb.min;
                var center = fix2.ToXY(aabb.min + size / new fix(2));

                UnityEngine.Gizmos.color = _colors[++i] * new Color(1, 1, 1, 0.2f * treeDepth);
                UnityEngine.Gizmos.DrawCube(center, fix2.ToXY(size));

                UnityEngine.Gizmos.color = _colors[i];
                UnityEngine.Gizmos.DrawWireCube(center, fix2.ToXY(size));

                DrawChildren(childNode, treeDepth + 1);
            }
        }
    }
}
