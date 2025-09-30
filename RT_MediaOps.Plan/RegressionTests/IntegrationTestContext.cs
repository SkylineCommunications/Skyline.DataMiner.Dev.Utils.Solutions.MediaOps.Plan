namespace RT_MediaOps.Plan.RegressionTests
{
    using System.Net;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net;

    using DMConnection = Skyline.DataMiner.Net.Connection;

    public sealed class IntegrationTestContext : IDisposable
    {
        private readonly ILoggerFactory factory;
        private readonly DMConnection connection;

        public IMediaOpsPlanApi Api { get; private set; }

        public IntegrationTestContext()
        {
            var credentials = CredentialCache.DefaultNetworkCredentials;

            var config = Config.Load();

            connection = ConnectionSettings.GetConnection(config.BaseUrl) ?? throw new NullReferenceException("Unable to connect to DataMiner");
            connection.Authenticate(credentials.UserName, credentials.Password, credentials.Domain);
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            factory = serviceProvider.GetService<ILoggerFactory>() ?? throw new NullReferenceException("Unable to create factory logger");
            var logger = factory.CreateLogger<IMediaOpsPlanApi>();

            Api = new MediaOpsPlanApi(connection, logger) ?? throw new NullReferenceException("Unable to create MediaOpsPlanApi");
        }

        public void Dispose()
        {
            Api.Dispose();
            factory.Dispose();
            connection.Dispose();
        }
    }
}
