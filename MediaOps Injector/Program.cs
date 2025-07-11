namespace MediaOps_Injector
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using OpenTelemetry;
    using OpenTelemetry.Trace;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;

    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
    {
        public static readonly string OpenTelemetrySourceName = "Skyline.DataMiner.MediaOps.Plan.Injector";
        internal static readonly ActivitySource activitySource = new ActivitySource(OpenTelemetrySourceName);

        static void Main(string[] args)
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("slc-h67-g03.skyline.local");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var planApi = new MediaOpsPlanApi(connection);

            var tracingProvider = Sdk.CreateTracerProviderBuilder()
                .AddSource(OpenTelemetrySourceName)
                .AddSource(MediaOpsPlanApi.ApiSourceName)
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
                tracingProvider.Dispose();
            }
        }
    }
}
