﻿using Unity.Profiling;

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
        public static readonly ProfilerMarker RTreeUpdate = new(nameof(RTreeUpdate));
        public static readonly ProfilerMarker RTreeNativeArrayFill = new(nameof(RTreeNativeArrayFill));
        public static readonly ProfilerMarker RTreeInsert = new(nameof(RTreeInsert));
        public static readonly ProfilerMarker RTreeInsertJob = new(nameof(RTreeInsertJob));
        public static readonly ProfilerMarker RTreeInsertJobInitWorker = new(nameof(RTreeInsertJobInitWorker));
        public static readonly ProfilerMarker RTreeInsertEntry = new(nameof(RTreeInsertEntry));
        public static readonly ProfilerMarker RTreeInitInsertJob = new(nameof(RTreeInitInsertJob));
        public static readonly ProfilerMarker RTreeInsertJobComplete = new(nameof(RTreeInsertJobComplete));
        public static readonly ProfilerMarker RTreeGrow = new(nameof(RTreeGrow));
        public static readonly ProfilerMarker RTreeSplitNode = new(nameof(RTreeSplitNode));
        public static readonly ProfilerMarker RTreeLeafNodesUpdate = new(nameof(RTreeLeafNodesUpdate));
        public static readonly ProfilerMarker RTreeNodesUpdate = new(nameof(RTreeNodesUpdate));
        public static readonly ProfilerMarker RTreeGetNodeIndexToInsert = new(nameof(RTreeGetNodeIndexToInsert));
        public static readonly ProfilerMarker RTreeFindLargestPair = new(nameof(RTreeFindLargestPair));
        public static readonly ProfilerMarker RTreeFillNodes = new(nameof(RTreeFillNodes));
        public static readonly ProfilerMarker RTreeIsSecondNodeTarget = new(nameof(RTreeIsSecondNodeTarget));
    }
}
