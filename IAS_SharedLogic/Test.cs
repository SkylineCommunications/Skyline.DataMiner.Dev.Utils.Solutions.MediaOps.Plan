namespace IAS_TestMediaOpsPlanApi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using IAS_SharedLogic;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    internal class Test
    {
        private readonly IMediaOpsPlanApi planApi;

        private Test(IMediaOpsPlanApi planApi)
        {
            this.planApi = planApi;
        }

        public static ICollection<PerformanceResult> TestSingleDraftPerformance(IMediaOpsPlanApi planApi, int runCount)
        {
            var tests = new Test(planApi);
            var results = new List<PerformanceResult>();
            for (int i = 0; i < runCount; i++)
            {
                results.AddRange(tests.TestSingleDraftPerformance());
            }

            return results;
        }

        public static ICollection<PerformanceResult> TestBulkDraftPerformance(IMediaOpsPlanApi planApi, int runCount)
        {
            var tests = new Test(planApi);
            var results = new List<PerformanceResult>();
            for (int i = 0; i < runCount; i++)
            {
                results.AddRange(tests.TestBulkDraftPerformance());
            }

            return results;
        }

        public static string FormatResults(string title, ICollection<PerformanceResult> results)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"{title} - {DateTime.Now}");

            var uniqueBatchSizes = results.Select(r => r.BatchSize).Distinct().OrderBy(s => s);

            sb.AppendLine($"Creation Results");
            foreach (var batchSize in uniqueBatchSizes)
            {
                var creationResults = results.Where(r => r.BatchSize == batchSize).ToList();
                sb.AppendLine($"{batchSize}\t{String.Join("\t", creationResults.Select(r => r.ResourceCreationDuration))}");
            }

            sb.AppendLine($"Deletion Results");
            foreach (var batchSize in uniqueBatchSizes)
            {
                var creationResults = results.Where(r => r.BatchSize == batchSize).ToList();
                sb.AppendLine($"{batchSize}\t{String.Join("\t", creationResults.Select(r => r.ResourceDeletionDuration))}");
            }

            return sb.ToString();
        }

        private ICollection<PerformanceResult> TestSingleDraftPerformance()
        {
            List<PerformanceResult> results = new List<PerformanceResult>();
            int[] batchSizes = { 5, 10, 20, 50, 100, 200 }
            ;
            foreach (var batchSize in batchSizes)
            {
                var creationDuration = CreateDraftResources(planApi, batchSize, out Guid[] resourceIds);
                var moveToCompleteDuration = MoveToCompleteState(planApi, resourceIds);
                var deletionDuration = DeleteResources(planApi, resourceIds);

                results.Add(new PerformanceResult
                {
                    BatchSize = batchSize,
                    ResourceCreationDuration = creationDuration,
                    ResourceMoveToCompleteDuration = moveToCompleteDuration,
                    ResourceDeletionDuration = deletionDuration
                });
            }

            return results;
        }

        private ICollection<PerformanceResult> TestBulkDraftPerformance()
        {
            List<PerformanceResult> results = new List<PerformanceResult>();
            int[] batchSizes = { 5, 10, 20, 50, 100, 200 };
            foreach (var batchSize in batchSizes)
            {
                var creationDuration = BulkCreateDraftResources(planApi, batchSize, out Guid[] resourceIds);
                var moveToCompleteDuration = MoveToCompleteState(planApi, resourceIds);
                var deletionDuration = BulkDeleteResources(planApi, resourceIds);

                results.Add(new PerformanceResult
                {
                    BatchSize = batchSize,
                    ResourceCreationDuration = creationDuration,
                    ResourceMoveToCompleteDuration = moveToCompleteDuration,
                    ResourceDeletionDuration = deletionDuration
                });
            }

            return results;
        }

        private long CreateDraftResources(IMediaOpsPlanApi api, int count, out Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();

            resourceIds = new Guid[count];
            for (int i = 0; i < count; i++)
            {
                var resource = new UnmanagedResource { Name = $"Green Resource [{Guid.NewGuid()}]" };
                var resourceId = api.Resources.Create(resource);
                resourceIds[i] = resourceId;
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long BulkCreateDraftResources(IMediaOpsPlanApi api, int count, out Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();

            var resources = new List<Resource>();

            for (int i = 0; i < count; i++)
            {
                resources.Add(new UnmanagedResource { Name = $"Green Resource [{Guid.NewGuid()}]" });
            }

            resourceIds = api.Resources.Create(resources).ToArray();

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long MoveToCompleteState(IMediaOpsPlanApi api, Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();

            foreach (var resourceId in resourceIds)
            {
                api.Resources.MoveTo(resourceId, ResourceState.Complete);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long DeleteResources(IMediaOpsPlanApi api, Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var resourceId in resourceIds)
            {
                api.Resources.Delete(resourceId);
            }

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private long BulkDeleteResources(IMediaOpsPlanApi api, Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();

            api.Resources.Delete(resourceIds);

            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
