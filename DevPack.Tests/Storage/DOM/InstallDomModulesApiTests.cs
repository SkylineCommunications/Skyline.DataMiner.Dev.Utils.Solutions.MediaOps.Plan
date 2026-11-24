namespace RT_MediaOps.Plan.Storage.DOM
{
	using RT_MediaOps.Plan.Workflow.Jobs;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Apps.Modules;
	using Skyline.DataMiner.Net.ManagerStore;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("UnitTest")]
	public sealed class InstallDomModulesApiTests
	{
		[TestMethod]
		public void ConstructingApi_DoesNotInstallOrReadAnything()
		{
			var store = new InMemoryDomSchemaStore();

			var api = store.CreateConnection().GetMediaOpsPlanApi();

			Assert.IsNotNull(api);
			Assert.AreEqual(0, store.Messages.Count);
		}

		[TestMethod]
		public void InstallDomModules_RejectsNullApi()
		{
			IMediaOpsPlanApi? api = null;
			Assert.ThrowsException<ArgumentNullException>(() => api!.InstallDomModules());
		}

		[TestMethod]
		public void InstallDomModules_InstallsOnlyTheFourPlanModules()
		{
			var store = new InMemoryDomSchemaStore();
			var api = store.CreateConnection().GetMediaOpsPlanApi();
			var log = new List<string>();
			api.SetLogger(new CallbackLogger(log.Add));

			api.InstallDomModules();

			CollectionAssert.AreEqual(
				new[] { "(slc)properties", "(slc)relationships", "(slc)resource_studio", "(slc)workflow" },
				store.ModuleSettings.Select(x => x.ModuleId).ToArray());
			Assert.AreEqual(50, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
			Assert.AreEqual(225, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Sum(x => x.Object.GetAllFieldDescriptors().Count()));
			Assert.AreEqual(5, store.Messages.OfType<ManagerStoreCreateRequest<DomBehaviorDefinition>>().Count());
			Assert.AreEqual(17, store.Messages.OfType<ManagerStoreCreateRequest<DomDefinition>>().Count());
			Assert.AreEqual(4, store.Messages.OfType<ManagerStoreReinitializeCustomManagerRequest>().Count());
			Assert.AreEqual(80, log.Count);
		}

		[TestMethod]
		public void InstallDomModules_SecondCallUsesFreshEqualDeclarations()
		{
			var store = new InMemoryDomSchemaStore();
			var api = store.CreateConnection().GetMediaOpsPlanApi();
			api.InstallDomModules();
			store.Messages.Clear();
			var log = new List<string>();
			api.SetLogger(new CallbackLogger(log.Add));

			api.InstallDomModules();

			Assert.AreEqual(76, log.Count(x => x.StartsWith("Skipping ", StringComparison.Ordinal)));
			Assert.AreEqual(80, log.Count);
			Assert.AreEqual(4, store.Messages.OfType<ManagerStoreReinitializeCustomManagerRequest>().Count());
			Assert.IsFalse(log.Any(x => x.StartsWith("Creating ", StringComparison.Ordinal) || x.StartsWith("Updating ", StringComparison.Ordinal)));
		}

		[TestMethod]
		public void InstallDomModules_FailureInFirstModuleDoesNotStartLaterModules()
		{
			var store = new InMemoryDomSchemaStore();
			store.OverrideResponse = message => message is ManagerStoreCreateRequest<DomDefinition>
				? throw new InvalidOperationException("Injected definition write failure.")
				: null;
			var api = store.CreateConnection().GetMediaOpsPlanApi();

			Assert.ThrowsException<InvalidOperationException>(api.InstallDomModules);

			Assert.AreEqual(1, store.ModuleSettings.Count);
			Assert.AreEqual("(slc)properties", store.ModuleSettings.Single().ModuleId);
			Assert.AreEqual(5, store.Messages.OfType<ManagerStoreCreateRequest<SectionDefinition>>().Count());
			Assert.AreEqual(0, store.Messages.OfType<ManagerStoreReinitializeCustomManagerRequest>().Count());
		}
	}
}
