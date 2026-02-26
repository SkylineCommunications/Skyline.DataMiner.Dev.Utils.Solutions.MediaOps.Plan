namespace MediaOps_Injector
{
    using System;
    using System.Net;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
    {
        static void Main(string[] args)
        {
            // Usage: MediaOps_Injector [host]
            // host: The hostname or IP address of the DataMiner Agent to connect to (default: localhost)
            var host = args.Length > 0 ? args[0] : "localhost";
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection(host);
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var api = connection.GetMediaOpsPlanApi();

            var resource = new UnmanagedResource()
            {
                Name = "Injected Resource",
                Concurrency = 1,
            };

            api.Resources.Create(resource);

            var r = api.Resources.Read(resource.Id);
            r.Name = "Updated Injected Resource";

            api.Resources.Update(r);
        }
    }
}
