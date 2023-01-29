using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Game.Systems.RTree
{
    [JobProducerType(typeof(JobExtensions.InsertJobProducer<>))]
    public interface IInsertJob
    {
        void Execute(ref PerWorkerData perWorkerData, int startIndex, int count);
    }

    public struct PerWorkerData
    {
        public NativeArray<int> CurrentThreadNodesEndIndices;
        public NativeArray<RTreeLeafEntry> CurrentThreadResultEntries;

        public int JobIndex;
        public int LeafEntriesCounter;
    }

    public static class JobExtensions
    {
        internal struct InsertJobProducer<T> where T : struct, IInsertJob
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData =
                SharedStatic<IntPtr>.GetOrCreate<InsertJobProducer<T>>();

            private delegate void ExecuteJobFunction(
                ref T jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int workerThreadIndex);

            [Preserve]
            internal static void Initialize()
            {
                if (jobReflectionData.Data != IntPtr.Zero)
                    return;

                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), (ExecuteJobFunction) Execute);
            }

            private static unsafe void Execute(
                ref T jobData,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int workerThreadIndex)
            {
                var insertJobData = UnsafeUtility.As<T, AabbRTree.InsertJob>(ref jobData);

                var readOnlyData = insertJobData.ReadOnlyData;
                var sharedWriteData = insertJobData.SharedWriteData;

                var jobIndex = sharedWriteData.CountersContainer[0].Add(1);
                sharedWriteData.PerThreadWorkerIndices[workerThreadIndex] = jobIndex;

                sharedWriteData.RootNodesLevelIndices[jobIndex] = 0;

                var treeMaxHeight = readOnlyData.TreeMaxHeight;
                var resultEntriesContainerCapacity = readOnlyData.ResultEntriesContainerCapacity;

                var nodesContainerStartIndex = jobIndex * readOnlyData.NodesContainerCapacity;
                for (var i = 0; i < AabbRTree.MaxEntries; i++)
                    sharedWriteData.NodesContainer[nodesContainerStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

                var currentThreadNodesEndIndices = sharedWriteData.NodesEndIndicesContainer
                    .GetSubArray(jobIndex * treeMaxHeight, treeMaxHeight);

                UnsafeUtility.MemClear(currentThreadNodesEndIndices.GetUnsafePtr(), currentThreadNodesEndIndices.Length);
                currentThreadNodesEndIndices[sharedWriteData.RootNodesLevelIndices[jobIndex]] = AabbRTree.MaxEntries;

                var currentThreadResultEntries = sharedWriteData.ResultEntries
                    .GetSubArray(jobIndex * resultEntriesContainerCapacity, resultEntriesContainerCapacity);

                var perWorkerData = new PerWorkerData
                {
                    CurrentThreadNodesEndIndices = currentThreadNodesEndIndices,
                    CurrentThreadResultEntries = currentThreadResultEntries,
                    JobIndex = jobIndex,
                    LeafEntriesCounter = 0
                };

                var firstBatchIndex = -1;

                while (JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out var startIndex, out var endIndex))
                {
                    var batchSize = endIndex - startIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(
                        bufferRangePatchData,
                        UnsafeUtility.AddressOf(ref jobData),
                        startIndex,
                        batchSize);
#endif
                    if (firstBatchIndex == -1)
                        firstBatchIndex = startIndex;

                    jobData.Execute(ref perWorkerData, startIndex, batchSize);

                    // Preventing work stealing
                    // :TODO: change to work stealing logic
                    /*if (endIndex - firstBatchIndex == insertJobData.ReadOnlyData.PerWorkerEntriesCount)
                        break;*/
                }

                sharedWriteData.PerThreadWorkerIndices[workerThreadIndex] = -1;
            }
        }

        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IInsertJob
        {
            InsertJobProducer<T>.Initialize();
        }

        public static JobHandle ScheduleBatch<T>(
            this T jobData,
            int arrayLength,
            int minIndicesPerJobCount,
            JobHandle dependsOn = new())
            where T : struct, IInsertJob
        {
            return Schedule(jobData, arrayLength, minIndicesPerJobCount, dependsOn, ScheduleMode.Parallel);
        }

        public static void RunBatch<T>(this T jobData, int arrayLength)
            where T : struct, IInsertJob
        {
            Schedule(jobData, arrayLength, arrayLength, new JobHandle(), ScheduleMode.Parallel);
        }

        private static unsafe JobHandle Schedule<T>(T jobData, int arrayLength, int minIndicesPerJobCount,
            JobHandle dependsOn, ScheduleMode scheduleMode)
            where T : struct, IInsertJob
        {
            var reflectionData = InsertJobProducer<T>.jobReflectionData.Data;
            CheckReflectionDataCorrect(reflectionData);

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData,
                dependsOn, scheduleMode);

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, minIndicesPerJobCount);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReflectionDataCorrect(IntPtr reflectionData)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call");
        }
    }
}
