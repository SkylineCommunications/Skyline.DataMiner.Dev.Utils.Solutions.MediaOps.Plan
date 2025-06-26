namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;

    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;

    internal class CoreHelpers
    {
        private readonly IConnection connection;

        private Lazy<ResourceManagerHelper> lazyResourceManagerHelper;
        private Lazy<ProfileHelper> lazyProfileHelper;

        public CoreHelpers(IConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            lazyResourceManagerHelper = new Lazy<ResourceManagerHelper>(() => new ResourceManagerHelper(connection.HandleSingleResponseMessage));
            lazyProfileHelper = new Lazy<ProfileHelper>(() => new ProfileHelper(connection.HandleMessages));
        }

        public ResourceManagerHelper ResourceManagerHelper => lazyResourceManagerHelper.Value;

        public ProfileHelper ProfileHelper => lazyProfileHelper.Value;
    }
}
