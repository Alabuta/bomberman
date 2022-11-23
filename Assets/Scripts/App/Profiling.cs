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
        public static readonly ProfilerMarker RTreeGrow = new(Simulation, nameof(RTreeGrow));
    }
}
