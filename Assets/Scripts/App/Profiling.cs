using Unity.Profiling;

namespace App
{
    public static class Profiling
    {
        public static readonly ProfilerCategory Simulation = new(nameof(Simulation));

        public static readonly ProfilerMarker PlayersInputProcess = new(Simulation, nameof(PlayersInputProcess));
        public static readonly ProfilerMarker EcsFixedSystemsUpdate = new(Simulation, nameof(EcsFixedSystemsUpdate));
        public static readonly ProfilerMarker BeforeSimulationStep = new(Simulation, nameof(BeforeSimulationStep));
        public static readonly ProfilerMarker UpdateWorldModel = new(Simulation, nameof(UpdateWorldModel));
        public static readonly ProfilerMarker WorldViewUpdate = new(Simulation, nameof(WorldViewUpdate));
        public static readonly ProfilerMarker AttackBehavioursUpdate = new(Simulation, nameof(AttackBehavioursUpdate));
        public static readonly ProfilerMarker MovementBehavioursUpdate = new(Simulation, nameof(MovementBehavioursUpdate));
        public static readonly ProfilerMarker CollisionsDetection = new(Simulation, nameof(CollisionsDetection));
        public static readonly ProfilerMarker CollisionsResolver = new(Simulation, nameof(CollisionsResolver));
        public static readonly ProfilerMarker RTreeBuild = new(Simulation, nameof(RTreeBuild));
        public static readonly ProfilerMarker RTreeNativeArrayFill = new(Simulation, nameof(RTreeNativeArrayFill));
        public static readonly ProfilerMarker RTreeInsert = new(Simulation, nameof(RTreeInsert));
        public static readonly ProfilerMarker RTreeInsertJob = new(Simulation, nameof(RTreeInsertJob));
        public static readonly ProfilerMarker RTreeInitInsertJob = new(Simulation, nameof(RTreeInitInsertJob));
        public static readonly ProfilerMarker RTreeGrow = new(Simulation, nameof(RTreeGrow));
        public static readonly ProfilerMarker RTreeSplitNode = new(Simulation, nameof(RTreeSplitNode));
        public static readonly ProfilerMarker RTreeLeafNodesUpdate = new(Simulation, nameof(RTreeLeafNodesUpdate));
        public static readonly ProfilerMarker RTreeNodesUpdate = new(Simulation, nameof(RTreeNodesUpdate));
        public static readonly ProfilerMarker RTreeA = new(Simulation, nameof(RTreeA));
        public static readonly ProfilerMarker RTreeB = new(Simulation, nameof(RTreeB));
        public static readonly ProfilerMarker RTreeC = new(Simulation, nameof(RTreeC));
        public static readonly ProfilerMarker RTreeD = new(Simulation, nameof(RTreeD));
        public static readonly ProfilerMarker RTreeE = new(Simulation, nameof(RTreeE));
        public static readonly ProfilerMarker RTreeF = new(Simulation, nameof(RTreeF));
        public static readonly ProfilerMarker RTreeG = new(Simulation, nameof(RTreeG));
        public static readonly ProfilerMarker RTreeH = new(Simulation, nameof(RTreeH));
        public static readonly ProfilerMarker RTreeI = new(Simulation, nameof(RTreeI));
    }
}
