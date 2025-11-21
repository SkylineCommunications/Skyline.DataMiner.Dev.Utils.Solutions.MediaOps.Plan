namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Core.InterAppCalls.Common.CallBulk;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Logger;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Shared.Comm.InterApp.Messages;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

    /// <summary>
    /// Provides the main entry point for interacting with the MediaOps Plan API.
    /// </summary>
    public partial class MediaOpsPlanApi : IMediaOpsPlanApi
    {
        private static readonly IEnumerable<Type> knownTypes = new List<Type>
        {
            typeof(IInterAppCall),
            typeof(CreateResourceRequest),
            typeof(CreateResourceResponse),
            typeof(OperationFailedResponse),
            typeof(MediaOpsTraceData),
            typeof(Resource)
        };

        private readonly ILogger<IMediaOpsPlanApi> logger;
        private readonly IConnection connection;

        private readonly DomHelpers domHelpers;
        private readonly CoreHelpers coreHelpers;

        private readonly Lazy<IDms> lazyDms;
        private readonly Lazy<IResourcesRepository> lazyResourceRepository;
        private readonly Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;
        private readonly Lazy<ICapabilitiesRepository> lazyCapabilitiesRepository;
        private readonly Lazy<ICapacitiesRepository> lazyCapacitiesRepository;
        private readonly Lazy<IConfigurationsRepository> lazyConfigurationsRepository;
        private readonly Lazy<IResourcePropertiesRepository> lazyResourcePropertiesRepository;
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
            lazyResourceRepository = new Lazy<IResourcesRepository>(() => new ResourcesRepository(this));
            lazyResourcePoolsRepository = new Lazy<IResourcePoolsRepository>(() => new ResourcePoolsRepository(this));
            lazyCapabilitiesRepository = new Lazy<ICapabilitiesRepository>(() => new CapabilitiesRepository(this));
            lazyCapacitiesRepository = new Lazy<ICapacitiesRepository>(() => new CapacitiesRepository(this));
            lazyConfigurationsRepository = new Lazy<IConfigurationsRepository>(() => new ConfigurationsRepository(this));
            lazyResourcePropertiesRepository = new Lazy<IResourcePropertiesRepository>(() => new ResourcePropertiesRepository(this));

            Init();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IResourcesRepository Resources => lazyResourceRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IResourcePoolsRepository ResourcePools => lazyResourcePoolsRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ICapabilitiesRepository Capabilities => lazyCapabilitiesRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public ICapacitiesRepository Capacities => lazyCapacitiesRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IConfigurationsRepository Configurations => lazyConfigurationsRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IResourcePropertiesRepository Properties => lazyResourcePropertiesRepository.Value;

        internal IConnection Connection => connection;

        internal ILogger<IMediaOpsPlanApi> Logger => logger;

        internal DomHelpers DomHelpers => domHelpers;

        internal CoreHelpers CoreHelpers => coreHelpers;

        internal IDms Dms => lazyDms.Value;

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources.  If the instance holds unmanaged resources, ensure they are properly released by overriding  this
        /// method in a derived class.</remarks>
        /// <param name="disposing"></param>
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

        /// <summary>
        /// Releases the resources used by the current instance of the class.
        /// </summary>
        /// <remarks>This method should be called when the instance is no longer needed to free up
        /// resources.  It suppresses finalization to optimize garbage collection. For custom cleanup logic,  override
        /// the <c>Dispose(bool disposing)</c> method in derived classes.</remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
