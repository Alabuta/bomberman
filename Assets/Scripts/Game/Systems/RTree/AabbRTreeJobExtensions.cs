#if !NO_WORK_STEALING_RTREE_INSERT_JOB
namespace Game.Systems.RTree
{
    [JobProducerType(typeof(AabbRTreeJobExtensions.WorkStealingJobProducer<>))]
    public interface IWorkStealingJob
    {
        void Execute(ref PerWorkerData perWorkerData, int startIndex, int count);
    }

    public struct PerWorkerData
    {
        public int WorkerIndex;
        public NativeArray<int> CurrentThreadNodesEndIndices;
        public NativeArray<RTreeLeafEntry> CurrentThreadResultEntries;
    }

    public static class AabbRTreeJobExtensions
    {
        internal struct WorkStealingJobProducer<T> where T : struct, IWorkStealingJob
        {
            internal static readonly SharedStatic<IntPtr> jobReflectionData =
                SharedStatic<IntPtr>.GetOrCreate<WorkStealingJobProducer<T>>();

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
                ref var insertJobData = ref UnsafeUtility.As<T, AabbRTree.InsertJob>(ref jobData);

                var readOnlyData = insertJobData.ReadOnlyData;
                var sharedWriteData = insertJobData.SharedWriteData;

                var workerIndex = sharedWriteData.CountersContainer[0].Add(1);
                sharedWriteData.PerThreadWorkerIndices[workerThreadIndex] = workerIndex;

                if (workerIndex >= insertJobData.ReadOnlyData.SubTreesCount)
                    return;

#if ENABLE_RTREE_ASSERTS
                Assert.IsTrue(workerIndex < insertJobData.ReadOnlyData.SubTreesCount);
#endif

                sharedWriteData.RootNodesLevelIndices[workerIndex] = 0;
                sharedWriteData.RootNodesCounts[workerIndex] = 0;

                var treeMaxHeight = readOnlyData.TreeMaxHeight;
                var resultEntriesContainerCapacity = readOnlyData.ResultEntriesContainerCapacity;

                var nodesContainerStartIndex = workerIndex * readOnlyData.NodesContainerCapacity;
                for (var i = 0; i < AabbRTree.MaxEntries; i++)
                    sharedWriteData.NodesContainer[nodesContainerStartIndex + i] = TreeEntryTraits<RTreeNode>.InvalidEntry;

                var currentThreadNodesEndIndices = sharedWriteData.NodesEndIndicesContainer
                    .GetSubArray(workerIndex * treeMaxHeight, treeMaxHeight);

                UnsafeUtility.MemClear(currentThreadNodesEndIndices.GetUnsafePtr(), currentThreadNodesEndIndices.Length);
                currentThreadNodesEndIndices[sharedWriteData.RootNodesLevelIndices[workerIndex]] = AabbRTree.MaxEntries;

                var currentThreadResultEntries = sharedWriteData.ResultEntries
                    .GetSubArray(workerIndex * resultEntriesContainerCapacity, resultEntriesContainerCapacity);

                var perWorkerData = new PerWorkerData
                {
                    WorkerIndex = workerIndex,
                    CurrentThreadNodesEndIndices = currentThreadNodesEndIndices,
                    CurrentThreadResultEntries = currentThreadResultEntries
                };

                var firstBatchIndex = -1;

                while (JobsUtility.GetWorkStealingRange(ref ranges, workerIndex, out var startIndex, out var endIndex))
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
                }

                sharedWriteData.PerThreadWorkerIndices[workerThreadIndex] = -1;
            }
        }

        [UsedImplicitly]
        public static void EarlyJobInit<T>()
            where T : struct, IWorkStealingJob
        {
            WorkStealingJobProducer<T>.Initialize();
        }

        public static JobHandle ScheduleBatch<T>(
            this T jobData,
            int arrayLength,
            int minIndicesPerJobCount,
            JobHandle dependsOn = new())
            where T : struct, IWorkStealingJob
        {
            return Schedule(jobData, arrayLength, minIndicesPerJobCount, dependsOn, ScheduleMode.Parallel);
        }

        public static void RunBatch<T>(this T jobData, int arrayLength)
            where T : struct, IWorkStealingJob
        {
            Schedule(jobData, arrayLength, arrayLength, new JobHandle(), ScheduleMode.Parallel);
        }

        private static unsafe JobHandle Schedule<T>(T jobData, int arrayLength, int minIndicesPerJobCount,
            JobHandle dependsOn, ScheduleMode scheduleMode)
            where T : struct, IWorkStealingJob
        {
            var reflectionData = WorkStealingJobProducer<T>.jobReflectionData.Data;
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
#endif
