namespace MeidaOps.Plan.Injector
{
    using System;
    using System.Linq;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    internal class Program
    {
        static void Main(string[] args)
        {
            var credentials = GetLocalCredentials();

            Console.WriteLine("Connecting to DataMiner...");
            DMConnection connection = ConnectionSettings.GetConnection("mediaopstre-skylinedevelopment.on.dataminer.services");
            connection.Authenticate(credentials.UserName, credentials.Password);
            Console.WriteLine("Connected to DataMiner\r\n");

            using (ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole()))
            {
                var logger = factory.CreateLogger<IMediaOpsPlanApi>();
                var api = new MediaOpsPlanApi(connection, logger);
                var resource = new UnmanagedResource
                {
                    Name = $"Green Resource [{Guid.NewGuid()}]",
                };

                var id = api.Resources.Create(resource);
                logger.LogInformation($"Created new resource with ID: {id}");
            }
        }

        private static NetworkCredential GetLocalCredentials()
        {
            var environment = System.IO.File.ReadAllLines("..\\..\\..\\.env");
            var userName = environment.FirstOrDefault(x => x.StartsWith("US="))?.Skip(3)?.ToArray() ?? throw new InvalidOperationException("No username defined");
            var password = environment.FirstOrDefault(x => x.StartsWith("PW="))?.Skip(3)?.ToArray() ?? throw new InvalidOperationException("No password defined");
            return new NetworkCredential(new String(userName), new String(password));
        }
    }
}
