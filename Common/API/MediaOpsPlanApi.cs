namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

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
		private readonly IConnection connection;
		private readonly ILogger logger;

		private readonly DomHelpers domHelpers;
		private readonly CoreHelpers coreHelpers;

		private readonly Lazy<IDms> lazyDms;
		private readonly Lazy<IResourcePoolsRepository> lazyResourcePoolsRepository;

		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsPlanApi"/> class.
		/// </summary>
		/// <param name="connection">The connection to use for API operations.</param>
		/// <param name="logger">The logger to use for logging operations.</param>
		public MediaOpsPlanApi(IConnection connection, ILogger logger = null)
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

		internal ILogger Logger => logger;

		internal DomHelpers DomHelpers => domHelpers;

		internal CoreHelpers CoreHelpers => coreHelpers;

		internal IDms Dms => lazyDms.Value;
	}
}
