namespace MediaOps_Injector
{
    using System;
    using System.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Helper;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
    {
        static void Main(string[] args)
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("slc-h62-g04.skyline.local");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            Console.WriteLine("Connected to DataMiner\r\n");

            var api = new MediaOpsPlanApi(connection);
            var capabilityCount = api.Capabilities.CountAll();
            Console.WriteLine($"Total Capabilities: {capabilityCount}");
            var capabilities = api.Capabilities.ReadAll();
            capabilities.ForEach(x => Console.WriteLine(x.Name));
        }
    }
}
