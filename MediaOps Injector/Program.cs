namespace MediaOps_Injector
{
    using System;
    using System.Net;

    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;

    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
	{
		static void Main(string[] args)
		{
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("localhost");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var planApi = new MediaOpsPlanApi(connection);

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

            var movedResourcePool = planApi.ResourcePools.Read(resourcePoolId);
            movedResourcePool.Name = "UpdatedResourcePool";
            planApi.ResourcePools.Update(movedResourcePool);

            Console.WriteLine($"Updated Resource Pool Name: {movedResourcePool.Name}");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
	}
}
