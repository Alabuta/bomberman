using System;
using System.Collections.Generic;
using Game.Components;
using Game.Components.Tags;
using Leopotam.Ecs;
using Math.FixedPointMath;

namespace Game.Systems
{
    public interface IRTree : IDisposable
    {
        int TreeHeight { get; }
        IEnumerable<RTreeNode> RootNodes { get; }

        IEnumerable<RTreeNode> GetNodes(int levelIndex, IEnumerable<int> indices);

        IEnumerable<RTreeLeafEntry> GetLeafEntries(IEnumerable<int> indices);

        void QueryByLine(fix2 p0, fix2 p1, ICollection<RTreeLeafEntry> result);

        void QueryByAabb(in AABB aabb, ICollection<RTreeLeafEntry> result);

        void Build(EcsFilter<TransformComponent, HasColliderTag> filter, fix simulationSubStep);
    }
}
