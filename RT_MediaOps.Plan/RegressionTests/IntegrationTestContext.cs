namespace RT_MediaOps.Plan.RegressionTests
{
    using System.Net;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    public sealed class IntegrationTestContext : IDisposable
    {
        private readonly ILoggerFactory factory;
        private readonly DMConnection connection;

        private readonly Lazy<ResourceManagerHelper> lazyResourceManagerHelper;
        private readonly Lazy<ProfileHelper> lazyProfileHelper;

        public IMediaOpsPlanApi Api { get; private set; }

        public ResourceManagerHelper ResourceManagerHelper => lazyResourceManagerHelper.Value;

        public ProfileHelper ProfileHelper => lazyProfileHelper.Value;

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

            lazyResourceManagerHelper = new Lazy<ResourceManagerHelper>(() => new ResourceManagerHelper(connection.HandleSingleResponseMessage));
            lazyProfileHelper = new Lazy<ProfileHelper>(() => new ProfileHelper(connection.HandleMessages));
        }

        public void Dispose()
        {
            Api.Dispose();
            factory.Dispose();
            connection.Dispose();
        }
    }
}
