namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.SDM.Registration;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using LiveConstants = Skyline.DataMiner.Solutions.MediaOps.Live.Constants;
	using WorkflowDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions;

	/// <summary>
	/// Provides a pre-configured <see cref="SimulatedDms"/> that mirrors the installed state a real
	/// DataMiner Agent has when the MediaOps Plan solution is deployed (required elements, DOM module
	/// definitions, behaviors and initial statuses).
	/// </summary>
	public static class MediaOpsPlanSimulation
	{
		/// <summary>
		/// The name of the lock manager element the MediaOps Plan solution communicates with.
		/// </summary>
		public const string LockManagerElementName = "MOP Lock Manager";

		/// <summary>
		/// The catalog item ID the MediaOps Live solution is registered with in the SDM registrar.
		/// </summary>
		public const string MediaOpsLiveSolutionId = "213031b9-af0b-488c-be20-934912b967c0";

		/// <summary>
		/// The folder the MediaOps Live orchestration scripts are stored in.
		/// </summary>
		public const string OrchestrationScriptFolder = "MediaOps/OrchestrationScripts";

		private const string WorkflowModuleId = "(slc)workflow";
		private const string RelationshipsModuleId = "(slc)relationships";

		private const string GenericCameraProtocolName = "Generic Camera";

		/// <summary>
		/// Creates a <see cref="SimulatedDms"/> configured for the MediaOps Plan solution.
		/// </summary>
		/// <param name="installMediaOpsLive">
		/// When <see langword="true"/>, the MediaOps Live solution is installed as well (solution registration, DOM
		/// modules, orchestration scripts and connectivity levels), so the Plan solution pushes its orchestration
		/// configuration to MediaOps Live.
		/// </param>
		/// <returns>The configured <see cref="SimulatedDms"/>.</returns>
		public static SimulatedDms Create(bool installMediaOpsLive = false)
		{
			var dms = new SimulatedDms();

			dms.AddElement(LockManagerElementName);

			dms.CreateConnection().GetMediaOpsPlanApi().InstallDomModules();
			RegisterCategoriesModule(dms);
			RegisterProtocols(dms);

			SeedSampleData(dms);

			if (installMediaOpsLive)
			{
				InstallMediaOpsLive(dms);
			}

			return dms;
		}

		private static void InstallMediaOpsLive(SimulatedDms dms)
		{
			var connection = dms.CreateConnection();
			var version = MediaOpsLiveApi.GetVersion();

			dms.AddApplicationPackage(LiveConstants.AppPackageName, version);

			// The Plan solution only pushes orchestration configuration when MediaOps Live is registered in the SDM registrar.
			connection.GetSdmRegistrar().Solutions.CreateOrUpdate(new[]
			{
				new SolutionRegistration
				{
					ID = MediaOpsLiveSolutionId,
					DisplayName = "MediaOps Live",
					Version = version,
				},
			});

			dms.AddScript(LiveConstants.OrchestrationScriptName, OrchestrationScriptFolder);
			dms.AddScript(LiveConstants.OrchestrationSlidingWindowSchedulerScriptName, OrchestrationScriptFolder);

			var liveApi = connection.GetMediaOpsLiveApi();
			liveApi.InstallDomModules();

			SeedMediaOpsLiveLevels(liveApi);
		}

		private static void SeedMediaOpsLiveLevels(IMediaOpsLiveApi liveApi)
		{
			var transportType = liveApi.TransportTypes.Create(new TransportType { Name = "TSoIP" });

			liveApi.Levels.CreateOrUpdate(new[]
			{
				new Level { Number = 1, Name = "Video", TransportType = transportType },
				new Level { Number = 2, Name = "Audio", TransportType = transportType },
				new Level { Number = 3, Name = "Data", TransportType = transportType },
			});
		}

		private static void SeedSampleData(SimulatedDms dms)
		{
			var workflow = new WorkflowsInstance();
			workflow.WorkflowInfo.WorkflowName = "Sample Workflow";
			dms.CreateDomInstance(ToModuleInstance(workflow.ToInstance(), WorkflowModuleId));

			var jobInfo = new JobInfoSection
			{
				JobName = "Sample Recurring Job",
				JobStart = DateTime.UtcNow,
				JobEnd = DateTime.UtcNow.AddHours(1),
				JobDuration = TimeSpan.FromHours(1),
			};

			var recurringJob = new DomInstance
			{
				ID = new DomInstanceId(Guid.NewGuid()) { ModuleId = WorkflowModuleId },
				DomDefinitionId = WorkflowDefinitions.RecurringJobs,
			};
			recurringJob.Sections.Add(jobInfo.ToSection());
			dms.CreateDomInstance(recurringJob);

			// The solution seeds this reserved object type during setup; jobs cannot be linked to anything without it.
			var jobObjectType = new ObjectTypesInstance();
			jobObjectType.ObjectTypeInfo.ObjectName = "Job";
			dms.CreateDomInstance(ToModuleInstance(jobObjectType.ToInstance(), RelationshipsModuleId));
		}

		private static DomInstance ToModuleInstance(DomInstance instance, string moduleId)
		{
			instance.ID = new DomInstanceId(instance.ID.Id) { ModuleId = moduleId };
			return instance;
		}

		private static void RegisterCategoriesModule(SimulatedDms dms)
		{
			var connection = dms.CreateConnection();
			var categoriesApi = connection.GetCategoriesApi();

			categoriesApi.InstallDomModules();

			categoriesApi.Scopes.Create(new Scope { Name = CategoryScopes.ResourcePools });
			categoriesApi.Scopes.Create(new Scope { Name = CategoryScopes.JobTypes });
		}

		private static void RegisterProtocols(SimulatedDms dms)
		{
			dms.RegisterProtocol(GenericCameraProtocolName, "Production", ProtocolType.Virtual, "HTTP");

			var functionDefinition = new FunctionDefinition
			{
				GUID = Guid.NewGuid(),
				Name = GenericCameraProtocolName,
				EntryPoints = Array.Empty<FunctionEntryPointDefinition>(),
			};

			dms.RegisterProtocolFunction(GenericCameraProtocolName, functionDefinition);
		}
	}
}
