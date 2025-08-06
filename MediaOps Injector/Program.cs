namespace MediaOps_Injector
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using Microsoft.Extensions.Logging;
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

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            var logger = loggerFactory.CreateLogger<Program>();

            var tracingProvider = Sdk.CreateTracerProviderBuilder()
                .SetSampler(new AlwaysOnSampler())
                .AddSource(OpenTelemetrySourceName)
                .AddSource(ActivityHelper.ApiSourceName)
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri("http://SLC-H67-G02:8081/traces");
                    options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    options.Headers = $"Authorization=Bearer Skyline123";
                })
                //.AddConsoleExporter()
                .Build();

            var planApi = new MediaOpsPlanApi(connection, loggerFactory.CreateLogger<IMediaOpsPlanApi>());

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
                logger.LogError(e, "Something went wrong: {Message}", e.Message);
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
            finally
            {
                planApi.Dispose();
                loggerFactory.Dispose();
                tracingProvider.Dispose();
            }
        }

        private static void TestResourcePoolRepository(IMediaOpsPlanApi planApi)
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

        private static void TestResourceRepository(IMediaOpsPlanApi planApi)
        {
            var unmanagedResource = new UnmanagedResource()
            {
                Name = "MyUnmanagedResource",
                Concurrency = 10,
            };

            var unmanagedResourceId = planApi.Resources.Create(unmanagedResource);
            Console.WriteLine($"Created Unmanaged Resource with ID: {unmanagedResourceId}\r\n");

            planApi.Resources.MoveTo(unmanagedResource, ResourceState.Complete);
            planApi.Resources.MoveTo(unmanagedResource, ResourceState.Deprecated);
            planApi.Resources.Delete(unmanagedResource);

            var elementResource = new ElementResource()
            {
                Name = "MyElementResource",
                AgentId = 78,
                ElementId = 140466,
            };

            var elementResourceId = planApi.Resources.Create(elementResource);
            Console.WriteLine($"Created Element Resource with ID: {elementResourceId}\r\n");

            var virtualFunctionResource = new VirtualFunctionResource()
            {
                Name = "MyVirtualFunctionResource",
                AgentId = 78,
                ElementId = 140461,
                FunctionId = Guid.Parse("7bd8d399-b503-4fd9-9b2e-8dc188d591b8"), // Example Function ID
                FunctionTableIndex = "1"
            };

            var virtualFunctionResourceId = planApi.Resources.Create(virtualFunctionResource);
            Console.WriteLine($"Created Virtual Function Resource with ID: {virtualFunctionResourceId}\r\n");

            var serviceResource = new ServiceResource()
            {
                Name = "MyServiceResource",
                AgentId = 78,
                ServiceId = 140467, // Example Service ID
            };

            var serviceResourceId = planApi.Resources.Create(serviceResource);
            Console.WriteLine($"Created Service Resource with ID: {serviceResourceId}\r\n");

            var convertedVirtualFunctionResource = planApi.Resources.ConvertToVirtualFunctionResource(elementResource, new ResourceVirtualFunctionLinkConfiguration
            {
                AgentId = 78,
                ElementId = 140461,
                FunctionId = Guid.Parse("7bd8d399-b503-4fd9-9b2e-8dc188d591b8"), // Example Function ID
                FunctionTableIndex = "1"
            });

            var convertedElementResource = planApi.Resources.ConvertToElementResource(virtualFunctionResource, new ResourceElementLinkConfiguration
            {
                AgentId = 78,
                ElementId = 140466,
            });

            planApi.Resources.Delete(elementResource, virtualFunctionResource, serviceResource);

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
