namespace RT_MediaOps.Plan.RegressionTests
{
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Utils.Categories.API;

    using DMConnection = Skyline.DataMiner.Net.Connection;

    public sealed class IntegrationTestContext : IDisposable
    {
        private readonly DMConnection connection;

        public IntegrationTestContext()
        {
            var config = Config.Load();

            connection = Skyline.DataMiner.Net.ConnectionSettings.GetConnection(config.BaseUrl) ?? throw new NullReferenceException("Unable to connect to DataMiner");
            connection.Authenticate(config.Username, config.Password, config.Domain);

            Api = new MediaOpsPlanApi(connection) ?? throw new NullReferenceException("Unable to create MediaOpsPlanApi");
            Dms = connection.GetDms() ?? throw new NullReferenceException("Unable to get DMS");

            ResourceStudioDomHelper = new DomHelper(connection.HandleMessages, "(slc)resource_studio") ?? throw new NullReferenceException("Unable to create ResourceStudioDomHelper");

            ResourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage) ?? throw new NullReferenceException("Unable to create ResourceManagerHelper");
            ProfileHelper = new ProfileHelper(connection.HandleMessages) ?? throw new NullReferenceException("Unable to create ProfileHelper");
            CategoriesApi = new CategoriesApi(connection) ?? throw new NullReferenceException("Unable to create CategoriesApi");
            ProtocolFunctionHelper = new ProtocolFunctionHelper(connection.HandleMessages) ?? throw new NullReferenceException("Unable to create ProtocolFunctionHelper");
        }

        public IMediaOpsPlanApi Api { get; private set; }

        public IDms Dms { get; private set; }

        public DomHelper ResourceStudioDomHelper { get; private set; }

        public ResourceManagerHelper ResourceManagerHelper { get; private set; }

        public ProfileHelper ProfileHelper { get; private set; }

        public CategoriesApi CategoriesApi { get; private set; }

        public ProtocolFunctionHelper ProtocolFunctionHelper { get; private set; }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
