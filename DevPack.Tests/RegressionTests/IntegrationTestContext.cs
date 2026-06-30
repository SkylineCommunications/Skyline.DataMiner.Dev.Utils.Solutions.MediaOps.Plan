namespace RT_MediaOps.Plan.RegressionTests
{
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	public sealed class IntegrationTestContext : IDisposable
	{
		private readonly object connectionLock = new object();
		private readonly Config config;

		private IConnection connection;

		public IntegrationTestContext()
		{
			config = Config.Load();
			Initialize();
		}

		private void Initialize()
		{
			connection = config.UseRealDma
				? CreateRealConnection(config)
				: CreateSimulatedConnection();

			Api = connection.GetMediaOpsPlanApi() ?? throw new NullReferenceException("Unable to create MediaOpsPlanApi");
			Dms = connection.GetDms() ?? throw new NullReferenceException("Unable to get DMS");

			Api.SetLogger(Logger);
			ResourceStudioDomHelper = new DomHelper(connection.HandleMessages, "(slc)resource_studio") ?? throw new NullReferenceException("Unable to create ResourceStudioDomHelper");

			ResourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage) ?? throw new NullReferenceException("Unable to create ResourceManagerHelper");
			ProfileHelper = new ProfileHelper(connection.HandleMessages) ?? throw new NullReferenceException("Unable to create ProfileHelper");
			CategoriesApi = connection.GetCategoriesApi() ?? throw new NullReferenceException("Unable to create CategoriesApi");
			CategoriesApi.SetLogger(new ConsoleCategoriesLogger());
			ProtocolFunctionHelper = new ProtocolFunctionHelper(connection.HandleMessages) ?? throw new NullReferenceException("Unable to create ProtocolFunctionHelper");
		}

		/// <summary>
		/// Verifies that the underlying connection is still usable and recreates it when needed.
		/// A real DataMiner Agent connection can be shut down or lose its authentication during a
		/// long-running test session; when that happens a fresh connection is created and all
		/// connection-bound helpers are re-initialized. The simulated connection holds the in-memory
		/// system state, so it is never recreated.
		/// </summary>
		public void EnsureConnected()
		{
			lock (connectionLock)
			{
				if (IsConnectionHealthy())
				{
					return;
				}

				connection.Dispose();
				Initialize();
			}
		}

		private bool IsConnectionHealthy()
		{
			// The simulated connection holds the in-memory system state, never shuts down and has no
			// authentication, so it must never be recreated.
			if (!config.UseRealDma)
			{
				return true;
			}

			return connection is Skyline.DataMiner.Net.Connection realConnection
				&& !realConnection.IsShuttingDown
				&& realConnection.IsAuthenticated;
		}

		private static IConnection CreateRealConnection(Config config)
		{
			var connection = Skyline.DataMiner.Net.ConnectionSettings.GetConnection(config.BaseUrl)
				?? throw new NullReferenceException("Unable to connect to DataMiner");

			connection.Authenticate(config.Username, config.Password, config.Domain);

			return connection;
		}

		private static IConnection CreateSimulatedConnection()
		{
			var dms = MediaOpsPlanSimulation.Create();
			return dms.CreateConnection();
		}


		public IMediaOpsPlanApi Api { get; private set; }

		public IDms Dms { get; private set; }

		public ConsolePlanLogger Logger { get; private set; } = new ConsolePlanLogger();

		public DomHelper ResourceStudioDomHelper { get; private set; }

		public ResourceManagerHelper ResourceManagerHelper { get; private set; }

		public ProfileHelper ProfileHelper { get; private set; }

		public ICategoriesApi CategoriesApi { get; private set; }

		public ProtocolFunctionHelper ProtocolFunctionHelper { get; private set; }

		/// <summary>
		/// Reads the current job settings DOM instance and captures it as a <see cref="JobSettingsSnapshot"/>
		/// that can later be passed to <see cref="RestoreJobSettings"/>.
		/// </summary>
		internal JobSettingsSnapshot CreateJobSettingsSnapshot()
		{
			var instance = GetJobSettingsInstance();
			return JobSettingsSnapshot.FromDom(instance);
		}

		/// <summary>
		/// Restores the job settings DOM instance to the values captured in the given <paramref name="snapshot"/>.
		/// Writes directly to the DOM so all properties (including those not exposed by the public API,
		/// such as <c>JobIDNextSequence</c>) are restored.
		/// </summary>
		internal void RestoreJobSettings(JobSettingsSnapshot snapshot)
		{
			if (snapshot == null)
			{
				throw new ArgumentNullException(nameof(snapshot));
			}

			var planApi = (MediaOpsPlanApi)Api;
			var instance = GetJobSettingsInstance();
			snapshot.ApplyTo(instance);
			planApi.DomHelpers.SlcWorkflowHelper.DomHelper.DomInstances.Update(instance.ToInstance());
		}

		private AppSettingsInstance GetJobSettingsInstance()
		{
			var planApi = (MediaOpsPlanApi)Api;
			var instance = planApi.DomHelpers.SlcWorkflowHelper
				.GetAppSettings(DomInstanceExposers.Id.Equal(DomJobSettingHandler.JobSettingId))
				.SingleOrDefault()
				?? throw new InvalidOperationException("Job settings DOM instance not found.");
			return instance;
		}

		public void Dispose()
		{
			connection.Dispose();
		}
	}
}
