namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.Plan.Logger;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Net;

    public class MediaOpsPlanApi : IMediaOpsPlanApi
    {
        private readonly IConnection connection;
        private readonly ILogger logger;

        private readonly DomHelpers domHelpers;
        private readonly CoreHelpers coreHelpers;

        private Lazy<IDms> lazyDms;
        private Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;

        public MediaOpsPlanApi(IConnection connection, ILogger logger = null)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.logger = logger ?? new NoLogger();

            domHelpers = new DomHelpers(connection);
            coreHelpers = new CoreHelpers(connection);

            ContextId = Guid.NewGuid();

            lazyDms = new Lazy<IDms>(() => connection.GetDms());
            lazyResourcePoolsRepository = new Lazy<IResourcePoolsRepository>(() => new ResourcePoolsRepository(this));
        }

        public Guid ContextId { get; }

        public IResourcePoolsRepository ResourcePools => lazyResourcePoolsRepository.Value;

        internal IConnection Connection => connection;

        internal ILogger Logger => logger;

        internal DomHelpers DomHelpers => domHelpers;

        internal CoreHelpers CoreHelpers => coreHelpers;

        internal IDms Dms => lazyDms.Value;
    }
}
