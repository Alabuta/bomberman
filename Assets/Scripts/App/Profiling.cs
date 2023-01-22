using Unity.Profiling;

namespace App
{
    public static class Profiling
    {
        public static readonly ProfilerMarker PlayersInputProcess = new(nameof(PlayersInputProcess));
        public static readonly ProfilerMarker EcsFixedSystemsUpdate = new(nameof(EcsFixedSystemsUpdate));
        public static readonly ProfilerMarker BeforeSimulationStep = new(nameof(BeforeSimulationStep));
        public static readonly ProfilerMarker UpdateWorldModel = new(nameof(UpdateWorldModel));
        public static readonly ProfilerMarker WorldViewUpdate = new(nameof(WorldViewUpdate));
        public static readonly ProfilerMarker AttackBehavioursUpdate = new(nameof(AttackBehavioursUpdate));
        public static readonly ProfilerMarker MovementBehavioursUpdate = new(nameof(MovementBehavioursUpdate));
        public static readonly ProfilerMarker CollisionsDetection = new(nameof(CollisionsDetection));
        public static readonly ProfilerMarker CollisionsResolver = new(nameof(CollisionsResolver));
        public static readonly ProfilerMarker RTreeBuild = new(nameof(RTreeBuild));
        public static readonly ProfilerMarker RTreeNativeArrayFill = new(nameof(RTreeNativeArrayFill));
        public static readonly ProfilerMarker RTreeInsert = new(nameof(RTreeInsert));
        public static readonly ProfilerMarker RTreeInsertJob = new(nameof(RTreeInsertJob));
        public static readonly ProfilerMarker RTreeInitInsertJob = new(nameof(RTreeInitInsertJob));
        public static readonly ProfilerMarker RTreeGrow = new(nameof(RTreeGrow));
        public static readonly ProfilerMarker RTreeSplitNode = new(nameof(RTreeSplitNode));
        public static readonly ProfilerMarker RTreeLeafNodesUpdate = new(nameof(RTreeLeafNodesUpdate));
        public static readonly ProfilerMarker RTreeNodesUpdate = new(nameof(RTreeNodesUpdate));
    }
}
