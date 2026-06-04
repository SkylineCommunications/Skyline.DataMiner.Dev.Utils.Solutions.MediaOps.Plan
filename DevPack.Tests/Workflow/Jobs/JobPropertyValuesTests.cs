namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class JobPropertyValuesTests
	{
		[TestMethod]
		public void Job_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var job = new Job { Name = "Test" };

			Assert.IsNotNull(job.CustomPropertySettings);
			Assert.AreEqual(0, job.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstance_PropertyValuesIsEmpty()
		{
			var job = new Job { Name = "Test" };

			Assert.IsNotNull(job.PropertySettings);
			Assert.AreEqual(0, job.PropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstanceWithId_CustomPropertyValuesIsEmpty()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(job.CustomPropertySettings);
			Assert.AreEqual(0, job.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void Job_NewInstanceWithId_PropertyValuesIsEmpty()
		{
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			Assert.IsNotNull(job.PropertySettings);
			Assert.AreEqual(0, job.PropertySettings.Count);
		}

		[TestMethod]
		public void JobResourceNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void JobResourceNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void JobResourcePoolNode_NewInstance_CustomPropertyValuesIsEmpty()
		{
			var node = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.CustomPropertySettings);
			Assert.AreEqual(0, node.CustomPropertySettings.Count);
		}

		[TestMethod]
		public void JobResourcePoolNode_NewInstance_PropertyValuesIsEmpty()
		{
			var node = new JobResourcePoolNode(Guid.NewGuid());

			Assert.IsNotNull(node.PropertySettings);
			Assert.AreEqual(0, node.PropertySettings.Count);
		}

		[TestMethod]
		public void NewJob_NodeAddedThenProperties_PersistenceActionTargetsJobAndNode()
		{
			// Arrange: brand-new job with a new node added before any properties are configured.
			var job = new Job(Guid.NewGuid()) { Name = "Test" };
			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			job.NodeGraph.Add(node);

			node.AddCustomProperty(new CustomPropertySetting("Tag") { Value = "live" });

			// Act: simulate the save path - DomJobHandler calls EnsureContext() on each job.
			job.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, job.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count, "Nothing to delete - nothing was previously persisted.");
			Assert.AreEqual(1, toCreateOrUpdate.Count, "Only the node has dirty properties.");

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(job.Id.ToString(), collection.LinkedObjectId, "LinkedObjectId should be the owning job id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope, "Scope should be the MediaOps scope.");
			Assert.AreEqual(node.Id, collection.SubId, "SubId should be the node id.");
			Assert.AreEqual(1, collection.CustomSettings.Count, "The custom property added on the node should be present.");
			Assert.AreEqual("Tag", collection.CustomSettings.First().Name);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(job.Id, ownerId, "Failure reporting must map back to the job id.");
		}

		[TestMethod]
		public void NewJob_NodeAddedAfterContextEnsured_PersistenceActionTargetsJobAndNode()
		{
			// Arrange: job whose context has already been created (e.g. user touched properties
			// before adding the node). This simulates the loaded-job scenario where the context
			// existed before a freshly created node was attached to the graph.
			var job = new Job(Guid.NewGuid()) { Name = "Test" };

			// Trigger context creation up-front so the new node is added AFTER the context exists.
			job.EnsureContext();

			var node = new JobResourceNode(Guid.NewGuid(), Guid.NewGuid());
			node.AddCustomProperty(new CustomPropertySetting("Channel") { Value = "1" });
			job.NodeGraph.Add(node);

			// Act: save path re-runs EnsureContext, which must re-wire newly added nodes.
			job.EnsureContext();

			var ownerScopes = new[]
			{
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, job.PropertySettingsScope),
				new KeyValuePair<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertySettingsScope>(job.Id, node.PropertySettingsScope),
			};

			var (toCreateOrUpdate, toDelete, ownerByCollectionId) = ownerScopes.BuildPersistenceActions();

			// Assert
			Assert.AreEqual(0, toDelete.Count);
			Assert.AreEqual(1, toCreateOrUpdate.Count);

			var collection = toCreateOrUpdate[0];
			Assert.AreEqual(job.Id.ToString(), collection.LinkedObjectId, "Node added after the context was created must still resolve to the job id.");
			Assert.AreEqual(PropertySettingsContext.MediaOpsScope, collection.Scope);
			Assert.AreEqual(node.Id, collection.SubId);

			Assert.IsTrue(ownerByCollectionId.TryGetValue(collection.Id, out var ownerId));
			Assert.AreEqual(job.Id, ownerId);
		}
	}
}
