namespace MediaOps_Injector
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
    {
        public static readonly string OpenTelemetrySourceName = "Skyline.DataMiner.MediaOps.Plan.Injector";

        private static readonly ActivitySource activitySource = new ActivitySource(OpenTelemetrySourceName);

        static void Main(string[] args)
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("slc-h67-g03.skyline.local");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var planApi = new MediaOpsPlanApi(connection);

            var tracingProvider = Sdk.CreateTracerProviderBuilder()
                .SetSampler(new AlwaysOnSampler())
                .AddSource(OpenTelemetrySourceName)
                .AddSource(ActivityHelper.ApiSourceName)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://SLC-H67-G02:8081/traces"); // Replace with your OpenTelemetry collector endpoint
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"Authorization=Bearer Skyline123";
                })
                .Build();

            try
            {
                using (activitySource.StartActivity("MediaOps Plan Injector"))
                {
                    //TestResourcePoolRepository(planApi);
                    TestResourceRepository(planApi);

                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something went wrong: {e}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
            finally
            {
                planApi.Dispose();
                tracingProvider.Dispose();
            }
        }

        private static void TestResourcePoolRepository(MediaOpsPlanApi planApi)
        {
            var resourcePool = new ResourcePool()
            {
                Name = "MyResourcePool",
            };

            var resourcePoolId = planApi.ResourcePools.Create(resourcePool);
            Console.WriteLine($"Created Resource Pool with ID: {resourcePoolId}\r\n");

            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            var createdResourcePool = planApi.ResourcePools.Read(resourcePoolId);
            if (createdResourcePool != null)
            {
                Console.WriteLine($"Resource Pool Name: {createdResourcePool.Name}");
                Console.WriteLine($"Resource Pool ID: {createdResourcePool.Id}\r\n");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Failed to retrieve the created Resource Pool.\r\n");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();

                return;
            }

            planApi.ResourcePools.MoveTo(createdResourcePool, ResourcePoolState.Complete);
            Console.WriteLine($"Moved Resource Pool to state: {createdResourcePool.State}\r\n");
            Console.WriteLine($"Resource Pool ID: {createdResourcePool.Id}\r\n");
            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            var movedResourcePool = planApi.ResourcePools.Read(resourcePoolId);
            movedResourcePool.Name = "UpdatedResourcePool";
            planApi.ResourcePools.Update(movedResourcePool);

            Console.WriteLine($"Updated Resource Pool Name: {movedResourcePool.Name}");
        }

        private static void TestResourceRepository(MediaOpsPlanApi planApi)
        {
            var unmanagedResource = new UnmanagedResource()
            {
                Name = "MyUnmanagedResource",
                Concurrency = 10,
            };

            var unmanagedResourceId = planApi.Resources.Create(unmanagedResource);
            Console.WriteLine($"Created Unmanaged Resource with ID: {unmanagedResourceId}\r\n");

            var elementResource = new ElementResource()
            {
                Name = "MyElementResource",
                AgentId = 78,
                ElementId = 140466,
            };

            var elementResourceId = planApi.Resources.Create(elementResource);
            Console.WriteLine($"Created Element Resource with ID: {elementResourceId}\r\n");

            Console.WriteLine("Press Enter to continue...");
            Console.ReadLine();

            planApi.Resources.Delete(unmanagedResource, elementResource);

            var allResources = planApi.Resources.ReadAll();
            Console.WriteLine($"Resource Count: {allResources.Count()}");
            Console.WriteLine($"Draft Resource Count: {allResources.Count(x => x.State == ResourceState.Draft)}");
            Console.WriteLine($"Complete Resource Count: {allResources.Count(x => x.State == ResourceState.Complete)}");
            Console.WriteLine($"Deprecated Resource Count: {allResources.Count(x => x.State == ResourceState.Deprecated)}");
            Console.WriteLine($"Unmanaged Resource Count: {allResources.Count(x => x is UnmanagedResource)}");
            Console.WriteLine($"Service Resource Count: {allResources.Count(x => x is ServiceResource)}");
            Console.WriteLine($"Element Resource Count: {allResources.Count(x => x is ElementResource)}");
            Console.WriteLine($"Virtual Function Resource Count: {allResources.Count(x => x is VirtualFunctionResource)}");
        }
    }
}
