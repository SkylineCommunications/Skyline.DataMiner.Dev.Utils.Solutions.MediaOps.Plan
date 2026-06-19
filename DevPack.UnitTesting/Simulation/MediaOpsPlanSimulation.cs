namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Concatenation;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel.Status;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.Categories.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	using PropertiesDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Definitions;
	using PropertiesSections = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties.SlcPropertiesIds.Sections;
	using ResourceStudioBehaviors = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors;
	using ResourceStudioDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions;
	using ResourceStudioSections = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections;
	using WorkflowBehaviors = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors;
	using WorkflowDefinitions = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions;
	using WorkflowSections = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections;

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

		private const string ResourceStudioModuleId = "(slc)resource_studio";
		private const string WorkflowModuleId = "(slc)workflow";
		private const string PropertiesModuleId = "(slc)properties";

		/// <summary>
		/// The name of the category scope the MediaOps Plan solution uses for resource pools.
		/// </summary>
		private const string ResourcePoolsScopeName = "Resource Pools";

		private const string GenericCameraProtocolName = "Generic Camera";

		/// <summary>
		/// Creates a <see cref="SimulatedDms"/> configured for the MediaOps Plan solution.
		/// </summary>
		/// <returns>The configured <see cref="SimulatedDms"/>.</returns>
		public static SimulatedDms Create()
		{
			var dms = new SimulatedDms();

			dms.AddElement(LockManagerElementName);

			RegisterResourceStudioModule(dms);
			RegisterWorkflowModule(dms);
			RegisterPropertiesModule(dms);
			RegisterCategoriesModule(dms);
			RegisterProtocols(dms);

			SeedSampleData(dms);

			return dms;
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
			};

			var recurringJob = new DomInstance
			{
				ID = new DomInstanceId(Guid.NewGuid()) { ModuleId = WorkflowModuleId },
				DomDefinitionId = WorkflowDefinitions.RecurringJobs,
			};
			recurringJob.Sections.Add(jobInfo.ToSection());
			dms.CreateDomInstance(recurringJob);
		}

		private static DomInstance ToModuleInstance(DomInstance instance, string moduleId)
		{
			instance.ID = new DomInstanceId(instance.ID.Id) { ModuleId = moduleId };
			return instance;
		}

		private static void RegisterResourceStudioModule(SimulatedDms dms)
		{
			var resourcePoolBehavior = BuildBehavior(
				ResourceStudioBehaviors.Resourcepool_Behavior.Id,
				ResourceStudioBehaviors.Resourcepool_Behavior.Statuses.Draft,
				new DomStatusTransition(ResourceStudioBehaviors.Resourcepool_Behavior.Transitions.Draft_To_Complete, ResourceStudioBehaviors.Resourcepool_Behavior.Statuses.Draft, ResourceStudioBehaviors.Resourcepool_Behavior.Statuses.Complete),
				new DomStatusTransition(ResourceStudioBehaviors.Resourcepool_Behavior.Transitions.Complete_To_Deprecated, ResourceStudioBehaviors.Resourcepool_Behavior.Statuses.Complete, ResourceStudioBehaviors.Resourcepool_Behavior.Statuses.Deprecated));

			var resourceBehavior = BuildBehavior(
				ResourceStudioBehaviors.Resource_Behavior.Id,
				ResourceStudioBehaviors.Resource_Behavior.Statuses.Draft,
				new DomStatusTransition(ResourceStudioBehaviors.Resource_Behavior.Transitions.Draft_To_Complete, ResourceStudioBehaviors.Resource_Behavior.Statuses.Draft, ResourceStudioBehaviors.Resource_Behavior.Statuses.Complete),
				new DomStatusTransition(ResourceStudioBehaviors.Resource_Behavior.Transitions.Complete_To_Deprecated, ResourceStudioBehaviors.Resource_Behavior.Statuses.Complete, ResourceStudioBehaviors.Resource_Behavior.Statuses.Deprecated),
				new DomStatusTransition(ResourceStudioBehaviors.Resource_Behavior.Transitions.Deprecated_To_Complete, ResourceStudioBehaviors.Resource_Behavior.Statuses.Deprecated, ResourceStudioBehaviors.Resource_Behavior.Statuses.Complete));

			var definitions = new List<DomDefinition>
			{
				BuildDefinition(ResourceStudioDefinitions.Resourcepool, ResourceStudioBehaviors.Resourcepool_Behavior.Id, ResourceStudioSections.ResourcePoolInfo.Name),
				BuildDefinition(ResourceStudioDefinitions.Resource, ResourceStudioBehaviors.Resource_Behavior.Id, ResourceStudioSections.ResourceInfo.Name),
			};

			dms.RegisterDomModule(ResourceStudioModuleId, definitions, new[] { resourcePoolBehavior, resourceBehavior });
		}

		private static void RegisterWorkflowModule(SimulatedDms dms)
		{
			var workflowBehavior = BuildBehavior(
				WorkflowBehaviors.Workflow_Behavior.Id,
				WorkflowBehaviors.Workflow_Behavior.Statuses.Draft,
				new DomStatusTransition(WorkflowBehaviors.Workflow_Behavior.Transitions.Draft_To_Complete, WorkflowBehaviors.Workflow_Behavior.Statuses.Draft, WorkflowBehaviors.Workflow_Behavior.Statuses.Complete));

			var recurringJobBehavior = BuildBehavior(
				WorkflowBehaviors.Recurringjob_Behavior.Id,
				WorkflowBehaviors.Recurringjob_Behavior.Statuses.Active,
				new DomStatusTransition(WorkflowBehaviors.Recurringjob_Behavior.Transitions.Active_To_Completed, WorkflowBehaviors.Recurringjob_Behavior.Statuses.Active, WorkflowBehaviors.Recurringjob_Behavior.Statuses.Completed),
				new DomStatusTransition(WorkflowBehaviors.Recurringjob_Behavior.Transitions.Active_To_Cancelled, WorkflowBehaviors.Recurringjob_Behavior.Statuses.Active, WorkflowBehaviors.Recurringjob_Behavior.Statuses.Cancelled));

			var jobBehavior = BuildBehavior(
				WorkflowBehaviors.Job_Behavior.Id,
				WorkflowBehaviors.Job_Behavior.Statuses.Draft,
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Draft_To_Tentative, WorkflowBehaviors.Job_Behavior.Statuses.Draft, WorkflowBehaviors.Job_Behavior.Statuses.Tentative),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Tentative_To_Confirmed, WorkflowBehaviors.Job_Behavior.Statuses.Tentative, WorkflowBehaviors.Job_Behavior.Statuses.Confirmed),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Confirmed_To_Running, WorkflowBehaviors.Job_Behavior.Statuses.Confirmed, WorkflowBehaviors.Job_Behavior.Statuses.Running),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Running_To_Completed, WorkflowBehaviors.Job_Behavior.Statuses.Running, WorkflowBehaviors.Job_Behavior.Statuses.Completed),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Confirmed_To_Canceled, WorkflowBehaviors.Job_Behavior.Statuses.Confirmed, WorkflowBehaviors.Job_Behavior.Statuses.Canceled),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Tentative_To_Canceled, WorkflowBehaviors.Job_Behavior.Statuses.Tentative, WorkflowBehaviors.Job_Behavior.Statuses.Canceled),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Draft_To_Completed, WorkflowBehaviors.Job_Behavior.Statuses.Draft, WorkflowBehaviors.Job_Behavior.Statuses.Completed),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Tentative_To_Completed, WorkflowBehaviors.Job_Behavior.Statuses.Tentative, WorkflowBehaviors.Job_Behavior.Statuses.Completed),
				new DomStatusTransition(WorkflowBehaviors.Job_Behavior.Transitions.Confirmed_To_Tentative, WorkflowBehaviors.Job_Behavior.Statuses.Confirmed, WorkflowBehaviors.Job_Behavior.Statuses.Tentative));

			var definitions = new List<DomDefinition>
			{
				BuildDefinition(WorkflowDefinitions.Workflows, WorkflowBehaviors.Workflow_Behavior.Id, WorkflowSections.WorkflowInfo.WorkflowName),
				BuildDefinition(WorkflowDefinitions.RecurringJobs, WorkflowBehaviors.Recurringjob_Behavior.Id, WorkflowSections.JobInfo.JobName),
				BuildDefinition(WorkflowDefinitions.Jobs, WorkflowBehaviors.Job_Behavior.Id, WorkflowSections.JobInfo.JobName),
			};

			dms.RegisterDomModule(WorkflowModuleId, definitions, new[] { workflowBehavior, recurringJobBehavior, jobBehavior });
		}

		private static void RegisterCategoriesModule(SimulatedDms dms)
		{
			var connection = dms.CreateConnection();
			var categoriesApi = connection.GetCategoriesApi();

			categoriesApi.InstallDomModules();

			categoriesApi.Scopes.Create(new Scope { Name = ResourcePoolsScopeName });
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

		private static void RegisterPropertiesModule(SimulatedDms dms)
		{
			var definitions = new List<DomDefinition>
			{
				BuildDefinition(PropertiesDefinitions.Property, null, PropertiesSections.PropertyInfo.Name),
				BuildDefinition(PropertiesDefinitions.PropertyValues, null),
			};

			dms.RegisterDomModule(PropertiesModuleId, definitions, new DomBehaviorDefinition[0]);
		}

		private static DomDefinition BuildDefinition(DomDefinitionId definitionId, DomBehaviorDefinitionId behaviorDefinitionId, FieldDescriptorID nameFieldId = null)
		{
			var definition = new DomDefinition
			{
				ID = definitionId,
				DomBehaviorDefinitionId = behaviorDefinitionId,
			};

			if (nameFieldId != null)
			{
				definition.ModuleSettingsOverrides = new ModuleSettingsOverrides
				{
					NameDefinition = new DomInstanceNameDefinition
					{
						ConcatenationItems = new List<IDomInstanceConcatenationItem>
						{
							new FieldValueConcatenationItem(nameFieldId),
						},
					},
				};
			}

			return definition;
		}

		private static DomBehaviorDefinition BuildBehavior(DomBehaviorDefinitionId behaviorDefinitionId, string initialStatusId, params DomStatusTransition[] transitions)
		{
			return new DomBehaviorDefinition
			{
				ID = behaviorDefinitionId,
				InitialStatusId = initialStatusId,
				StatusTransitions = new List<DomStatusTransition>(transitions),
			};
		}
	}
}
