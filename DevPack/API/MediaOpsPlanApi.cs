namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.Live.API;
    using Skyline.DataMiner.MediaOps.Live.API.Extensions;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Logger;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;
    using Skyline.DataMiner.Utils.Categories.API;

    /// <summary>
    /// Provides the main entry point for interacting with the MediaOps Plan API.
    /// </summary>
    public class MediaOpsPlanApi : IMediaOpsPlanApi
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<IMediaOpsPlanApi> logger;
        private readonly IConnection connection;

        private readonly InstalledAppPackageCache installedAppPackages;
        private readonly DomHelpers domHelpers;
        private readonly CoreHelpers coreHelpers;

        private readonly Lazy<IDms> lazyDms;
        private readonly Lazy<MediaOpsLiveApi> lazyLiveApi;
        private readonly Lazy<IResourcesRepository> lazyResourceRepository;
        private readonly Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;
        private readonly Lazy<ICapabilitiesRepository> lazyCapabilitiesRepository;
        private readonly Lazy<ICapacitiesRepository> lazyCapacitiesRepository;
        private readonly Lazy<IConfigurationsRepository> lazyConfigurationsRepository;
        private readonly Lazy<IResourcePropertiesRepository> lazyResourcePropertiesRepository;
        private readonly Lazy<IJobsRepository> lazyJobsRepository;
        private readonly Lazy<IWorkflowsRepository> lazyWorkflowsRepository;
        private readonly Lazy<IRecurringJobsRepository> lazyRecurringJobsRepository;
        private readonly Lazy<Plan.Tools.LockManager> lazyLockManager;
        private readonly Lazy<CategoriesApi> lazyCategoriesApi;
        private bool disposedValue;

        internal static readonly int DefaultPageSize = 200;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsPlanApi"/> class.
        /// </summary>
        /// <param name="connection">The connection to use for API operations.</param>
        /// <param name="loggerFactory">The logger to use for logging operations.</param>
        public MediaOpsPlanApi(IConnection connection, ILoggerFactory loggerFactory = null)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.loggerFactory = loggerFactory ?? new NullLoggerFactory();
            this.logger = this.loggerFactory.CreateLogger<IMediaOpsPlanApi>() ?? new NullLogger<IMediaOpsPlanApi>();

            installedAppPackages = new InstalledAppPackageCache(connection);

            domHelpers = new DomHelpers(connection);
            coreHelpers = new CoreHelpers(connection);

            lazyDms = new Lazy<IDms>(() => connection.GetDms());
            lazyLiveApi = new Lazy<MediaOpsLiveApi>(() => connection.GetMediaOpsLiveApi());
            lazyResourceRepository = new Lazy<IResourcesRepository>(() => new ResourcesRepository(this));
            lazyResourcePoolsRepository = new Lazy<IResourcePoolsRepository>(() => new ResourcePoolsRepository(this));
            lazyCapabilitiesRepository = new Lazy<ICapabilitiesRepository>(() => new CapabilitiesRepository(this));
            lazyCapacitiesRepository = new Lazy<ICapacitiesRepository>(() => new CapacitiesRepository(this));
            lazyConfigurationsRepository = new Lazy<IConfigurationsRepository>(() => new ConfigurationsRepository(this));
            lazyResourcePropertiesRepository = new Lazy<IResourcePropertiesRepository>(() => new ResourcePropertiesRepository(this));
            lazyJobsRepository = new Lazy<IJobsRepository>(() => new JobsRepository(this));
            lazyWorkflowsRepository = new Lazy<IWorkflowsRepository>(() => new WorkflowsRepository(this));
            lazyRecurringJobsRepository = new Lazy<IRecurringJobsRepository>(() => new RecurringJobsRepository(this));
            lazyLockManager = new Lazy<Plan.Tools.LockManager>(() => new Plan.Tools.LockManager(this));
            lazyCategoriesApi = new Lazy<CategoriesApi>(() => new CategoriesApi(connection));
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
        public IResourcePropertiesRepository ResourceProperties => lazyResourcePropertiesRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IJobsRepository Jobs => lazyJobsRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IWorkflowsRepository Workflows => lazyWorkflowsRepository.Value;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IRecurringJobsRepository RecurringJobs => lazyRecurringJobsRepository.Value;

        internal IConnection Connection => connection;

        internal ILogger<IMediaOpsPlanApi> Logger => logger;

        internal ILoggerFactory LoggerFactory => loggerFactory;

        internal DomHelpers DomHelpers => domHelpers;

        internal CoreHelpers CoreHelpers => coreHelpers;

        internal IDms Dms => lazyDms.Value;

        internal MediaOpsLiveApi LiveApi => lazyLiveApi.Value;

        internal Plan.Tools.LockManager LockManager => lazyLockManager.Value;

        internal CategoriesApi Categories => lazyCategoriesApi.Value;

        /// <inheritdoc/>
        public bool IsInstalled(out string version)
        {
            var isInstalled = installedAppPackages.IsInstalled("SLC-S-MediaOps", out var installedAppInfo);
            version = isInstalled ? installedAppInfo?.AppInfo?.Version : null;
            return isInstalled;
        }

        /// <inheritdoc/>
        public bool IsInstalled()
        {
            return IsInstalled(out _);
        }

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
