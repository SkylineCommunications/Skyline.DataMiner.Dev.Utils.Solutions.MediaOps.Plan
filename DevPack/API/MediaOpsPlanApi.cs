namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM.Registration;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Logging;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	/// <summary>
	/// Provides the main entry point for interacting with the MediaOps Plan API.
	/// </summary>
	public class MediaOpsPlanApi : IMediaOpsPlanApi
	{
		private const string CatalogItemId = "1b67a623-4ca6-4d25-8b3d-ed4e39496a75";

		private readonly IConnection connection;

		private readonly DomHelpers domHelpers;
		private readonly CoreHelpers coreHelpers;

		private readonly Lazy<IDms> lazyDms;
		private readonly Lazy<IMediaOpsLiveApi> lazyLiveApi;
		private readonly Lazy<IResourcesRepository> lazyResourceRepository;
		private readonly Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;
		private readonly Lazy<ICapabilitiesRepository> lazyCapabilitiesRepository;
		private readonly Lazy<ICapacitiesRepository> lazyCapacitiesRepository;
		private readonly Lazy<IConfigurationsRepository> lazyConfigurationsRepository;
		private readonly Lazy<IResourcePropertiesRepository> lazyResourcePropertiesRepository;
		private readonly Lazy<IJobsRepository> lazyJobsRepository;
		private readonly Lazy<IWorkflowsRepository> lazyWorkflowsRepository;
		private readonly Lazy<IRecurringJobsRepository> lazyRecurringJobsRepository;
		private readonly Lazy<IPropertiesRepository> lazyPropertiesRepository;
		private readonly Lazy<IPropertyValueCollectionsRepository> lazyPropertyValueCollectionsRepository;
		private readonly Lazy<Plan.Tools.LockManager> lazyLockManager;
		private readonly Lazy<ICategoriesApi> lazyCategoriesApi;

		internal static readonly int DefaultPageSize = 200;

		private ILogger logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsPlanApi"/> class.
		/// </summary>
		/// <param name="connection">The connection to use for API operations.</param>
		internal MediaOpsPlanApi(IConnection connection)
		{
			this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.logger = new NullLogger();

			domHelpers = new DomHelpers(connection);
			coreHelpers = new CoreHelpers(connection);

			lazyDms = new Lazy<IDms>(() => connection.GetDms());
			lazyLiveApi = new Lazy<IMediaOpsLiveApi>(() => connection.GetMediaOpsLiveApi());
			lazyResourceRepository = new Lazy<IResourcesRepository>(() => new ResourcesRepository(this));
			lazyResourcePoolsRepository = new Lazy<IResourcePoolsRepository>(() => new ResourcePoolsRepository(this));
			lazyCapabilitiesRepository = new Lazy<ICapabilitiesRepository>(() => new CapabilitiesRepository(this));
			lazyCapacitiesRepository = new Lazy<ICapacitiesRepository>(() => new CapacitiesRepository(this));
			lazyConfigurationsRepository = new Lazy<IConfigurationsRepository>(() => new ConfigurationsRepository(this));
			lazyResourcePropertiesRepository = new Lazy<IResourcePropertiesRepository>(() => new ResourcePropertiesRepository(this));
			lazyJobsRepository = new Lazy<IJobsRepository>(() => new JobsRepository(this));
			lazyWorkflowsRepository = new Lazy<IWorkflowsRepository>(() => new WorkflowsRepository(this));
			lazyRecurringJobsRepository = new Lazy<IRecurringJobsRepository>(() => new RecurringJobsRepository(this));
			lazyPropertiesRepository = new Lazy<IPropertiesRepository>(() => new PropertiesRepository(this));
			lazyPropertyValueCollectionsRepository = new Lazy<IPropertyValueCollectionsRepository>(() => new PropertyValueCollectionsRepository(this));
			lazyLockManager = new Lazy<Plan.Tools.LockManager>(() => new Plan.Tools.LockManager(this));
			lazyCategoriesApi = new Lazy<ICategoriesApi>(() => connection.GetCategoriesApi());
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

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public IPropertiesRepository Properties => lazyPropertiesRepository.Value;

		/// <summary>
		/// <inheritdoc/>
		/// </summary>
		public IPropertyValueCollectionsRepository PropertyValueCollections => lazyPropertyValueCollectionsRepository.Value;

		internal IConnection Connection => connection;

		internal ILogger Logger => logger;

		internal DomHelpers DomHelpers => domHelpers;

		internal CoreHelpers CoreHelpers => coreHelpers;

		internal IDms Dms => lazyDms.Value;

		internal IMediaOpsLiveApi LiveApi => lazyLiveApi.Value;

		internal Plan.Tools.LockManager LockManager => lazyLockManager.Value;

		internal ICategoriesApi Categories => lazyCategoriesApi.Value;

		/// <inheritdoc/>
		public void SetLogger(ILogger logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc/>
		public bool IsInstalled(out string version)
		{
			var registrar = Connection.GetSdmRegistrar();
			var mediaOpsPlanRegistration = registrar.Solutions.Read(SolutionRegistrationExposers.ID.Equal(CatalogItemId)).FirstOrDefault();
			if (mediaOpsPlanRegistration == null)
			{
				version = null;
				return false;
			}

			version = mediaOpsPlanRegistration.Version;
			return true;
		}

		/// <inheritdoc/>
		public bool IsInstalled()
		{
			return IsInstalled(out _);
		}
	}
}
