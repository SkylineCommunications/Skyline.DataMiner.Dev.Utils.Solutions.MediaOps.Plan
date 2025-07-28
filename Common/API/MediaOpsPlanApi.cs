namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.Plan.Logger;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Net;

    /// <summary>
    /// Provides the main entry point for interacting with the MediaOps Plan API.
    /// </summary>
    public class MediaOpsPlanApi : IMediaOpsPlanApi
    {
        private readonly ILogger<IMediaOpsPlanApi> logger;
        private readonly IConnection connection;

        private readonly DomHelpers domHelpers;
        private readonly CoreHelpers coreHelpers;

        private readonly Lazy<IDms> lazyDms;
        private readonly Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;
        private bool disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsPlanApi"/> class.
        /// </summary>
        /// <param name="connection">The connection to use for API operations.</param>
        /// <param name="logger">The logger to use for logging operations.</param>
        public MediaOpsPlanApi(IConnection connection, ILogger<IMediaOpsPlanApi> logger = null)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.logger = logger ?? new DefaultLogger();

            domHelpers = new DomHelpers(connection);
            coreHelpers = new CoreHelpers(connection);

            lazyDms = new Lazy<IDms>(() => connection.GetDms());
            lazyResourcePoolsRepository = new Lazy<IResourcePoolsRepository>(() => new ResourcePoolsRepository(this));
        }

        /// <summary>
        /// Gets the repository for managing resource pools.
        /// </summary>
        public IResourcePoolsRepository ResourcePools => lazyResourcePoolsRepository.Value;

        internal IConnection Connection => connection;

        internal ILogger<IMediaOpsPlanApi> Logger => logger;

        internal DomHelpers DomHelpers => domHelpers;

        internal CoreHelpers CoreHelpers => coreHelpers;

        internal IDms Dms => lazyDms.Value;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && logger is DefaultLogger defaultLogger)
                {
                    defaultLogger.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
