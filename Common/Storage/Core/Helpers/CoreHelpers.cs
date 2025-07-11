namespace Skyline.DataMiner.MediaOps.Plan.Storage.Core
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Profiles;

    internal class CoreHelpers
    {
        private readonly IConnection connection;

        private readonly Lazy<ResourceManagerHelper> lazyResourceManagerHelper;
        private readonly Lazy<ProfileHelper> lazyProfileHelper;

        private readonly Lazy<ProtocolFunctionHelper> lazyProtocolFunctionHelper;
        private readonly Lazy<ProtocolFunctionHelperCache> lazyProtocolFunctionHelperCache;

        private readonly Lazy<IDms> lazyDms;
        private readonly Lazy<DataMinerSystemCache> lazyDataMinerSystemCache;

        public CoreHelpers(IConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));

            lazyResourceManagerHelper = new Lazy<ResourceManagerHelper>(() => new ResourceManagerHelper(connection.HandleSingleResponseMessage));
            lazyProfileHelper = new Lazy<ProfileHelper>(() => new ProfileHelper(connection.HandleMessages));

            lazyProtocolFunctionHelper = new Lazy<ProtocolFunctionHelper>(() => new ProtocolFunctionHelper(connection.HandleMessages));
            lazyProtocolFunctionHelperCache = new Lazy<ProtocolFunctionHelperCache>(() => new ProtocolFunctionHelperCache(ProtocolFunctionHelper));

            lazyDms = new Lazy<IDms>(() => connection.GetDms());
            lazyDataMinerSystemCache = new Lazy<DataMinerSystemCache>(() => new DataMinerSystemCache(Dms));
        }

        public ResourceManagerHelper ResourceManagerHelper => lazyResourceManagerHelper.Value;

        public ProfileHelper ProfileHelper => lazyProfileHelper.Value;

        public ProtocolFunctionHelper ProtocolFunctionHelper => lazyProtocolFunctionHelper.Value;

        public ProtocolFunctionHelperCache ProtocolFunctionHelperCache => lazyProtocolFunctionHelperCache.Value;

        public IDms Dms => lazyDms.Value;

        public DataMinerSystemCache DmsCache => lazyDataMinerSystemCache.Value;
    }
}
