namespace RT_MediaOps.Plan.Simulation
{
	using RT_MediaOps.Plan.Workflow.Jobs;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[TestClass]
	[TestCategory("UnitTest")]
	public sealed class MediaOpsPlanSimulationTests
	{
		[TestMethod]
		[DataRow("(slc)people_organizations", 13, 34, 3, 6)]
		[DataRow("(slc)properties", 5, 17, 0, 2)]
		[DataRow("(slc)relationships", 2, 11, 0, 2)]
		[DataRow("(slc)resource_studio", 23, 62, 2, 5)]
		[DataRow("(slc)workflow", 20, 135, 3, 8)]
		public void Create_InstallsTheCompleteProductionSchema(string moduleId, int sections, int fields, int behaviors, int definitions)
		{
			var connection = MediaOpsPlanSimulation.Create().CreateConnection();
			var settings = new ModuleSettingsHelper(connection.HandleMessages);
			var helper = new DomHelper(connection.HandleMessages, moduleId);

			Assert.AreEqual(1, settings.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal(moduleId)).Count);
			var installedSections = helper.SectionDefinitions.ReadAll();
			Assert.AreEqual(sections, installedSections.Count);
			Assert.AreEqual(fields, installedSections.Sum(x => x.GetAllFieldDescriptors().Count()));
			Assert.AreEqual(behaviors, helper.DomBehaviorDefinitions.ReadAll().Count);
			Assert.AreEqual(definitions, helper.DomDefinitions.ReadAll().Count);
		}

		[TestMethod]
		public void Create_SeedsInstancesUsingInstalledNamesAndInitialStatuses()
		{
			var connection = MediaOpsPlanSimulation.Create().CreateConnection();
			var workflowHelper = new DomHelper(connection.HandleMessages, SlcWorkflowIds.ModuleId);
			var instances = workflowHelper.DomInstances.ReadAll();
			var workflow = instances.Single(x => x.DomDefinitionId.Equals(SlcWorkflowIds.Definitions.Workflows));
			var recurringJob = instances.Single(x => x.DomDefinitionId.Equals(SlcWorkflowIds.Definitions.RecurringJobs));

			Assert.AreEqual("Sample Workflow", workflow.Name);
			Assert.AreEqual(SlcWorkflowIds.Behaviors.Workflow_Behavior.Statuses.Draft, workflow.StatusId);
			Assert.AreEqual("Sample Recurring Job", recurringJob.Name);
			Assert.AreEqual(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Statuses.Active, recurringJob.StatusId);

			var relationshipsHelper = new DomHelper(connection.HandleMessages, SlcRelationshipsIds.ModuleId);
			Assert.AreEqual("Job", relationshipsHelper.DomInstances.ReadAll().Single().Name);
		}

		[TestMethod]
		public void ReinstallingModules_UsesEqualDeclarationsAndPreservesSeededInstances()
		{
			var connection = MediaOpsPlanSimulation.Create().CreateConnection();
			var helper = new DomHelper(connection.HandleMessages, SlcWorkflowIds.ModuleId);
			var before = helper.DomInstances.ReadAll().OrderBy(x => x.ID.Id).Select(x => x.ToJson()).ToArray();
			var log = new List<string>();
			var api = connection.GetMediaOpsPlanApi();
			api.SetLogger(new CallbackLogger(log.Add));

			api.InstallDomModules();

			Assert.AreEqual(99, log.Count(x => x.StartsWith("Skipping ", StringComparison.Ordinal)));
			Assert.IsFalse(log.Any(x => x.StartsWith("Creating ", StringComparison.Ordinal) || x.StartsWith("Updating ", StringComparison.Ordinal)));
			CollectionAssert.AreEqual(before, helper.DomInstances.ReadAll().OrderBy(x => x.ID.Id).Select(x => x.ToJson()).ToArray());
		}

		[TestMethod]
		public void Create_WithMediaOpsLive_InstallsItsModulesAndSeedsLevels()
		{
			var connection = MediaOpsPlanSimulation.Create(installMediaOpsLive: true).CreateConnection();
			var settings = new ModuleSettingsHelper(connection.HandleMessages);

			Assert.AreEqual(1, settings.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal("(slc)connectivity_management")).Count);
			Assert.AreEqual(1, settings.ModuleSettings.Read(ModuleSettingsExposers.ModuleId.Equal("(slc)orchestration")).Count);
			var helper = new DomHelper(connection.HandleMessages, "(slc)connectivity_management");
			var levelDefinition = helper.DomDefinitions.ReadAll().Single(x => x.Name == "Level");
			Assert.AreEqual(3, helper.DomInstances.ReadAll().Count(x => x.DomDefinitionId.Equals(levelDefinition.ID)));
		}
	}
}
