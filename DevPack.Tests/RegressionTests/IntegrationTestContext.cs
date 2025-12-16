namespace RT_MediaOps.Plan.RegressionTests
{
    using System.Net;
    using System.Reflection;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using DMConnection = Skyline.DataMiner.Net.Connection;

    public sealed class IntegrationTestContext : IDisposable
    {
        private readonly ILoggerFactory factory;
        private readonly DMConnection connection;

        public IntegrationTestContext()
        {
            var config = Config.Load();

            connection = ConnectionSettings.GetConnection(config.BaseUrl) ?? throw new NullReferenceException("Unable to connect to DataMiner");
            connection.Authenticate(config.Username, config.Password, config.Domain);
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            factory = serviceProvider.GetService<ILoggerFactory>() ?? throw new NullReferenceException("Unable to create factory logger");
            var logger = factory.CreateLogger<IMediaOpsPlanApi>();

            Api = new MediaOpsPlanApi(connection, logger) ?? throw new NullReferenceException("Unable to create MediaOpsPlanApi");

            ResourceStudioDomHelper = new DomHelper(connection.HandleMessages, "(slc)resource_studio") ?? throw new NullReferenceException("Unable to create ResourceStudioDomHelper");

            ResourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage) ?? throw new NullReferenceException("Unable to create ResourceManagerHelper");
            ProfileHelper = new ProfileHelper(connection.HandleMessages) ?? throw new NullReferenceException("Unable to create ProfileHelper");
        }

        public IMediaOpsPlanApi Api { get; private set; }

        public DomHelper ResourceStudioDomHelper { get; private set; }

        public ResourceManagerHelper ResourceManagerHelper { get; private set; }

        public ProfileHelper ProfileHelper { get; private set; }

        public void Dispose()
        {
            Api.Dispose();
            factory.Dispose();
            connection.Dispose();
        }
    }
}
