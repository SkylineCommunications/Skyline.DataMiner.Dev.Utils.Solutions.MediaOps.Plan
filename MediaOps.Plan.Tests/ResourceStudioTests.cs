namespace Dev.Utils.Solutions.MediaOps.Plan.Tests
{
    using System;
    using System.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;
    using DMConnection = Skyline.DataMiner.Net.Connection;
    using System.Diagnostics;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceStudioTests
    {
        private readonly List<long> creationTimings = new List<long>();
        private readonly List<long> deletionTimings = new List<long>();

        [TestMethod]
        public void TestCreateResources_SingleCalls()
        {
            creationTimings.Clear();
            deletionTimings.Clear();

            var api = GetApi();

            int[] batchSizes = { 5, /*10, 20, 50, 100, 200*/ };
            foreach (var batchSize in batchSizes)
            {
                var resourceIds = CreateResources(api, batchSize);
                DeleteResources(api, resourceIds);
            }

            Console.WriteLine($"\tCreate\tDelete");
            for (int i = 0; i < batchSizes.Length; i++)
            {
                Console.WriteLine($"{batchSizes[i]}\t{creationTimings[i]}\t{deletionTimings[i]}");
            }
        }

        [TestMethod]
        public void TestCreateResources_BulkCalls()
        {
            for (int run = 0; run < 5; run++)
            {
                creationTimings.Clear();
                deletionTimings.Clear();

                var api = GetApi();

                int[] batchSizes = { 5, 10, 20, 50, 100, 200 };
                foreach (var batchSize in batchSizes)
                {
                    var resourceIds = BulkCreateResources(api, batchSize);
                    BulkDeleteResources(api, resourceIds);
                }

                Console.WriteLine($"\tCreate\tDelete");
                for (int i = 0; i < batchSizes.Length; i++)
                {
                    Console.WriteLine($"{batchSizes[i]}\t{creationTimings[i]}\t{deletionTimings[i]}");
                }
            }
        }

        private MediaOpsPlanApi GetApi()
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("slc-h67-g02.skyline.local");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            return new MediaOpsPlanApi(connection);
        }

        private Guid[] CreateResources(MediaOpsPlanApi api, int count)
        {
            var stopwatch = Stopwatch.StartNew();

            var ids = new Guid[count];
            for (int i = 0; i < count; i++)
            {
                var resource = new UnmanagedResource { Name = $"Green Resource [{Guid.NewGuid()}]" };
                var resourceId = api.Resources.Create(resource);
                ids[i] = resourceId;
            }

            stopwatch.Stop();
            creationTimings.Add(stopwatch.ElapsedMilliseconds);

            return ids;
        }

        private Guid[] BulkCreateResources(MediaOpsPlanApi api, int count)
        {
            var stopwatch = Stopwatch.StartNew();

            var resources = new List<Resource>();

            for (int i = 0; i < count; i++)
            {
                resources.Add(new UnmanagedResource { Name = $"Green Resource [{Guid.NewGuid()}]" });
            }

            var ids = api.Resources.Create(resources).ToArray();

            stopwatch.Stop();
            creationTimings.Add(stopwatch.ElapsedMilliseconds);

            return ids;
        }

        private void DeleteResources(MediaOpsPlanApi api, Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();
            foreach (var resourceId in resourceIds)
            {
                api.Resources.Delete(resourceId);
            }

            stopwatch.Stop();
            deletionTimings.Add(stopwatch.ElapsedMilliseconds);
        }

        private void BulkDeleteResources(MediaOpsPlanApi api, Guid[] resourceIds)
        {
            var stopwatch = Stopwatch.StartNew();

            api.Resources.Delete(resourceIds);

            stopwatch.Stop();
            deletionTimings.Add(stopwatch.ElapsedMilliseconds);
        }
    }
}
