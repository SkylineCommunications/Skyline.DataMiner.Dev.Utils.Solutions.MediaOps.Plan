namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class JobPropertyManagementTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobPropertyManagementTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateAndUpdateJob_WithCustomPropertiesOnJobAndNodes_PersistsAllProperties()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var node1 = new JobResourcePoolNode(pool);
			node1.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			job.NodeGraph.Add(node1);

			job = objectCreator.CreateJob(job);
			Assert.IsNotNull(job);
			Assert.AreEqual(1, job.CustomPropertySettings.Count);
			var customJobProperty = job.CustomPropertySettings.First();
			Assert.AreEqual("Tag1", customJobProperty.Name);
			Assert.AreEqual("Value1", customJobProperty.Value);

			Assert.AreEqual(1, job.NodeGraph.Nodes.Count);
			foreach (var node in job.NodeGraph.Nodes)
			{
				Assert.AreEqual(1, node.CustomPropertySettings.Count);
				var customNodeProperty = node.CustomPropertySettings.First();
				Assert.AreEqual("Tag1", customNodeProperty.Name);
				Assert.AreEqual("Value1", customNodeProperty.Value);
			}

			// Update job and node properties
			job.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });

			node1 = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().First();
			node1.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });

			var node2 = new JobResourcePoolNode(pool);
			node2.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			node2.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			job.NodeGraph.Add(node2);

			job = TestContext.Api.Jobs.Update(job);
			Assert.IsNotNull(job);
			Assert.AreEqual(2, job.CustomPropertySettings.Count);
			var customJobTag1Property = job.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag1");
			Assert.IsNotNull(customJobTag1Property);
			Assert.AreEqual("Value1", customJobTag1Property.Value);
			var customJobTag2Property = job.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag2");
			Assert.IsNotNull(customJobTag2Property);
			Assert.AreEqual("Value2", customJobTag2Property.Value);

			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			foreach (var node in job.NodeGraph.Nodes)
			{
				Assert.AreEqual(2, node.CustomPropertySettings.Count);
				var customNodeTag1Property = node.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag1");
				Assert.IsNotNull(customNodeTag1Property);
				Assert.AreEqual("Value1", customNodeTag1Property.Value);
				var customNodeTag2Property = node.CustomPropertySettings.FirstOrDefault(x => x.Name == "Tag2");
				Assert.IsNotNull(customNodeTag2Property);
				Assert.AreEqual("Value2", customNodeTag2Property.Value);
			}
		}

		[TestMethod]
		public void CreateJob_WithPropertiesOnJobAndNodes_PersistedCollectionsCarryOwnerMetadata()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
			job.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var node1 = new JobResourcePoolNode(pool);
			node1.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			job.NodeGraph.Add(node1);

			var node2 = new JobResourcePoolNode(pool);
			node2.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			job.NodeGraph.Add(node2);

			job = objectCreator.CreateJob(job);

			// Read back the persisted collections straight from the repository to verify the metadata
			// that addresses each collection (LinkedObjectId, Scope and SubId).
			var collections = ReadCollections(job.Id);
			var nodeIds = job.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();

			Assert.AreEqual(3, collections.Count, "Expected one owner collection plus one collection per node.");
			Assert.IsTrue(collections.All(c => c.LinkedObjectId == job.Id.ToString()), "Every collection must be linked to the job id.");
			Assert.IsTrue(collections.All(c => c.Scope == PropertySettingsContext.MediaOpsScope), "Every collection must use the MediaOps scope.");

			var ownerCollection = collections.SingleOrDefault(c => string.IsNullOrEmpty(c.SubId));
			Assert.IsNotNull(ownerCollection, "The owner-level collection must use an empty SubId.");

			var nodeCollections = collections.Where(c => !string.IsNullOrEmpty(c.SubId)).ToList();
			Assert.AreEqual(2, nodeCollections.Count, "Each node must have its own collection.");
			Assert.IsTrue(nodeCollections.All(c => nodeIds.Contains(c.SubId)), "Each node collection's SubId must match a node id.");
		}

		[TestMethod]
		public void CreateJobFromWorkflow_WithPropertiesOnWorkflowAndNodes_PersistedCollectionsCarryJobMetadata()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var node1 = new WorkflowResourcePoolNode(pool);
			node1.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });
			workflow.NodeGraph.Add(node1);

			var node2 = new WorkflowResourcePoolNode(pool);
			node2.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			workflow.NodeGraph.Add(node2);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);
			job.Name = $"{prefix}_Job";
			job.Start = DateTime.UtcNow.RoundToNextSecond();
			job.End = job.Start.AddMinutes(10);
			job.PreRollStart = job.Start;
			job.PostRollEnd = job.End;

			job = objectCreator.CreateJob(job);

			// The persisted collections must be addressed by the NEW job (and its freshly cloned node ids),
			// not by the source workflow.
			var collections = ReadCollections(job.Id);
			var nodeIds = job.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();

			Assert.AreEqual(3, collections.Count, "Expected one owner collection plus one collection per cloned node.");
			Assert.IsTrue(collections.All(c => c.LinkedObjectId == job.Id.ToString()), "Every collection must be linked to the new job id, not the workflow.");
			Assert.IsTrue(collections.All(c => c.Scope == PropertySettingsContext.MediaOpsScope), "Every collection must use the MediaOps scope.");

			var ownerCollection = collections.SingleOrDefault(c => string.IsNullOrEmpty(c.SubId));
			Assert.IsNotNull(ownerCollection, "The owner-level collection must use an empty SubId.");

			var nodeCollections = collections.Where(c => !string.IsNullOrEmpty(c.SubId)).ToList();
			Assert.AreEqual(2, nodeCollections.Count, "Each cloned node must have its own collection.");
			Assert.IsTrue(nodeCollections.All(c => nodeIds.Contains(c.SubId)), "Each node collection's SubId must match a cloned node id.");
		}

		[TestMethod]
		public void UpdateJob_ValuesChangedInPlaceOnJob_PersistsValues()
		{
			var prefix = Guid.NewGuid();
			var (stringProperty, booleanProperty, discreteProperty) = CreateSchedulingProperties(prefix);

			var job = NewJob($"{prefix}_Job");
			job.AddProperty(new StringPropertySetting(stringProperty) { Value = "Original" });
			job.AddProperty(new BooleanPropertySetting(booleanProperty) { Value = false });
			job.AddProperty(new DiscretePropertySetting(discreteProperty) { Value = "A" });
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "Original" });
			job = objectCreator.CreateJob(job);

			var read = TestContext.Api.Jobs.Read(job.Id);
			read.PropertySettings.OfType<StringPropertySetting>().Single().Value = "Changed";
			read.PropertySettings.OfType<BooleanPropertySetting>().Single().Value = true;
			read.PropertySettings.OfType<DiscretePropertySetting>().Single().Value = "B";
			read.CustomPropertySettings.Single().Value = "Changed";
			TestContext.Api.Jobs.Update(read);

			var returned = TestContext.Api.Jobs.Read(job.Id);
			AssertChangedValues(returned.PropertySettings, returned.CustomPropertySettings);
		}

		[TestMethod]
		public void UpdateJob_ValuesChangedInPlaceOnNode_PersistsValues()
		{
			var prefix = Guid.NewGuid();
			var (stringProperty, booleanProperty, discreteProperty) = CreateSchedulingProperties(prefix);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var node = new JobResourcePoolNode(pool);
			node.AddProperty(new StringPropertySetting(stringProperty) { Value = "Original" });
			node.AddProperty(new BooleanPropertySetting(booleanProperty) { Value = false });
			node.AddProperty(new DiscretePropertySetting(discreteProperty) { Value = "A" });
			node.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "Original" });

			var job = NewJob($"{prefix}_Job");
			job.NodeGraph.Add(node);
			job = objectCreator.CreateJob(job);

			var read = TestContext.Api.Jobs.Read(job.Id);
			var readNode = read.NodeGraph.Nodes.Single();
			readNode.PropertySettings.OfType<StringPropertySetting>().Single().Value = "Changed";
			readNode.PropertySettings.OfType<BooleanPropertySetting>().Single().Value = true;
			readNode.PropertySettings.OfType<DiscretePropertySetting>().Single().Value = "B";
			readNode.CustomPropertySettings.Single().Value = "Changed";
			TestContext.Api.Jobs.Update(read);

			var returnedNode = TestContext.Api.Jobs.Read(job.Id).NodeGraph.Nodes.Single();
			AssertChangedValues(returnedNode.PropertySettings, returnedNode.CustomPropertySettings);
		}

		[TestMethod]
		public void ReadJob_OnlyReadingSettings_HasNoPropertySettingChanges()
		{
			var job = CreateJobWithStringProperty(out _);

			var read = TestContext.Api.Jobs.Read(job.Id);
			_ = read.PropertySettings.Single().Id;
			_ = read.CustomPropertySettings.Single().Value;

			Assert.IsFalse(read.HasPropertySettingChanges);
		}

		[TestMethod]
		public void SetProperties_WithUnchangedValues_HasNoPropertySettingChanges()
		{
			var job = CreateJobWithStringProperty(out var property);

			var read = TestContext.Api.Jobs.Read(job.Id);
			read.SetProperties([new StringPropertySetting(property) { Value = "Original" }]);
			read.SetCustomProperties([new CustomPropertySetting("Tag") { Value = "Original" }]);

			Assert.IsFalse(read.HasPropertySettingChanges);
		}

		[TestMethod]
		public void ValueChangedInPlaceAndRestored_HasNoPropertySettingChanges()
		{
			var job = CreateJobWithStringProperty(out _);

			var read = TestContext.Api.Jobs.Read(job.Id);
			var setting = read.PropertySettings.OfType<StringPropertySetting>().Single();
			setting.Value = "Changed";
			Assert.IsTrue(read.HasPropertySettingChanges);

			setting.Value = "Original";
			Assert.IsFalse(read.HasPropertySettingChanges);
		}

		private Job CreateJobWithStringProperty(out StringProperty property)
		{
			var prefix = Guid.NewGuid();
			property = objectCreator.CreateSchedulingProperty(new StringProperty { Name = $"{prefix}_String", SectionName = "General" });

			var job = NewJob($"{prefix}_Job");
			job.AddProperty(new StringPropertySetting(property) { Value = "Original" });
			job.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "Original" });
			return objectCreator.CreateJob(job);
		}

		private static void AssertChangedValues(IReadOnlyCollection<PropertySetting> settings, IReadOnlyCollection<CustomPropertySetting> customSettings)
		{
			Assert.AreEqual("Changed", settings.OfType<StringPropertySetting>().Single().Value);
			Assert.IsTrue(settings.OfType<BooleanPropertySetting>().Single().Value);
			Assert.AreEqual("B", settings.OfType<DiscretePropertySetting>().Single().Value);
			Assert.AreEqual("Changed", customSettings.Single().Value);
		}

		private static Job NewJob(string name)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			return new Job
			{
				Name = name,
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};
		}

		private (StringProperty, BooleanProperty, DiscreteProperty) CreateSchedulingProperties(Guid prefix)
		{
			var stringProperty = objectCreator.CreateSchedulingProperty(new StringProperty { Name = $"{prefix}_String", SectionName = "General" });
			var booleanProperty = objectCreator.CreateSchedulingProperty(new BooleanProperty { Name = $"{prefix}_Boolean", SectionName = "General" });
			var discreteProperty = new DiscreteProperty { Name = $"{prefix}_Discrete", SectionName = "General" };
			discreteProperty.AddDiscrete("A").AddDiscrete("B");
			discreteProperty = objectCreator.CreateSchedulingProperty(discreteProperty);

			return (stringProperty, booleanProperty, discreteProperty);
		}

		private System.Collections.Generic.List<PropertySettingCollection> ReadCollections(Guid ownerId)
		{
			var filter = new ANDFilterElement<PropertySettingCollection>(
				PropertySettingCollectionExposers.LinkedObjectId.Equal(ownerId.ToString()),
				PropertySettingCollectionExposers.Scope.Equal(PropertySettingsContext.MediaOpsScope));

			return TestContext.Api.PropertySettingCollections.Read(filter).ToList();
		}
	}
}
